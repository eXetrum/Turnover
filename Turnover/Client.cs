using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;

namespace Turnover
{
    public class Client
    {
        public IPEndPoint EndPoint { get; private set; }
        private Socket clientSocket;

        public delegate void DataReceivedEventHandler(Client sender, Packet p);
        public delegate void DisconnectedEventHandler(Client sender);
        public event DataReceivedEventHandler Received;
        public event DisconnectedEventHandler Disconnected;

        byte[] lenBuffer;
        ReceiveBuffer buffer;

        public class ReceiveBuffer
        {
            public const int BUFFER_SIZE = 1024;
            public long ToReceive;
            public byte[] Buffer;            
            public MemoryStream memStream;

            public ReceiveBuffer(long toRec)
            {
                ToReceive = toRec;
                Buffer = new byte[BUFFER_SIZE];
                memStream = new MemoryStream();
            }

            public void Dispose()
            {
                Buffer = null;
                ToReceive = 0;
                if (memStream != null && memStream.CanWrite)
                {
                    memStream.Close();
                    memStream.Dispose();
                    memStream = null;
                }
            }
        }

        public Client(Socket accepted)
        {
            clientSocket = accepted;
            EndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            lenBuffer = new byte[4];

            StateObject state = new StateObject();
            state.workSocket = clientSocket;

            clientSocket.BeginReceive(lenBuffer, 0, lenBuffer.Length, SocketFlags.None, new AsyncCallback(receiveAsync), state);
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public MemoryStream stream = new MemoryStream();
        }

        void receiveAsync(IAsyncResult ar)
        {
            try
            {
                int rec = clientSocket.EndReceive(ar);
                if (rec == 0)
                {
                    if (Disconnected != null)
                    {
                        Disconnected(this);
                        return;
                    }
                    if (rec != 4)
                    {
                        throw new Exception("Error file size header");
                    }
                }
            }
            catch (SocketException se)
            {
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                        if (Disconnected != null)
                        {
                            Disconnected(this);
                            return;
                        }
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            buffer = new ReceiveBuffer(BitConverter.ToInt32(lenBuffer, 0));

            clientSocket.BeginReceive(buffer.Buffer, 0, buffer.Buffer.Length, SocketFlags.None, new AsyncCallback(receivePacketCallback), null);

        }

        void receivePacketCallback(IAsyncResult ar)
        {
            int rec = clientSocket.EndReceive(ar);

            if (rec <= 0)
            {
                return;
            }

            buffer.memStream.Write(buffer.Buffer, 0, rec);

            buffer.ToReceive -= rec;

            if (buffer.ToReceive > 0)
            {
                Array.Clear(buffer.Buffer, 0, buffer.Buffer.Length);
                clientSocket.BeginReceive(buffer.Buffer, 0, buffer.Buffer.Length, SocketFlags.None, receivePacketCallback, null);
                return;
            }

            if (Received != null)
            {
                byte[] totalReceived = buffer.memStream.ToArray();
                Packet receivedPacket = (Packet)Packet.ByteArrayToObject(new Security().Decrypt(totalReceived));
                receivedPacket.from = EndPoint;
                Received(this, receivedPacket);
            }

            if (Disconnected != null)
                Disconnected(this);

            Close();
        }

        public void Close()
        {
            if (clientSocket != null)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            clientSocket = null;
            buffer.Dispose();
            lenBuffer = null;
            Disconnected = null;
            Received = null;
        }

    }
}
