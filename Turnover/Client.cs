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
    class Client
    {
        public static Encoding encoding = new UTF8Encoding();

        public IPEndPoint EndPoint { get; private set; }
        private Socket clientSocket;

        public delegate void DataReceivedEventHandler(Client sender, byte[] data);
        public delegate void DisconnectedEventHandler(Client sender);
        public event DataReceivedEventHandler Received;
        public event DisconnectedEventHandler Disconnected;
        

        public Client(Socket accepted)
        {
            clientSocket = accepted;
            EndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

            StateObject state = new StateObject();
            state.workSocket = clientSocket;

            clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback), state);
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
        }

        void receiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.
            if (read > 0)
            {
                state.sb.Append(encoding.GetString(state.buffer, 0, read));

                if (Received != null) Received(this, state.buffer);

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(receiveCallback), state);
            }
            else
            {
                if (state.sb.Length > 1)
                {
                    // All the data has been read from the client;
                    // display it on the console.
                    string content = state.sb.ToString();
                    /*Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                       content.Length, content);*/

                    if (Received != null) Received(this, encoding.GetBytes(content));
                }

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
