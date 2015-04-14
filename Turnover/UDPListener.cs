using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Turnover
{
    public class UDPListener : UdpClient
    {

        TcpListener tcpListener;

        Form form;

        bool online = true;
        public UDPListener(int broadcastPort, int privatePort, 
            string nickName, Form form, TextBox logTextBox, ListView usersListView)
            : base(broadcastPort)
        {
            this.form = form;
            

            this.broadcastPort = broadcastPort;
            this.privatePort = privatePort;

            this.localIP = GetLocalIP();
            this.broadCastIP = GetBroadcastIP(this.localIP);

            this.logTextBox = logTextBox;

            this.user = new User(GetLocalIP().ToString(), nickName);

            this.usersOnline = new ObservableList<User>(usersListView);
            this.inviteEvent = new ManualResetEvent(false);

        }
        private void Log(string text,params object[] args)
        {
            this.logTextBox.Invoke(new Action(() =>
            {
                this.logTextBox.AppendText(string.Format(text, args) + Environment.NewLine);
            }));
        }

        public void SendMessage(string message)
        {
            // Рассылаем сообщения
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] sendbuf = Encoding.UTF8.GetBytes(message);
            IPEndPoint ep = new IPEndPoint(broadCastIP, broadcastPort);
            socket.SendTo(sendbuf, ep);
            socket.Close();
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public PrivateChat f;
        }
        ManualResetEvent allDone = new ManualResetEvent(false);
        void StartPMListener()
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPEndPoint localEP = new IPEndPoint(ipHostInfo.AddressList[0], privatePort);

            Console.WriteLine("Local address and port : {0}", localEP.ToString());

            Socket listener = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEP);
                listener.Listen(10);

                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(acceptCallback),
                        listener);

                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Closing the listener...");
        }

        public Socket listener = null;

        void acceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            PrivateChat pc = new PrivateChat(this);

            form.Invoke(new Action(() =>
            {
                pc.Show();
                pc.FormClosing += new FormClosingEventHandler(delegate(object sender, FormClosingEventArgs e)
                    {
                        MessageBox.Show("qwe1");
                    });
            }));

            // Signal the main thread to continue.
            allDone.Set();

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            state.f = pc;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(readCallback), state);
        }

        void readCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            PrivateChat f = state.f;

            // Read data from the client socket.
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.
            if (read > 0)
            {
                //f.txtReceived.AppendText(Encoding.ASCII.GetString(state.buffer, 0, read));
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(readCallback), state);

                /*string content = state.sb.ToString();
                Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                       content.Length, content);*/
            }
            else
            {
                if (state.sb.Length > 1)
                {
                    // All the data has been read from the client;
                    // display it on the console.
                    string content = state.sb.ToString();
                    Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                       content.Length, content);

                    f.txtReceived.AppendText(content);

                }
                f.Close();
                handler.Close();
            }
        }









        public void StartListen()
        {
            try {
                this.BeginReceive(new AsyncCallback(recv), null);
                

                this.listenThread = new Thread(new ThreadStart(StartPMListener));

                this.listenThread.IsBackground = true;
                this.listenThread.Start();

                /*
                this.tcpListener = new TcpListener(IPAddress.Any, privatePort);
                this.listenThread = new Thread(new ThreadStart(listenPrivate));

                this.listenThread.IsBackground = true;
                this.listenThread.Start();*/

                //this.Log("UDPListen start...");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); }

        }
        // Разослать приглашения
        public void SendInvites()
        {
            inviteThread = new Thread(Inviter);
            inviteThread.Start();
        }

        private void Inviter(object sender)
        {
            string goodBye, wellcome = string.Empty;
            byte[] sendbuf = null;
            IPEndPoint ep = null;

            do
            {
                goodBye = "USER_OFFLINE:" + User.Serialize(user);
                wellcome = "USER_ONLINE:" + User.Serialize(user);
                // Рассылаем приглашения
                Socket wellcomeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sendbuf = Encoding.UTF8.GetBytes(wellcome);
                ep = new IPEndPoint(broadCastIP, broadcastPort);
                wellcomeSocket.SendTo(sendbuf, ep);
                wellcomeSocket.Close();

            } while (!inviteEvent.WaitOne(2000));
            // Рассылаем широковещательное прощание
            Socket goodByeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sendbuf = Encoding.UTF8.GetBytes(goodBye);
            ep = new IPEndPoint(broadCastIP, broadcastPort);
            goodByeSocket.SendTo(sendbuf, ep);
            goodByeSocket.Close();
        }

        public void StopListener()
        {
            this.inviteEvent.Set();
            this.Close();
        }



        public static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        // Accept one client connection asynchronously.
        public static void DoBeginAcceptTcpClient(TcpListener listener)
        {
            // Set the event to nonsignaled state.
            tcpClientConnected.Reset();

            // Start to listen for connections from a client.
            Console.WriteLine("Waiting for a connection...");

            // Accept the connection. 
            // BeginAcceptSocket() creates the accepted socket.
            listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);
            // Wait until a connection is made and processed before 
            // continuing.
            tcpClientConnected.WaitOne();
        }

        // Process the client connection.
        public static void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;

            // End the operation and display the received data on 
            // the console.
            TcpClient client = listener.EndAcceptTcpClient(ar);

            // Process the connection here. (Add the client to a
            // server table, read data, etc.)
            Console.WriteLine("Client connected completed");

            // Signal the calling thread to continue.
            tcpClientConnected.Set();

        }







        private void listenPrivate()
        {
            /*
            this.tcpListener.Start();

            do
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();
                

                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);

            } while (!inviteEvent.WaitOne(100));*/
            
            MessageBox.Show("private done");
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            /*form.Invoke(new MethodInvoker(delegate()
            {}));*/
            
            Form clientForm = new Form();
            TextBox textMessages = new TextBox();
            Button sendButton = new Button();

            ManualResetEvent readDone = new ManualResetEvent(false);

            /*
            clientForm.Text = "Private Messages Text";
            clientForm.Width = 400;
            clientForm.Height = 200;
            clientForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            clientForm.FormClosing += new FormClosingEventHandler(delegate(object sender, FormClosingEventArgs e)
            {
                tcpClient.Close();

            });

            textMessages.Multiline = true;
            textMessages.Width = 360;
            textMessages.Height = 200;
            textMessages.Location = new System.Drawing.Point(0, 0);


            sendButton.Text = "Send";
            sendButton.Size = new System.Drawing.Size(40, 40);
            sendButton.Location = new System.Drawing.Point(360, 160);

            clientForm.Controls.Add(textMessages);
            clientForm.Controls.Add(sendButton);*/

                form.Invoke(new MethodInvoker(delegate()
                {
                    clientForm.Text = "Private Messages Text";
                    clientForm.Width = 400;
                    clientForm.Height = 180;
                    clientForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;

                    clientForm.FormClosing += new FormClosingEventHandler(delegate(object sender, FormClosingEventArgs e)
                    {
                        readDone.Set();
                        MessageBox.Show("qwe"); 

                    }); 
                    
                    textMessages.Multiline = true;
                    textMessages.Width = 300;
                    textMessages.Height = 180;
                    textMessages.Location = new System.Drawing.Point(0, 0);


                    sendButton.Text = "Send";
                    sendButton.Size = new System.Drawing.Size(100, 180);
                    sendButton.Location = new System.Drawing.Point(300, 0);

                    clientForm.Controls.Add(textMessages);
                    clientForm.Controls.Add(sendButton);
                    clientForm.Show();
                }));
            
            //
 
            
            /*MessageBox.Show(Application.OpenForms[0].Text);
            MessageBox.Show(Application.OpenForms[1].Text);*/


            

            //new System.Threading.Thread(clientForm).Start();


            while (!readDone.WaitOne(100))
            {

                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                //Log( encoder.GetString(message, 0, bytesRead));
                textMessages.AppendText(encoder.GetString(message, 0, bytesRead));
            }

            MessageBox.Show("qwe2");

            clientForm.Close();
            tcpClient.Close();         
            
        }



        private void recv(IAsyncResult res)
        {
            if (res == null) return;
            try
            {
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
                byte[] recived = this.EndReceive(res, ref groupEP);
                var dataRecived = Encoding.UTF8.GetString(recived, 0, recived.Length);


                

                if (dataRecived.StartsWith("USER_ONLINE:"))
                {
                    dataRecived = dataRecived.Remove(0, "USER_ONLINE:".Length);

                    User temp = User.Deserialize(dataRecived);
                    if (temp.nickName == "test")
                        online = false;
                    //Log(temp.nickName);
                    User alreadyInList = usersOnline.Find(x => x.ipAddress.Equals(groupEP.Address.ToString()));
                    if (alreadyInList == null)
                    {
                        usersOnline.AddItem(new User(groupEP.Address.ToString(), temp.nickName));
                        //Log("some one online");
                    }
                    else
                    {
                        alreadyInList.nickName = User.Deserialize(dataRecived).nickName;
                        usersOnline.UpdateItem(alreadyInList);
                    }
                }
                else if (dataRecived.StartsWith("USER_OFFLINE:"))
                {
                    dataRecived = dataRecived.Remove(0, "USER_OFFLINE:".Length);
                    usersOnline.RemoveItem(User.Deserialize(dataRecived));
                }
                else
                {
                    this.Log("{0} : {1}", groupEP.Address.ToString(), dataRecived);
                }
                
                this.BeginReceive(new AsyncCallback(recv), null);
            }
                // Если закрыли может вывалится ошибка, т.к. будет доступ к несущ. объекту
            catch (ObjectDisposedException) {}
                // Отлавливаем остальные ошибки
            catch (Exception ex) { MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace); }
        }
        public IPAddress GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIP = null;
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip;
                    break;
                }
            }
            return localIP;
        }
        // Получаем широковещательный айпи для подсети по используемому локальному
        public IPAddress GetBroadcastIP(IPAddress ip)
        {
            // Calculate broadcast address for current interface
            var addressInt = BitConverter.ToInt32(ip.GetAddressBytes(), 0);
            var maskInt = BitConverter.ToInt32(GetSubnetMask(ip).GetAddressBytes(), 0);
            var broadcastInt = addressInt | ~maskInt;
            return new IPAddress(BitConverter.GetBytes(broadcastInt));
        }

        private static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }


        private ManualResetEvent inviteEvent = null;
        private Thread inviteThread = null;
        private Thread listenThread = null;

        public ObservableList<User> usersOnline;
        public User user;

        private IPAddress broadCastIP, localIP;
        private int broadcastPort, privatePort;

        private TextBox logTextBox;
        
    }
}
