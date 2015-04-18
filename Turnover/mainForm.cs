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
        const int MAX_PATH = 260;

        string textBefore = string.Empty;
        GlobalServer globalServer = null;

        IPAddress localIP;
        ConcurrentDictionary<string, Client> peers = new ConcurrentDictionary<string, Client>();

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
                MessageBox.Show("init: " + ex.Message + Environment.NewLine + ex.StackTrace);
                //MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                /*MessageBox.Show(this,
                                "Error in application configuration file!",
                                "Error Turnover", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);*/
                Application.Exit();
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
                    lvItem.Text = p.NickName;
                    lvItem.Name = "lvi_" + userIP;
                    lvItem.Tag = p;
                    lvItem.ImageIndex = 0;
                    usersOnline.Items.Add(lvItem);
                }
                else
                {
                    Packet pack = lvi.Tag as Packet;
                    if (pack.NickName.Equals(p.NickName) == false || pack.privatePort.Equals(p.privatePort) == false)
                    {
                        lvi.Text = p.NickName;
                        lvi.Tag = p;
                    }
                }
            });            
        }

        void globalServer_ClientStatusOffline(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                ListViewItem lvi = FindListItem(usersOnline, p.from.Address.ToString());
                if (lvi != null)
                {
                    usersOnline.Items.Remove(lvi);
                }
            });
        }
        
        void globalServer_AcceptedPM(Socket e)
        {
            try
            {
                Client client = new Client(e);
                client.Received += client_DataReceived;
                client.Disconnected += client_Disconnected;

                string userIP = client.EndPoint.Address.ToString();

                peers.TryAdd(userIP, client);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion


        void client_DataReceived(Client sender, Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                string userIP = sender.EndPoint.Address.ToString();
                try
                {
                    switch (p.msgType)
                    {
                        case MSG_TYPE.MESSAGE:
                            {
                                TabPage page = GetPage(userIP);
                                LogBox privateBox = page.Controls["privateBox_" + userIP] as LogBox;

                                string formatted_data = string.Format("[{0}][{1}][{2}] ", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine
                                + Packet.encoding.GetString(p.data) + Environment.NewLine;
                                privateBox.Append(formatted_data);
                            }
                            break;
                        case MSG_TYPE.FILE:
                            {
                                TabPage page = GetPage(userIP);
                                LogBox privateBox = page.Controls["privateBox_" + userIP] as LogBox;

                                byte[] fileName = new byte[MAX_PATH];
                                byte[] fileData = new byte[p.data.Length - MAX_PATH];

                                Buffer.BlockCopy(p.data, 0, fileName, 0, MAX_PATH);
                                Buffer.BlockCopy(p.data, MAX_PATH, fileData, 0, p.data.Length - MAX_PATH);


                                /*string formatted_data = string.Format("[{0}][{1}][{2}] ", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine
                                + Packet.encoding.GetString(p.data) + Environment.NewLine;
                                privateBox.AppendText(formatted_data);*/


                                // Get the current directory. 
                                string path = Directory.GetCurrentDirectory();
                                string target = @"Received Documents";
                                path = Path.Combine(path, target);
                                //Console.WriteLine("The current directory is {0}", path);
                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }


                                string fName = Packet.encoding.GetString(fileName).TrimEnd(new char[] {(char)0 });
                                path = Path.Combine(path, fName);
                                
                                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                                {
                                    fs.Write(fileData, 0, fileData.Length);                                    
                                }


                                privateBox.Append("Принят файл: " + path + Environment.NewLine);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Data received: " + ex.Message);
                }
                
            });
        }

        void client_Disconnected(Client sender)
        {
            Invoke((MethodInvoker)delegate
            {
                try
                {
                    string userIP = sender.EndPoint.Address.ToString();
                    Client client;
                    if (peers.TryRemove(userIP, out client) == false) throw new Exception("peer TryRemove failure");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("client_Disconnected event: " + ex.Message);
                }                
            });
        }


        private void usersOnline_DoubleClick(object sender, EventArgs e)
        {
            ListView listView = (sender as ListView);
            if (listView.SelectedItems.Count == 1)
            {
                ListViewItem lvi = listView.SelectedItems[0];

                string userIP = lvi.Name.Substring(4);

                TabPage privatePage = GetPage(userIP);
                chatTabs.SelectTab(privatePage);
            }
        }

        private void chatTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            msgBox.Focus();
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                // Получаем сообщение
                string message = msgBox.Text;
                // Формируем пакет
                Packet packet = new Packet(MSG_TYPE.MESSAGE, Packet.encoding.GetBytes(message),
                    Properties.Settings.Default.NickName,
                    Properties.Settings.Default.privatePort);
                // Если выбрана первая вкладка - вещаем в общий чат
                if (chatTabs.SelectedIndex == 0)
                {
                    globalServer.SendMulticastMessage(packet);
                    msgBox.Clear();
                    msgBox.Focus();
                    return;
                }
                // Иначе отправляем сообщение указанному пользователю
                TabPage selectedPage = chatTabs.SelectedTab;
                string userIP = selectedPage.Name.Substring(12);
                LogBox privateBox = selectedPage.Controls["privateBox_" + userIP] as LogBox;
                ListViewItem lvi = FindListItem(usersOnline, userIP);
                if(lvi == null)
                {
                    MessageBox.Show("Пользователь вне сети");
                    return;
                }

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    Packet p = lvi.Tag as Packet;
                    socket.Connect(new IPEndPoint(p.from.Address, p.privatePort));
                    if (socket.Connected)
                    {
                        string formatted_data = string.Format("[{0}][{1}][{2}] ", "0"/*packet.from.Address*/, DateTime.Now, packet.NickName) + Environment.NewLine
                            + Packet.encoding.GetString(packet.data) + Environment.NewLine;
                        privateBox.Append(formatted_data);

                        globalServer.SendPrivateMessage(socket, packet);
                        msgBox.Clear();
                        msgBox.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Пользователь вне сети");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Disconnect(false);
                    socket.Close();
                }

            });
        }

        private TabPage GetPage(string userIP)
        {
            ListViewItem lvi = FindListItem(usersOnline, userIP);

            string userName = lvi.Text;

            TabPage privatePage = chatTabs.TabPages["privatePage_" + userIP];
            if (privatePage != null) return privatePage;

            privatePage = new TabPage(userName);
            privatePage.Name = "privatePage_" + userIP;

            LogBox privateBox = new LogBox(userIP);
            privateBox.Location = new Point(7, 7);
            privateBox.Size = new System.Drawing.Size(555, 365);

            privatePage.Controls.Add(privateBox);
            chatTabs.TabPages.Add(privatePage);

            return privatePage;
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

        #region MessageBox, NicknameBox, Form

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

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            globalServer.StopGlobalListener();
        }

        #endregion

        #region Menu items
        private void userMenu_Opening(object sender, CancelEventArgs e)
        {
            if (usersOnline.SelectedItems.Count <= 0)
                e.Cancel = true;
        }

        private void отправитьСообщениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (usersOnline.SelectedItems.Count == 1)
            {
                ListViewItem lvi = usersOnline.SelectedItems[0];
                string userIP = lvi.Name.Substring(4);
                TabPage privatePage = GetPage(userIP);
                chatTabs.SelectTab(privatePage);
            }
        }

        private void отправитьФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (usersOnline.SelectedItems.Count == 1)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                byte[] temp = Packet.encoding.GetBytes(ofd.SafeFileName);
                byte[] fileName = new byte[MAX_PATH];

                if (temp.Length > MAX_PATH)
                {
                    MessageBox.Show("Слишком длинное имя файла");
                    return;
                }
                Array.Copy(temp, fileName, temp.Length);

                ListViewItem lvi = usersOnline.SelectedItems[0];
                Packet packet = lvi.Tag as Packet;
                string userIP = lvi.Name.Substring(4);
                TabPage privatePage = GetPage(userIP);
                chatTabs.SelectTab(privatePage);

                byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
                byte[] data = new byte[MAX_PATH + fileBytes.Length];


                Buffer.BlockCopy(fileName, 0, data, 0, MAX_PATH);
                Buffer.BlockCopy(fileBytes, 0, data, MAX_PATH, fileBytes.Length);

                try
                {
                    using (Socket sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        sc.Connect(new IPEndPoint(packet.from.Address, packet.privatePort));
                        //MessageBox.Show(data.Length.ToString());
                        Packet p = new Packet(MSG_TYPE.FILE, data, Properties.Settings.Default.NickName, Properties.Settings.Default.privatePort);
                        globalServer.SendPrivateMessage(sc, p);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void посмотретьСетевыеДанныеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (usersOnline.SelectedItems.Count == 1)
            {
                ListViewItem lvi = usersOnline.SelectedItems[0];
                Packet packet = lvi.Tag as Packet;
                
                Form form = new Form();
                Label lblIP = new Label();
                Label lblPrivatePort = new Label();

                int width = 180;
                int height = 20;
                form.Text = "Username: " + packet.NickName;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
                form.Size = new System.Drawing.Size(width, 120);
                form.StartPosition = FormStartPosition.CenterParent;

                lblIP.Size = new System.Drawing.Size(width, height);
                lblIP.Location = new Point(10, 15);
                lblIP.Text = "IP address: " + packet.from.Address.ToString();

                lblPrivatePort.Size = new System.Drawing.Size(width, height);
                lblPrivatePort.Location = new Point(10, 45);
                lblPrivatePort.Text = "Port for pm messages: " + packet.privatePort.ToString();

                form.Controls.Add(lblIP);
                form.Controls.Add(lblPrivatePort);
                form.ShowDialog();
            }
        }
        #endregion

        private void chatTabs_DoubleClick(object sender, EventArgs e)
        {
            if (chatTabs.SelectedIndex != 0)
            {
                chatTabs.SelectedTab.Dispose();
            }            
        }

    }
}
