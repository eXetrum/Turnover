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
            ////////////////////////////////////////////////////////////////
            privatePort = Properties.Settings.Default.privatePort;

            privateListener = new TcpListener(new IPEndPoint(IPAddress.Any, privatePort));


            /*var port = ((IPEndPoint)privateListener.LocalEndpoint).Port;
            privatePort = port;*/

            privateListener.Start(0);

            privateListener.BeginAcceptSocket(acceptedPMCallback, null);

            ////////////////////////////////////////////////////////////////
            // Разослать приглашения
            inviteThread = new Thread(Inviter);
            inviteThread.IsBackground = true;
            inviteThread.Start();

        }
        TcpListener privateListener;


        public delegate void SocketAcceptedHandler(Socket e);
        public event SocketAcceptedHandler AcceptedPM;

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
                MessageBox.Show(ex.Message);
            }
        }

        private UdpClient udpclient;
        private IPAddress multicastAddress;
        private int multicastPort;
        private IPEndPoint remoteEP;
        private int privatePort;

        private Thread inviteThread = null;

        public void SendMulticastMessage(Packet packet)
        {
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            udpclient.Send(encrypted, encrypted.Length, remoteEP);
        }

        public void SendPrivateMessage(Socket remote, Packet packet)
        {
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            remote.Send(encrypted);
        }


        ManualResetEvent inviteEvent = new ManualResetEvent(false);
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

        public void StopGlobalListener()
        {
            inviteEvent.Set();
            inviteThread.Join();

            udpclient.DropMulticastGroup(multicastAddress);
            udpclient.Close();

            privateListener.Stop();
        }


        ManualResetEvent allDone = new ManualResetEvent(false);

        public void Listen()
        {
            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Ttl = 0;
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEP);

            client.JoinMulticastGroup(multicastAddress);

            UdpData udpData = new UdpData();
            udpData.workUDPclient = client;
            udpData.ipEndPoint = localEP;

            client.BeginReceive(new AsyncCallback(receiveCallback), udpData);            
        }

        public delegate void ClientReceivedMulticastHandler(Packet p);
        public delegate void ClientStatusOnlineChangedHandle(Packet p);
        public delegate void ClientStatusOfflineChangedHandle(Packet p);

        public event ClientReceivedMulticastHandler ReceivedMulticast;        
        public event ClientStatusOnlineChangedHandle ClientStatusOnline;        
        public event ClientStatusOfflineChangedHandle ClientStatusOffline;
        

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
                MessageBox.Show("receive " + ex.Message);
            }
        }

        
    }
}
