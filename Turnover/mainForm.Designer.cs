﻿using System.Windows.Forms;
namespace Turnover
{
    partial class mainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mainForm));
            this.chatBox = new System.Windows.Forms.TextBox();
            this.usersOnline = new System.Windows.Forms.ListView();
            this.userIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.userNickName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.userIcons = new System.Windows.Forms.ImageList(this.components);
            this.msgBox = new System.Windows.Forms.TextBox();
            this.btn_send = new System.Windows.Forms.Button();
            this.nickNameBox = new System.Windows.Forms.TextBox();
            this.chatTabs = new System.Windows.Forms.TabControl();
            this.tabGlobalChat = new System.Windows.Forms.TabPage();
            this.userPrivateIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chatTabs.SuspendLayout();
            this.tabGlobalChat.SuspendLayout();
            this.SuspendLayout();
            // 
            // chatBox
            // 
            this.chatBox.BackColor = System.Drawing.Color.White;
            this.chatBox.Location = new System.Drawing.Point(6, 7);
            this.chatBox.Multiline = true;
            this.chatBox.Name = "chatBox";
            this.chatBox.ReadOnly = true;
            this.chatBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.chatBox.Size = new System.Drawing.Size(555, 365);
            this.chatBox.TabIndex = 0;
            // 
            // usersOnline
            // 
            this.usersOnline.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.userIP,
            this.userNickName,
            this.userPrivateIP});
            this.usersOnline.FullRowSelect = true;
            this.usersOnline.GridLines = true;
            this.usersOnline.LargeImageList = this.userIcons;
            this.usersOnline.Location = new System.Drawing.Point(12, 12);
            this.usersOnline.MultiSelect = false;
            this.usersOnline.Name = "usersOnline";
            this.usersOnline.Size = new System.Drawing.Size(384, 509);
            this.usersOnline.SmallImageList = this.userIcons;
            this.usersOnline.StateImageList = this.userIcons;
            this.usersOnline.TabIndex = 3;
            this.usersOnline.UseCompatibleStateImageBehavior = false;
            this.usersOnline.View = System.Windows.Forms.View.Details;
            this.usersOnline.SelectedIndexChanged += new System.EventHandler(this.usersOnline_SelectedIndexChanged);
            this.usersOnline.DoubleClick += new System.EventHandler(this.usersOnline_DoubleClick);
            // 
            // userIP
            // 
            this.userIP.Text = "userIP";
            this.userIP.Width = 120;
            // 
            // userNickName
            // 
            this.userNickName.Text = "userNickName";
            this.userNickName.Width = 140;
            // 
            // userIcons
            // 
            this.userIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("userIcons.ImageStream")));
            this.userIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.userIcons.Images.SetKeyName(0, "compIMG.png");
            // 
            // msgBox
            // 
            this.msgBox.Location = new System.Drawing.Point(402, 434);
            this.msgBox.Multiline = true;
            this.msgBox.Name = "msgBox";
            this.msgBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.msgBox.Size = new System.Drawing.Size(475, 87);
            this.msgBox.TabIndex = 0;
            this.msgBox.TextChanged += new System.EventHandler(this.msgBox_TextChanged);
            this.msgBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.msgBox_KeyDown);
            // 
            // btn_send
            // 
            this.btn_send.Enabled = false;
            this.btn_send.Location = new System.Drawing.Point(883, 434);
            this.btn_send.Name = "btn_send";
            this.btn_send.Size = new System.Drawing.Size(92, 87);
            this.btn_send.TabIndex = 1;
            this.btn_send.Text = "Send";
            this.btn_send.UseVisualStyleBackColor = true;
            this.btn_send.Click += new System.EventHandler(this.btn_send_Click);
            // 
            // nickNameBox
            // 
            this.nickNameBox.BackColor = System.Drawing.SystemColors.Control;
            this.nickNameBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.nickNameBox.ForeColor = System.Drawing.Color.Green;
            this.nickNameBox.Location = new System.Drawing.Point(871, 10);
            this.nickNameBox.Name = "nickNameBox";
            this.nickNameBox.ReadOnly = true;
            this.nickNameBox.Size = new System.Drawing.Size(100, 13);
            this.nickNameBox.TabIndex = 5;
            this.nickNameBox.Text = "Nickname";
            this.nickNameBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nickNameBox.Enter += new System.EventHandler(this.nickNameBox_Enter);
            this.nickNameBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.nickNameBox_KeyDown);
            this.nickNameBox.Leave += new System.EventHandler(this.nickNameBox_Leave);
            // 
            // chatTabs
            // 
            this.chatTabs.Controls.Add(this.tabGlobalChat);
            this.chatTabs.Location = new System.Drawing.Point(402, 28);
            this.chatTabs.Name = "chatTabs";
            this.chatTabs.SelectedIndex = 0;
            this.chatTabs.Size = new System.Drawing.Size(573, 404);
            this.chatTabs.TabIndex = 6;
            // 
            // tabGlobalChat
            // 
            this.tabGlobalChat.Controls.Add(this.chatBox);
            this.tabGlobalChat.Location = new System.Drawing.Point(4, 22);
            this.tabGlobalChat.Name = "tabGlobalChat";
            this.tabGlobalChat.Padding = new System.Windows.Forms.Padding(3);
            this.tabGlobalChat.Size = new System.Drawing.Size(565, 378);
            this.tabGlobalChat.TabIndex = 0;
            this.tabGlobalChat.Text = "Global Chat";
            this.tabGlobalChat.UseVisualStyleBackColor = true;
            // 
            // userPrivateIP
            // 
            this.userPrivateIP.Text = "userPrivateIP";
            this.userPrivateIP.Width = 120;
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(987, 525);
            this.Controls.Add(this.chatTabs);
            this.Controls.Add(this.nickNameBox);
            this.Controls.Add(this.btn_send);
            this.Controls.Add(this.msgBox);
            this.Controls.Add(this.usersOnline);
            this.Name = "mainForm";
            this.Text = "Turnover";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mainForm_FormClosed);
            this.chatTabs.ResumeLayout(false);
            this.tabGlobalChat.ResumeLayout(false);
            this.tabGlobalChat.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox chatBox;
        private System.Windows.Forms.ListView usersOnline;
        private TextBox msgBox;
        private TextBox nickNameBox;
        private Button btn_send;        
        public ImageList userIcons;
        private ColumnHeader userIP;
        private ColumnHeader userNickName;
        private TabControl chatTabs;
        private TabPage tabGlobalChat;
        private ColumnHeader userPrivateIP;
    }
}

