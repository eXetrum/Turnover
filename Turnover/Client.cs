using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

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

        public Client(Socket accepted)
        {
            clientSocket = accepted;
            EndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

            StateObject state = new StateObject();
            state.workSocket = clientSocket;

            clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 
                SocketFlags.None, new AsyncCallback(receiveCallback), state);
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public MemoryStream stream = new MemoryStream();
        }

        void receiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                // Read data from the client socket.
                int read = handler.EndReceive(ar);

                state.stream.Write(state.buffer, 0, read);

                // Data was read from the client socket.
                if (read > 0)
                {
                    if (handler.Available == 0)
                    {
                        //MessageBox.Show(string.Format("Available: {0}, read: {1}, reciveBuffSize: {2}", handler.Available, read, handler.ReceiveBufferSize));
                        byte[] totalReceived = state.stream.ToArray();
                        byte[] decryptedBytes = new Security().Decrypt(totalReceived);
                        Packet receivedPacket = (Packet)Packet.ByteArrayToObject(decryptedBytes);
                        receivedPacket.from = (IPEndPoint)handler.RemoteEndPoint;
                        if (Received != null) Received(this, receivedPacket);
                        state.stream.Close();
                        state.stream.Dispose();
                        state.stream = null;
                        state.stream = new MemoryStream();
                    }

                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(receiveCallback), state);
                }
                else
                {
                    if (Disconnected != null) Disconnected(this);
                    Close();
                }
            }
            catch (SocketException)
            {
                if (Disconnected != null) Disconnected(this);
                Close();
            }
            catch (ObjectDisposedException)
            {
                if (Disconnected != null) Disconnected(this);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Client recive callback error: " + ex.Message);
                if (Disconnected != null) Disconnected(this);
                Close();
            }
        }

        public void Close()
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

    }
}
