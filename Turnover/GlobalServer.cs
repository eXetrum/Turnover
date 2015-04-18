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
        public delegate void ClientReceivedMulticastHandler(Packet p);
        public delegate void ClientStatusOnlineChangedHandle(Packet p);
        public delegate void ClientStatusOfflineChangedHandle(Packet p);

        public event ClientReceivedMulticastHandler ReceivedMulticast;
        public event ClientStatusOnlineChangedHandle ClientStatusOnline;
        public event ClientStatusOfflineChangedHandle ClientStatusOffline;

        public delegate void SocketAcceptedHandler(Socket e);
        public event SocketAcceptedHandler AcceptedPM;

        private UdpClient udpSender;
        private IPAddress multicastAddress;
        private int multicastPort;
        private IPEndPoint multicastEP;
        private int privatePort;
        private TcpListener privateListener;
        private Thread inviteThread = null;
        ManualResetEvent inviteEvent = new ManualResetEvent(false);

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
            multicastEP = new IPEndPoint(multicastAddress, multicastPort);

            udpSender = new UdpClient();
            /*udpclient.MulticastLoopback = true;
            udpclient.JoinMulticastGroup(multicastAddress);*/
            
            ////////////////////////////////////////////////////////////////
            privatePort = Properties.Settings.Default.privatePort;

            
            ////////////////////////////////////////////////////////////////
        }

        public void Listen()
        {
            // TCP listener для приватных сообщений
            privateListener = new TcpListener(new IPEndPoint(IPAddress.Any, privatePort));
            privateListener.Start(0);
            privateListener.BeginAcceptSocket(acceptedPMCallback, null);

            // UDP listener для сообщений мультикаст группы
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);
            UdpClient client = new UdpClient();
            client.ExclusiveAddressUse = false;            
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(localEP);
            client.JoinMulticastGroup(multicastAddress);

            UdpData udpData = new UdpData();
            udpData.workUDPclient = client;
            udpData.ipEndPoint = localEP;

            client.BeginReceive(new AsyncCallback(receiveCallback), udpData);

            // Разослать приглашения
            inviteThread = new Thread(Inviter);
            inviteThread.IsBackground = true;
            inviteThread.Start();
        }

        public void StopGlobalListener()
        {
            inviteEvent.Set();
            inviteThread.Join();

            //udpSender.DropMulticastGroup(multicastAddress);
            udpSender.Close();

            privateListener.Stop();
        }

        private void Inviter(object sender)
        {
            do
            {
                Packet invitePacket = new Packet(MSG_TYPE.STATUS_ONLINE, null,
                    Properties.Settings.Default.NickName,
                    privatePort);
                SendMulticastMessage(invitePacket);

            } while (!inviteEvent.WaitOne(2000));
            // Рассылаем прощание
            Packet endPacket = new Packet(MSG_TYPE.STATUS_OFFLINE, null, Properties.Settings.Default.NickName, privatePort);
            SendMulticastMessage(endPacket);
        }

        public void SendMulticastMessage(Packet packet)
        {
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            udpSender.Send(encrypted, encrypted.Length, multicastEP);
        }

        public void SendPrivateMessage(Socket remote, Packet packet)
        {
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            remote.Send(encrypted);
        }

        private void acceptedPMCallback(IAsyncResult ar)
        {
            try
            {
                Socket s = privateListener.EndAcceptSocket(ar);

                if (AcceptedPM != null)
                    AcceptedPM(s);
                privateListener.BeginAcceptSocket(new AsyncCallback(acceptedPMCallback), null);
            }
            catch (ObjectDisposedException)
            { return; }
            catch (NullReferenceException)
            { return; }
            catch (Exception ex)
            {
                MessageBox.Show("SERVER AcceptPMcallback: " + ex.Message);
            }
        }    

        void receiveCallback(IAsyncResult ar)
        {
            try
            {
                UdpData udpData = (UdpData)ar.AsyncState;
                UdpClient client = udpData.workUDPclient;
                IPEndPoint ep = udpData.ipEndPoint;


                byte[] receivedBytes = client.EndReceive(ar, ref ep);
                byte[] decryptedBytes = new Security().Decrypt(receivedBytes);

                Packet receivedPacket = (Packet)Packet.ByteArrayToObject(decryptedBytes);
                receivedPacket.from = ep;

                switch (receivedPacket.msgType)
                {
                    case MSG_TYPE.STATUS_ONLINE:
                        {
                            if (ClientStatusOnline != null)
                                ClientStatusOnline(receivedPacket);
                        }
                        break;
                    case MSG_TYPE.STATUS_OFFLINE:
                        {
                            if (ClientStatusOffline != null)
                                ClientStatusOffline(receivedPacket);
                        }
                        break;
                    case MSG_TYPE.MESSAGE:
                        {
                            if (ReceivedMulticast != null)
                                ReceivedMulticast(receivedPacket);
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
            catch (Exception ex)
            {
                MessageBox.Show("SERVER ReceiveCallback: " + ex.Message);
            }
        }

        
    }
}
