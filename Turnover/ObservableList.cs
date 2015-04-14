using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Turnover
{
    public class ObservableList<T> : List<T>
    {
        private object _sync = new object();

        public ObservableList(ListView listView)
        {
            OnAdd += new EventHandler(l_OnAdd);
            OnRemove += new EventHandler(l_OnRemove);
            OnClear += new EventHandler(l_OnClear);
            this.listView = listView;
        }

        private EventHandler OnAdd, OnRemove, OnClear;
        private ListView listView;

        public void UpdateItem(T item)
        {
            User u = item as User;
            lock (_sync)
            {
                listView.Invoke(new Action(() =>
                {
                    for (int i = 0; i < listView.Items.Count; ++i)
                    {
                        if (listView.Items[i].Text.Equals(u.ipAddress))
                        {
                            listView.Items[i].SubItems[1].Text = u.nickName;

                            break;
                        }
                    }
                }));

            }
        }

        public void AddItem(T item)
        {
            lock (_sync)
            {
                if (OnAdd != null)
                    OnAdd(item, null);
                base.Add(item);
            }
        }
        public void RemoveItem(T item)
        {
            lock (_sync)
            {
                if (OnRemove != null)
                    OnRemove(item, null);
                base.Remove(item);
            }
        }
        public void ClearList()
        {
            lock (_sync)
            {
                if (OnClear != null)
                    OnClear(this, null);
                base.Clear();
            }
        }

        private void l_OnAdd(object sender, EventArgs e)
        {
            User user = (User)sender;
            ListViewItem lvi = new ListViewItem();

            lvi.Text = user.ipAddress;
            lvi.SubItems.Add(user.nickName);
            
            lvi.ImageIndex = 0;
            lvi.Tag = user;

            listView.Invoke(new Action(() =>
            {
                listView.Items.Add(lvi);
            }));
            
        }

        private void l_OnRemove(object sender, EventArgs e)
        {
            User user = (User)sender;
            ListViewItem lvi = new ListViewItem();
            lvi.Text = user.ipAddress;
  

            listView.Invoke(new Action(() =>
            {
                for (int i = 0; i < listView.Items.Count; ++i)
                {
                    if (listView.Items[i].Text.Equals( user.ipAddress ))
                    {
                        listView.Items.RemoveAt(i);
                        break;
                    }
                }
                
            }));
            //MessageBox.Show("Element removed...");
        }
        private void l_OnClear(object sender, EventArgs e)
        {
            //MessageBox.Show("List cleared...");
            listView.Invoke(new Action(() =>
            {
                listView.Items.Clear();
            }));
        }

    }
}
