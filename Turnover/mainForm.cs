using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace Turnover
{
    public partial class mainForm : Form
    {
        string textBefore = string.Empty;
        GlobalServer globalServer = null;

        public class UserInfo
        {
            // Исходящее соединение
            public Socket remoteSocket { get; set; }
            // Входящее соединение
            public Client acceptedClient { get; set; }
            // Последний "инфо" пакет
            public Packet packet { get; set; }
        }

        ConcurrentDictionary<string, UserInfo> userInfos = new ConcurrentDictionary<string, UserInfo>();
        ConcurrentDictionary<string, Socket> userSockets = new ConcurrentDictionary<string, Socket>();
        IPAddress localIP;

        public mainForm()
        {
            InitializeComponent();
            try
            {
                localIP = GetLocalIP();

                nickNameBox.Text = Properties.Settings.Default.NickName;

                globalServer = new GlobalServer();
                globalServer.ReceivedMulticast += new GlobalServer.ClientReceivedMulticastHandler(globalServer_MulticastReceived);
                globalServer.ClientStatusOnline += new GlobalServer.ClientStatusOnlineChangedHandle(globalServer_ClientStatusOnline);
                globalServer.ClientStatusOffline += new GlobalServer.ClientStatusOfflineChangedHandle(globalServer_ClientStatusOffline);
                globalServer.AcceptedPM += new GlobalServer.SocketAcceptedHandler(globalServer_AcceptedPM);
                
                globalServer.Listen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                /*MessageBox.Show(this,
                                "Error in application configuration file!",
                                "Error Turnover", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);*/
            }
        }

        

        #region "Global Server Events"

        void globalServer_MulticastReceived(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                string formatted_data = string.Format("[{0}][{1}][{2}]", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine
                + Packet.encoding.GetString(p.data) + Environment.NewLine;
                chatBox.AppendText(formatted_data);
            });
        }

        void globalServer_ClientStatusOnline(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                string userIP = p.from.Address.ToString();

                ListViewItem lvi = FindListItem(usersOnline, userIP);
                if (lvi == null)
                {
                    ListViewItem lvItem = new ListViewItem();
                    lvItem.Text = userIP;
                    lvItem.Name = "lvi_" + userIP;
                    lvItem.ImageIndex = 0;
                    //lvItem.Tag = new UserInfo() {remoteSocket = 0, };
                    lvItem.SubItems.Add(p.NickName);
                    lvItem.SubItems.Add(p.privatePort.ToString());
                    usersOnline.Items.Add(lvItem);
                }
                else
                {
                    if (lvi.SubItems[1].Text.Equals(p.NickName) == false)
                        lvi.SubItems[1].Text = p.NickName;
                    if(lvi.SubItems[2].Text.Equals(p.privatePort.ToString()) == false)
                        lvi.SubItems[2].Text = p.privatePort.ToString();
                }

                /*if (userInfos.ContainsKey(userIP)) return;
                
                Socket remSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    MessageBox.Show("new socket");
                    remSocket.Connect(new IPEndPoint(p.from.Address, p.privatePort));
                    if (!remSocket.Connected) throw new Exception("Remote connection failure");
                    if (!userInfos.TryAdd(userIP, new UserInfo() { 
                        remoteSocket = remSocket,
                        acceptedClient = null,
                        packet = p})) throw new Exception("Error save user socket");
                            
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    remSocket.Shutdown(SocketShutdown.Both);
                    remSocket.Close();
                }

                ListViewItem lvi = FindListItem(usersOnline, packetFrom);

                if (lvi == null)
                {
                    Socket userSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        userSocket.Connect(new IPEndPoint(p.from.Address, p.privatePort));
                        if (!userSocket.Connected) throw new Exception("Connection failure");
                        
                        ListViewItem lvItem = new ListViewItem();
                        lvItem.Text = p.from.Address.ToString();
                        lvItem.Name = "lvi_" + p.from.Address.ToString();
                        lvItem.ImageIndex = 0;
                        lvItem.Tag = userSocket;
                        lvItem.SubItems.Add(p.NickName);
                        lvItem.SubItems.Add(p.privatePort.ToString());
                        usersOnline.Items.Add(lvItem);
                    }
                    catch
                    {
                        userSocket.Shutdown(SocketShutdown.Both);
                        userSocket.Close();
                    }
                }
                else
                {
                    if (lvi.SubItems[1].Text.Equals(p.NickName) == false) lvi.SubItems[1].Text = p.NickName;
                    //Socket userSocket = lvi.Tag as Socket;
                    //lvi.SubItems[2].Text = p.privatePort.ToString();
                }
                */

                /*if (!clientSockets.ContainsKey(packetFrom))
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSockets.Add(packetFrom, s);
                    try
                    {
                        s.Connect(new IPEndPoint(p.from.Address, Properties.Settings.Default.privatePort));

                        for (int i = 0; i < usersOnline.Items.Count; ++i)
                        {
                            Packet packet = usersOnline.Items[i].Tag as Packet;

                            if (packet.from.Address.Equals(p.from.Address))
                            {
                                usersOnline.Items[i].SubItems[1].Text = p.NickName;
                                usersOnline.Items[i].SubItems[2].Text = p.privatePort.ToString();
                                return;
                            }
                        }

                        ListViewItem lvi = new ListViewItem();
                        lvi.Text = p.from.Address.ToString();
                        lvi.ImageIndex = 0;
                        lvi.Tag = p;
                        lvi.SubItems.Add(p.NickName);
                        lvi.SubItems.Add(p.privatePort.ToString());
                        usersOnline.Items.Add(lvi);
                    }
                    catch
                    {
                        clientSockets[packetFrom].Close();
                        clientSockets.Remove(packetFrom);
                    }
                }*/
            });            
        }

        void globalServer_ClientStatusOffline(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                ListViewItem lvi = FindListItem(usersOnline, p.from.Address.ToString());
                if (lvi != null) usersOnline.Items.Remove(lvi);
            });
        }

        void globalServer_AcceptedPM(Socket e)
        {
            Client client = new Client(e);
            client.Received += client_DataReceived;
            client.Disconnected += client_Disconnected;

            Invoke((MethodInvoker)delegate
            {
                string userIP = client.EndPoint.Address.ToString();
                
                if (userInfos.ContainsKey(userIP))
                {
                    UserInfo user = userInfos[userIP];
                    user.acceptedClient = client;

                    try
                    {
                        // Если исходящее соединение уже установлено
                        if (!user.remoteSocket.Connected) throw new Exception("Connection failure");

                        ListViewItem lvItem = new ListViewItem();
                        lvItem.Text = userIP;
                        lvItem.Name = "lvi_user_info_" + userIP; 
                        lvItem.ImageIndex = 0;
                        lvItem.Tag = user;
                        lvItem.SubItems.Add(user.packet.NickName);
                        lvItem.SubItems.Add(user.packet.privatePort.ToString());
                        usersOnline.Items.Add(lvItem);
                    }
                    catch
                    {
                        userInfos.TryRemove(userIP, out user);
                        user.remoteSocket.Shutdown(SocketShutdown.Both);
                        user.remoteSocket.Close();

                        user.acceptedClient.Close();
                    }
                }

                /*if (chatTabs.TabPages.ContainsKey(userIP)) return;

                TabPage page = new TabPage(userIP);
                page.Name = userIP;
                page.Tag = client;

                TextBox privateBox = new TextBox();
                privateBox.Name = userIP;
                privateBox.Multiline = true;
                privateBox.ReadOnly = true;
                privateBox.BackColor = Color.White;
                privateBox.Location = new Point(6, 7);
                privateBox.ScrollBars = ScrollBars.Vertical;
                privateBox.Size = new System.Drawing.Size(555, 365);
                page.Controls.Add(privateBox);

                chatTabs.TabPages.Add(page);*/

                /*for (int i = 0; i < usersOnline.Items.Count; ++i)
                {
                    Client c = usersOnline.Items[i].Tag as Client;

                    if (c.EndPoint.Address.Equals(ep.Address))
                    {
                        usersOnline.Items[i].SubItems[1].Text = p.NickName;
                        usersOnline.Items[i].SubItems[2].Text = (p.privateIPEndPoint == null) ? "null" : p.privateIPEndPoint.Address.ToString();
                        return;
                    }
                }

                ListViewItem lvi = new ListViewItem();
                lvi.Text = client.EndPoint.Address.ToString();
                lvi.ImageIndex = 0;
                lvi.Tag = client;
                lvi.SubItems.Add(p.NickName);
                lvi.SubItems.Add((p.privateIPEndPoint == null) ? "null" : p.privateIPEndPoint.ToString());
                usersOnline.Items.Add(lvi);*/
            });
        }

        #endregion


        void client_DataReceived(Client sender, Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                TabPage page = chatTabs.TabPages[sender.EndPoint.Address.ToString()];
                TextBox privateBox = page.Controls[sender.EndPoint.Address.ToString()] as TextBox;

                string formatted_data = string.Format("[{0}][{1}][{2}] ", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine
                + Packet.encoding.GetString(p.data) + Environment.NewLine;
                privateBox.AppendText(formatted_data);                
            });
        }

        void client_Disconnected(Client sender)
        {
            Invoke((MethodInvoker)delegate
            {
                try
                {
                    string userIP = sender.EndPoint.Address.ToString();

                    if (userInfos.ContainsKey(userIP))
                    {
                        UserInfo user = null;
                        userInfos.TryRemove(userIP, out user);
                        user.remoteSocket.Shutdown(SocketShutdown.Both);
                        user.remoteSocket.Close();

                        ListViewItem lvi = FindListItem(usersOnline, userIP);
                        if (lvi != null)
                            usersOnline.Items.Remove(lvi);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                MessageBox.Show("Disconnected");
            });
        }

        

        

        

        private void usersOnline_DoubleClick(object sender, EventArgs e)
        {
            ListView listView = (sender as ListView);
            if (listView.SelectedItems.Count == 1)
            {
                ListViewItem lvi = listView.SelectedItems[0];
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(new IPEndPoint(IPAddress.Parse(lvi.Text), int.Parse(lvi.SubItems[2].Text)));
                    if(!socket.Connected) throw new Exception("Connect failure.");

                    if (!userSockets.TryAdd(lvi.Text, socket)) throw new Exception("Save socket failure");

                    TabPage privatePage = new TabPage(lvi.Text);
                    privatePage.Name = "page_" + lvi.Text;
                    privatePage.Tag = socket;

                    TextBox privateBox = new TextBox();
                    privateBox.Name = "text_box_" + lvi.Text;
                    privateBox.Multiline = true;
                    privateBox.ReadOnly = true;
                    privateBox.BackColor = Color.White;
                    privateBox.Location = new Point(6, 7);
                    privateBox.ScrollBars = ScrollBars.Vertical;
                    privateBox.Size = new System.Drawing.Size(555, 365);
                    privatePage.Controls.Add(privateBox);

                    chatTabs.TabPages.Add(privatePage);
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }

            }
        }

        private void usersOnline_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*if (usersOnline.SelectedItems.Count == 1)
            {
                msgBox.AppendText(usersOnline.SelectedItems[0].ToString() + Environment.NewLine);
            } */  
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                string message = msgBox.Text;
                
                Packet packet = new Packet(MSG_TYPE.MESSAGE, Packet.encoding.GetBytes(message),
                    Properties.Settings.Default.NickName,
                    Properties.Settings.Default.privatePort);

                if (chatTabs.SelectedIndex == 0)
                {
                    globalServer.SendMulticastMessage(packet);
                    msgBox.Clear();
                    msgBox.Focus();
                    return;
                }
                TabPage selectedPage = chatTabs.SelectedTab;
                string userIP = selectedPage.Text;

                Socket socket = selectedPage.Tag as Socket;

                globalServer.SendPrivateMessage(socket, packet);

                /*string tabName = selectedPage.Name;
                if (userInfos.ContainsKey(tabName)
                    && userInfos.ContainsKey(tabName)
                    && userInfos[tabName].Connected)
                {
                    globalServer.SendPrivateMessage(userInfos[tabName], packet);

                    packet.from = (IPEndPoint)userInfos[tabName].RemoteEndPoint;

                    TextBox privateBox = selectedPage.Controls[tabName] as TextBox;

                    string formatted_data = string.Format("[{0}][{1}][{2}] ", packet.from.Address, DateTime.Now, packet.NickName) + Environment.NewLine
                    + Packet.encoding.GetString(packet.data) + Environment.NewLine;
                    privateBox.AppendText(formatted_data);
                }
                else
                    MessageBox.Show("Пользователь вне сети");
                 */
            });
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            globalServer.StopGlobalListener();
            //privateServer.Stop();
        }

        private ListViewItem FindListItem(ListView lv, string userIP)
        {
            for (int i = 0; i < lv.Items.Count; ++i)
                if (lv.Items[i].Name.Equals("lvi_" + userIP)) return lv.Items[i];
            return null;
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

        #region MessageBox, NicknameBox
        private void msgBox_TextChanged(object sender, EventArgs e)
        {
            btn_send.Enabled = msgBox.Text.Length > 0;
        }

        private void msgBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btn_send.PerformClick();
            }
        }

        private void nickNameBox_Enter(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            nickNameBox.ReadOnly = false;
            box.BorderStyle = BorderStyle.Fixed3D;
            box.BackColor = Color.White;
            box.ForeColor = Color.Black;
            textBefore = box.Text;
        }
        
        private void nickNameBox_Leave(object sender, EventArgs e)
        {
            TextBox box = sender as TextBox;
            nickNameBox.ReadOnly = true;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = SystemColors.Control;
            box.ForeColor = Color.Green;

            KeyEventArgs keyArgs = e as KeyEventArgs;

            if (box.Text.Length == 0 || (keyArgs != null && keyArgs.KeyCode == Keys.Escape))
            {
                box.Text = textBefore;
            }
            else
            {
                Properties.Settings.Default.NickName = box.Text;
                Properties.Settings.Default.Save();
            }
            msgBox.Focus();
        }        

        private void nickNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = sender as TextBox;

            if (box.Focused && (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter))
            {
                nickNameBox_Leave(box, e);
                e.Handled = true;
            }
        }
        #endregion
    }
}
