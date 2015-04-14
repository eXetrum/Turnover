using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Turnover
{
    class Server
    {
        public delegate void SocketAcceptedHandler(Socket e);
        public event SocketAcceptedHandler Accepted;

        Socket serverSocket;
        public int Port;

        public bool Running { get; private set; }
        public Server() { Port = 0; }
        public void Start(int port)
        {
            if (Running) return;

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);

            serverSocket.BeginAccept(acceptedCallback, null);
            Running = true;
        }

        public void Stop()
        {
            if (!Running) return;

            serverSocket.Close();
            serverSocket.Shutdown(SocketShutdown.Both);
            serverSocket.Close();

            Running = false;
        }

        void acceptedCallback(IAsyncResult ar)
        {
            try
            {
                Socket s = serverSocket.EndAccept(ar);

                if(Accepted != null)
                    Accepted(s);
            }
            catch
            {
            }

            if(Running)
            {
                
                try
                {
                    serverSocket.BeginAccept(acceptedCallback, null);
                }
                catch
                {
                }
            }

        }


    }
}
