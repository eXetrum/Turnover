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

namespace Turnover
{
    public partial class mainForm : Form
    {
        string textBefore = string.Empty;
        GlobalServer globalServer = null;
        PMServer privateServer = null;

        List<Client> clients = new List<Client>();

        public mainForm()
        {
            InitializeComponent();
            try
            {
                nickNameBox.Text = Properties.Settings.Default.NickName;

                globalServer = new GlobalServer();
                globalServer.Received += new GlobalServer.ClientReceivedHandler(globalServer_Received);
                globalServer.ClientStatusOnline += new GlobalServer.ClientStatusOnlineChangedHandle(globalServer_ClientStatusOnline);
                globalServer.ClientStatusOffline += new GlobalServer.ClientStatusOfflineChangedHandle(globalServer_ClientStatusOffline);
                globalServer.Listen();

                privateServer = new PMServer();
                privateServer.Accepted += new PMServer.SocketAcceptedHandler(server_Accepted);
                privateServer.Start(Properties.Settings.Default.privatePort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show(this,
                                "Error in application configuration file!",
                                "Error Turnover", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        #region "Global Server Events"
        void globalServer_Received(IPEndPoint ep, Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                string formatted_data = string.Format("[{0}][{1}] ", ep.ToString(), p.NickName) + Packet.encoding.GetString(p.data);
                chatBox.AppendText(formatted_data + Environment.NewLine);
            });
        }

        void globalServer_ClientStatusOnline(IPEndPoint ep, Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < usersOnline.Items.Count; ++i)
                {
                    IPEndPoint itemEP = usersOnline.Items[i].Tag as IPEndPoint;

                    if (itemEP.Equals(ep))
                    {
                        usersOnline.Items[i].SubItems[1].Text = p.NickName;
                        usersOnline.Items[i].SubItems[2].Text = (p.privateIPEndPoint == null) ? "null" : p.privateIPEndPoint.ToString();
                        return;
                    }
                }

                ListViewItem lvi = new ListViewItem();
                lvi.Text = ep.ToString();
                lvi.ImageIndex = 0;
                lvi.Tag = ep;
                lvi.SubItems.Add(p.NickName);
                lvi.SubItems.Add((p.privateIPEndPoint == null) ? "null" : p.privateIPEndPoint.ToString());
                usersOnline.Items.Add(lvi);
            });            
        }

        void globalServer_ClientStatusOffline(IPEndPoint sender)
        {
            Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < usersOnline.Items.Count; ++i)
                {
                    IPEndPoint ep = usersOnline.Items[i].Tag as IPEndPoint; 

                    if (ep.Equals(sender))
                    {
                        usersOnline.Items.RemoveAt(i);
                        break;
                    }
                }
            });
        }
        #endregion

        void server_Accepted(Socket e)
        {
            Client client = new Client(e);
            client.Received += client_DataReceived;
            client.Disconnected += client_Disconnected;

            Invoke((MethodInvoker)delegate
            {
                TabPage page = new TabPage(client.EndPoint.ToString());
                page.Name = client.EndPoint.ToString();
                TextBox privateBox = new TextBox();

                privateBox.Name = client.EndPoint.ToString();
                privateBox.Multiline = true;
                privateBox.ReadOnly = true;
                privateBox.BackColor = Color.White;
                privateBox.Location = new Point(6, 7);
                privateBox.ScrollBars = ScrollBars.Vertical;
                privateBox.Size = new System.Drawing.Size(555, 365);
                page.Controls.Add(privateBox);

                chatTabs.TabPages.Add(page);
                privateBox.AppendText("Connected: " + client.EndPoint.ToString() + Environment.NewLine);
            });

        }

        void client_DataReceived(Client sender, byte []data)
        {
            Invoke((MethodInvoker)delegate
            {
                TabPage page = chatTabs.TabPages[sender.EndPoint.ToString()];
                TextBox privateBox = page.Controls[sender.EndPoint.ToString()] as TextBox;
                privateBox.AppendText(Client.encoding.GetString(data) + Environment.NewLine);
            });
        }

        void client_Disconnected(Client sender)
        {
            MessageBox.Show("Disconnected");
        }

        

        

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            globalServer.StopGlobalListener();
            privateServer.Stop();
        }

        private void usersOnline_DoubleClick(object sender, EventArgs e)
        {
            ListView listView = (sender as ListView);
            if (listView.SelectedItems.Count == 1)
            {//display the text of selected item

                MessageBox.Show(listView.SelectedItems[0].Text);

            }
        }

        private void usersOnline_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (usersOnline.SelectedItems.Count == 1)
            {
                msgBox.AppendText(usersOnline.SelectedItems[0].ToString() + Environment.NewLine);
            }   
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            string message = msgBox.Text;
            msgBox.Clear();
            msgBox.Focus();
            Packet packet = new Packet(MSG_TYPE.MESSAGE, Packet.encoding.GetBytes(message),
                Properties.Settings.Default.NickName, null);

            globalServer.SendMessage(packet);
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
