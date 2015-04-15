using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

namespace Turnover
{
    class GlobalServer
    {

        public class UdpData
        {
            public UdpClient workUDPclient = null;
            public IPEndPoint ipEndPoint = null;
        }

        public GlobalServer()
        {
            // один из зарезервированных для локальных нужд UDP адресов
            multicastAddress = IPAddress.Parse(Properties.Settings.Default.multicastGroup); 
            multicastPort = Properties.Settings.Default.multicastPort;

            udpclient = new UdpClient();
            udpclient.JoinMulticastGroup(multicastAddress);
            remoteEP = new IPEndPoint(multicastAddress, multicastPort);
            
            // Разослать приглашения
            inviteThread = new Thread(Inviter);
            inviteThread.IsBackground = true;
            inviteThread.Start();

        }

        private UdpClient udpclient;
        private IPAddress multicastAddress;
        private int multicastPort;
        private IPEndPoint remoteEP;        

        private Thread inviteThread = null;

        public void SendMessage(Packet packet)
        {
            Byte[] encrypted = Encrypt(Packet.ObjectToByteArray(packet));
            udpclient.Send(encrypted, encrypted.Length, remoteEP);
        }
        ManualResetEvent inviteEvent = new ManualResetEvent(false);
        private void Inviter(object sender)
        {
            do
            {
                Packet invitePacket = new Packet(MSG_TYPE.STATUS_ONLINE, null, Properties.Settings.Default.NickName, null);
                SendMessage(invitePacket);

            } while (!inviteEvent.WaitOne(2000));
            // Рассылаем прощание
            Packet endPacket = new Packet(MSG_TYPE.STATUS_OFFLINE, null, Properties.Settings.Default.NickName, null);
            SendMessage(endPacket);
        }

        public void StopGlobalListener()
        {
            inviteEvent.Set();
            inviteThread.Join();

            udpclient.DropMulticastGroup(multicastAddress);
            udpclient.Close();
        }


        ManualResetEvent allDone = new ManualResetEvent(false);

        public void Listen()
        {
            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEP);

            client.JoinMulticastGroup(multicastAddress);

            UdpData udpData = new UdpData();
            udpData.workUDPclient = client;
            udpData.ipEndPoint = localEP;

            client.BeginReceive(new AsyncCallback(receiveCallback), udpData);            
        }

        public delegate void ClientReceivedHandler(IPEndPoint ep, Packet p);
        public delegate void ClientStatusOnlineChangedHandle(IPEndPoint ep, Packet p);
        public delegate void ClientStatusOfflineChangedHandle(IPEndPoint ep);

        public event ClientReceivedHandler Received;        
        public event ClientStatusOnlineChangedHandle ClientStatusOnline;        
        public event ClientStatusOfflineChangedHandle ClientStatusOffline;
        

        void receiveCallback(IAsyncResult ar)
        {
            UdpData udpData = (UdpData)ar.AsyncState;
            UdpClient client = udpData.workUDPclient;
            IPEndPoint ep = udpData.ipEndPoint;


            byte[] receivedBytes = client.EndReceive(ar, ref ep);
            byte[] decryptedBytes = Decrypt(receivedBytes);

            Packet receivedPacket = (Packet)Packet.ByteArrayToObject(decryptedBytes);

            switch (receivedPacket.msgType)
            {
                case MSG_TYPE.STATUS_ONLINE:
                    {
                        if (ClientStatusOnline != null)
                            ClientStatusOnline(ep, receivedPacket);
                    }
                    break;
                case MSG_TYPE.STATUS_OFFLINE:
                    {
                        if (ClientStatusOffline != null)
                            ClientStatusOffline(ep);
                    }
                    break;
                case MSG_TYPE.MESSAGE:
                    {
                        if (Received != null)
                            Received(ep, receivedPacket);
                    }
                    break;
                case MSG_TYPE.FILE:
                    {
                    }
                    break;
                default:
                    break;
            }

            client.BeginReceive(new AsyncCallback(receiveCallback), udpData);
        }

        #region Encrypt/Decrypt
        private byte[] Encrypt(byte[] clearBytes, string EncryptionKey = "123")
        {
            ///
            EncryptionKey = Properties.Settings.Default.SECRET_KEY;
            ///
            
            byte[] encrypted;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }); // еще один плюс шарпа в наличие таких вот костылей.
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encrypted = ms.ToArray();
                }
            }
            return encrypted;
        }

        private byte[] Decrypt(byte[] cipherBytes, string EncryptionKey = "123")
        {
            ///
            EncryptionKey = Properties.Settings.Default.SECRET_KEY;
            ///
            byte[] decryptedBytes = null;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }
            return decryptedBytes;
        }
        #endregion
    }
}
