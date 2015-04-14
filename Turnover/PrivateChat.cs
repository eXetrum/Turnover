using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Turnover
{
    public partial class PrivateChat : Form
    {
        Encoding encoding = new UTF8Encoding();
        UDPListener listener = null;
        public PrivateChat(UDPListener listener)
        {
            this.listener = listener;
            InitializeComponent();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            /*byte[] buffer = encoding.GetBytes(txtInput.Text);            
            IPEndPoint remote_ep = new IPEndPoint(IPAddress.Any, 9050);
            listener.listener.BeginSend(buffer, new AsyncCallback(sendCallback), listener.listener);*/
        }

        private void sendCallback(IAsyncResult ar)
        {
        }
    }
}
