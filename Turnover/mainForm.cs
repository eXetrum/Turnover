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
        // Максимальная длинна пути для файла
        const int MAX_PATH = 260;
        // Строка текста которую запоминаем перед редактированием никнейма
        string textBefore = string.Empty;
        // Объект сервера
        GlobalServer globalServer = null;
        // Необязательное сохранение приватных подключений
        ConcurrentDictionary<string, Client> peers = new ConcurrentDictionary<string, Client>();

        public mainForm()
        {
            InitializeComponent();
            try
            {
                // Получаем никнейм из настроек
                nickNameBox.Text = Properties.Settings.Default.NickName;
                // Создаем объект сервера
                globalServer = new GlobalServer();
                // Создаем обработчики событий
                // Прием UDP данных
                globalServer.ReceivedMulticast += new GlobalServer.ClientReceivedMulticastHandler(globalServer_MulticastReceived);
                // Прием сообщения "онлайн"
                globalServer.ClientStatusOnline += new GlobalServer.ClientStatusOnlineChangedHandle(globalServer_ClientStatusOnline);
                // Прием сообщения "оффлайн"
                globalServer.ClientStatusOffline += new GlobalServer.ClientStatusOfflineChangedHandle(globalServer_ClientStatusOffline);
                // Прием подключений на порт для обмена приватными сообщениями
                globalServer.AcceptedPM += new GlobalServer.SocketAcceptedHandler(globalServer_AcceptedPM);
                // Запускаем сервер
                globalServer.Listen();
            }
                // Отлавливаем ошибки
            catch (Exception ex)
            {
                // Покажем текст ошибки
                MessageBox.Show(this,
                                "init: " + ex.Message + Environment.NewLine + ex.StackTrace,
                                "Error Turnover", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                // Завершаем приложение если произошла ошибка
                Application.Exit();
            }
        }        

        #region "Global Server Events"
        // Обработчик события приема UDP данных от группы
        void globalServer_MulticastReceived(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                // Формируем строку для добавления
                string formatted_data = string.Format("[{0}][{1}][{2}]", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine + Packet.encoding.GetString(p.data) + Environment.NewLine;
                // Добавляем в поле глобального чата
                chatBox.AppendText(formatted_data);
            });
        }
        // Обработчик события "пользователь онлайн"
        void globalServer_ClientStatusOnline(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                // Получаем ip аддресс пользователя
                string userIP = p.from.Address.ToString();
                // Смотрим нет ли уже такого в списке
                ListViewItem lvi = FindListItem(usersOnline, userIP);
                // Если нету
                if (lvi == null)
                {
                    // Создаем объект списка
                    ListViewItem lvItem = new ListViewItem();
                    // Заполняем поля объекта
                    lvItem.Text = p.NickName;
                    lvItem.Name = "lvi_" + userIP;
                    // Сохраним принятый пакет, прикрепив его к элементу списка
                    lvItem.Tag = p;
                    // Укажем используемую иконку
                    lvItem.ImageIndex = 0;
                    // Добавляем в список нового пользователя
                    usersOnline.Items.Add(lvItem);
                }
                    // Если уже есть в списке такой пользователь
                else
                {
                    // Получаем прикрепленный к этому пункту списка объект "пакет"
                    Packet pack = lvi.Tag as Packet;
                    // Проверим есть ли обновления относительно нового принятого пакета
                    if (pack.NickName.Equals(p.NickName) == false || pack.privatePort.Equals(p.privatePort) == false)
                    {
                        // Если есть обновим данные и прикрепим новый пакет к пункту списка
                        lvi.Text = p.NickName;
                        lvi.Tag = p;
                    }
                }
            });            
        }
        // Обработчик события "пользователь оффлайн"
        void globalServer_ClientStatusOffline(Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                // Ищем пользователя в списке
                ListViewItem lvi = FindListItem(usersOnline, p.from.Address.ToString());
                // Если найден
                if (lvi != null)
                {
                    // Удаляем
                    usersOnline.Items.Remove(lvi);
                }
            });
        }
        // Обработчик события приема TCP подключений
        void globalServer_AcceptedPM(Socket e)
        {
            try
            {
                // Создаем объект для нового клиента используя выданный сервером сокет
                Client client = new Client(e);
                // Создаем обработчики событий приема данных и отключения пользователя
                client.Received += client_DataReceived;
                client.Disconnected += client_Disconnected;
                // Получаем ip аддресс
                string userIP = client.EndPoint.Address.ToString();
                // Заносим в коллекцию новое подлключение
                peers.TryAdd(userIP, client);
            }
                // Отлавливаем ошибки
            catch (Exception ex) { Console.WriteLine("globalServer_AcceptedPM: " + ex.Message); }
        }

        #endregion

        #region "Client Events"
        // Клиентский обработчик приема данных
        void client_DataReceived(Client sender, Packet p)
        {
            Invoke((MethodInvoker)delegate
            {
                // Получаем ip аддресс
                string userIP = sender.EndPoint.Address.ToString();
                try
                {
                    // Определяем тип сообщения
                    switch (p.msgType)
                    {
                            // Простое текстовое
                        case MSG_TYPE.MESSAGE:
                            {
                                // Получаем вкладку для пользователя
                                TabPage page = GetPage(userIP);
                                // Получаем текстовое поле
                                LogBox privateBox = page.Controls["privateBox_" + userIP] as LogBox;
                                // Формируем строку
                                string formatted_data = string.Format("[{0}][{1}][{2}] ", p.from.Address, DateTime.Now, p.NickName) + Environment.NewLine + Packet.encoding.GetString(p.data) + Environment.NewLine;
                                // Добавляем данные в текстовое поле
                                privateBox.Append(formatted_data);
                            }
                            break;
                            // Сообщение типа файл
                        case MSG_TYPE.FILE:
                            {
                                // Получаем вкладку для пользователя
                                TabPage page = GetPage(userIP);
                                // Получаем текстовое поле
                                LogBox privateBox = page.Controls["privateBox_" + userIP] as LogBox;
                                // Массив байт - имя файла
                                byte[] fileName = new byte[MAX_PATH];
                                // Массив байт - данные файла
                                byte[] fileData = new byte[p.data.Length - MAX_PATH];
                                // Копируем кусок из тела пакета содержащий имя файла
                                Buffer.BlockCopy(p.data, 0, fileName, 0, MAX_PATH);
                                // Копируем кусок содержащий файл
                                Buffer.BlockCopy(p.data, MAX_PATH, fileData, 0, p.data.Length - MAX_PATH);
                                // Получаем путь к папке ехе файла
                                string path = Directory.GetCurrentDirectory();
                                // Формируем путь для папки принятых документов
                                path = Path.Combine(path, "Received Documents");
                                // Проверяем существует ли папка - если нет - создаем.
                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }
                                // Переводим имя файла в строку
                                string fName = Packet.encoding.GetString(fileName).TrimEnd(new char[] {(char)0 });
                                // Формируем полный путь к файлу
                                path = Path.Combine(path, fName);
                                // Записываем файл
                                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                                {
                                    fs.Write(fileData, 0, fileData.Length);                                    
                                }
                                // Добавляем информацию о принятом файле в текстовое поле
                                privateBox.Append("Принят файл: " + path + Environment.NewLine);
                            }
                            break;
                    }
                }
                    // Отлавливаем ошибки
                catch (Exception ex) { Console.WriteLine("Data received: " + ex.Message); }                
            });
        }
        // Обработчик события отключения клиента
        void client_Disconnected(Client sender)
        {
            Invoke((MethodInvoker)delegate
            {
                try
                {
                    // Получили ip аддресс
                    string userIP = sender.EndPoint.Address.ToString();
                    Client client;
                    // Удаляем из списка отключенного клиента
                    if (peers.TryRemove(userIP, out client) == false) throw new Exception("peer TryRemove failure");
                }
                    // Отлавливаем ошибки
                catch (Exception ex) { Console.WriteLine("client_Disconnected event: " + ex.Message); }                
            });
        }
        #endregion

        #region "Tabs, ListView"
        // Двойной щелчёк по элементу списка пользователей
        private void usersOnline_DoubleClick(object sender, EventArgs e)
        {
            ListView listView = (sender as ListView);
            if (listView.SelectedItems.Count == 1)
            {
                // Получаем выделенный элемент списка
                ListViewItem lvi = listView.SelectedItems[0];
                // Получаем ip аддресс
                string userIP = lvi.Name.Substring(4);
                // Получаем страницу для пользователя
                TabPage privatePage = GetPage(userIP);
                // Переключаемся на страницу
                chatTabs.SelectTab(privatePage);
            }
        }
        // Поиск пользователя в онлайн списке
        private ListViewItem FindListItem(ListView lv, string userIP)
        {
            for (int i = 0; i < lv.Items.Count; ++i)
                if (lv.Items[i].Name.Equals("lvi_" + userIP)) return lv.Items[i];
            return null;
        }
        // Меняя вкладки оставляем фокус в поле для ввода сообщения
        private void chatTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            msgBox.Focus();
        }
        // По двойному щелчку закрываем вкладку
        private void chatTabs_DoubleClick(object sender, EventArgs e)
        {
            if (chatTabs.SelectedIndex != 0)
            {
                chatTabs.SelectedTab.Dispose();
            }
        }
        // Получить вкладку для пользователя
        private TabPage GetPage(string userIP)
        {
            // Получаем пункт списка для пользователя с выбранным ip аддрессом
            ListViewItem lvi = FindListItem(usersOnline, userIP);
            // Получаем имя пользователя
            string userName = lvi.Text;
            // Пробуем получить доступ к вкладке (если уже открыта)
            TabPage privatePage = chatTabs.TabPages["privatePage_" + userIP];
            // Если получили доступ - возвращаем вкладку
            if (privatePage != null) return privatePage;
            // Иначе создаем новую
            privatePage = new TabPage(userName);
            privatePage.Name = "privatePage_" + userIP;
            // Создаем текстовое поле
            LogBox privateBox = new LogBox(userIP);
            privateBox.Location = new Point(7, 7);
            privateBox.Size = new System.Drawing.Size(555, 365);
            // Добавляем на вкладку текстбокс
            privatePage.Controls.Add(privateBox);
            // Добавляем вкладку
            chatTabs.TabPages.Add(privatePage);
            // Возвращаем созданную вкладку
            return privatePage;
        }

        #endregion

        // Обработка кнопки отправки данных
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
                // Создаем сокет TCP
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Пытаемся отправить данные
                try
                {
                    // Получаем пакет в котором есть инфо о пользователе которому будем отправлять
                    Packet p = lvi.Tag as Packet;
                    // Пробуем соединиться
                    socket.Connect(new IPEndPoint(p.from.Address, p.privatePort));
                    // Проверяем успешно ли прошло соединение
                    if (socket.Connected)
                    {
                        // Формируем строку
                        string formatted_data = string.Format("[{0}][{1}][{2}] ", "0"/*packet.from.Address*/, DateTime.Now, packet.NickName) + Environment.NewLine
                            + Packet.encoding.GetString(packet.data) + Environment.NewLine;
                        // Добавляем в текстовое поле привата
                        privateBox.Append(formatted_data);
                        // Отправляем приватное сообщение
                        globalServer.SendPrivateMessage(socket, packet);
                        // Поле ввода текста очищаем и переводим фокус в негоже
                        msgBox.Clear();
                        msgBox.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Пользователь вне сети");
                    }
                }
                    // Отлавливаем ошибки
                catch (Exception ex) { Console.WriteLine("btn_send_Click: " + ex.Message); }
                    // По завершению закрываем сокет
                finally
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Disconnect(false);
                    socket.Close();
                }
            });
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

    }
}
