using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Turnover
{
    public partial class mainForm : Form
    {
        UDPListener listener = null;
        string textBefore = string.Empty;

        public mainForm()
        {
            InitializeComponent();
            try
            {
                nickNameBox.Text = Properties.Settings.Default.NickName;

                listener = new UDPListener(Properties.Settings.Default.BroadcastPort,
                    Properties.Settings.Default.PrivatePort,
                    Properties.Settings.Default.NickName,
                    this,
                    chatBox, 
                    usersOnline);

                listener.StartListen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show(this,
                                "Error in application configuration file!",
                                "Error Turnover", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }


            //usersOnline.LargeImageList = userIcons;
            usersOnline.View = View.Details;
            usersOnline.StateImageList = userIcons;
        }

        private void btn_scan_Click(object sender, EventArgs e)
        {
            listener.SendInvites();
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            listener.StopListener();
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
            listener.SendMessage(message);
        }

        private void msgBox_TextChanged(object sender, EventArgs e)
        {
            btn_send.Enabled = msgBox.Text.Length > 0;
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

            if (box.Text.CompareTo(textBefore) != 0 && box.Text.Length > 0)
            {
                if(keyArgs != null)
                    if (keyArgs.KeyCode == Keys.Enter)
                    {
                        listener.user.nickName = box.Text;
                        Properties.Settings.Default.NickName = box.Text;
                        Properties.Settings.Default.Save();
                    }
                    else
                        box.Text = textBefore;
            }
            else
            {
                box.Text = textBefore;
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

        private void usersOnline_DoubleClick(object sender, EventArgs e)
        {
            ListView listView = (sender as ListView);
            if (listView.SelectedItems.Count == 1)
            {//display the text of selected item

                MessageBox.Show(listView.SelectedItems[0].Text);

            }
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

        
    }
}
