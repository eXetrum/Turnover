using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

namespace Turnover
{
    class GlobalServer
    {
        public delegate void ClientReceivedMulticastHandler(Packet p);
        public delegate void ClientStatusOnlineChangedHandle(Packet p);
        public delegate void ClientStatusOfflineChangedHandle(Packet p);
        public delegate void SocketAcceptedHandler(Socket e);
        // События приема файла, смены статуса(онлайн/оффлайн), приема приватного сообщения
        public event ClientReceivedMulticastHandler ReceivedMulticast;
        public event ClientStatusOnlineChangedHandle ClientStatusOnline;
        public event ClientStatusOfflineChangedHandle ClientStatusOffline;
        public event SocketAcceptedHandler AcceptedPM;
        // UDP клиент для отправки сообщений в мультикаст группу
        private UdpClient udpSender;
        // Аддресс мультикаст группы
        private IPAddress multicastAddress;
        // Порт мультикаст группы
        private int multicastPort;
        // Конечная точка мультикаст группы
        private IPEndPoint multicastEP;
        // Порт для приватных сообщений
        private int privatePort;
        // "Приемник" приватных сообщений
        private TcpListener privateListener;
        // Поток рассылки статуса пользователя
        private Thread inviteThread = null;
        // Ивент рассылки
        ManualResetEvent inviteEvent = new ManualResetEvent(false);
        // Структура для асинхронного приема данных
        public class UdpData
        {
            public UdpClient workUDPclient = null;
            public IPEndPoint ipEndPoint = null;
        }
        // Конструктор
        public GlobalServer()
        {
            // один из зарезервированных для локальных нужд UDP адресов
            // Получаем аддресс мультикаст группы из настроек приложения
            multicastAddress = IPAddress.Parse(Properties.Settings.Default.multicastGroup); 
            // Получаем порт мультикаст группы
            multicastPort = Properties.Settings.Default.multicastPort;
            // Создаем конечную точку
            multicastEP = new IPEndPoint(multicastAddress, multicastPort);
            // Создаем отправщик для мультикаст группы
            udpSender = new UdpClient();
            // Получаем порт для приватных сообщений из настроек приложения
            privatePort = Properties.Settings.Default.privatePort;
        }
        // Стартовый метод сервера
        public void Listen()
        {
            // TCP listener для приватных сообщений
            privateListener = new TcpListener(new IPEndPoint(IPAddress.Any, privatePort));
            // Запускаем прослушку
            privateListener.Start(0);
            // Запускаем асинхронный прием TCP подключений
            privateListener.BeginAcceptSocket(acceptedPMCallback, null);
            // UDP listener для рассылки сообщений мультикаст группы
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);
            UdpClient client = new UdpClient();
            // Отключаем монопольный доступ для выбранного порта
            client.ExclusiveAddressUse = false;            
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // Биндимся на указанный порт
            client.Client.Bind(localEP);
            // Присоединяемся к мультикаст группе
            client.JoinMulticastGroup(multicastAddress);
            // Создаем пакет данных
            UdpData udpData = new UdpData();
            udpData.workUDPclient = client;
            udpData.ipEndPoint = localEP;
            // Начинаем асинхронный прием сообщений от мультикаст группы
            client.BeginReceive(new AsyncCallback(receiveCallback), udpData);
            // Запускаем поток рассылки приглашений
            inviteThread = new Thread(Inviter);
            inviteThread.IsBackground = true;
            inviteThread.Start();
        }
        // Остановка сервера
        public void StopGlobalListener()
        {
            // Сигнализируем событие
            inviteEvent.Set();
            // Ожидаем завершения потока рассылающего приглашения
            inviteThread.Join();
            // Закрываем клиент отправки данные группе
            udpSender.Close();
            // Останавливаем TCP прослушку порта приватных сообщений
            privateListener.Stop();
        }
        // Метод рассылки статуса
        private void Inviter(object sender)
        {
            do
            {
                // Формируем пакет
                Packet invitePacket = new Packet(MSG_TYPE.STATUS_ONLINE, null,
                    Properties.Settings.Default.NickName,
                    privatePort);
                // Отправляем
                SendMulticastMessage(invitePacket);
                // Ожидаем 2 секунды и повторяем
            } while (!inviteEvent.WaitOne(2000));
            // Рассылаем пакет прощания
            Packet endPacket = new Packet(MSG_TYPE.STATUS_OFFLINE, null, Properties.Settings.Default.NickName, privatePort);
            SendMulticastMessage(endPacket);
        }
        // Метод отправки UDP сообщения мультикаст группе
        public void SendMulticastMessage(Packet packet)
        {
            // Шифруем пакет
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            // Отправляем
            udpSender.Send(encrypted, encrypted.Length, multicastEP);
        }
        // Метод отправки TCP пакета конечному пользователю
        public void SendPrivateMessage(Socket remote, Packet packet)
        {
            // Шифруем пакет
            Byte[] encrypted = new Security().Encrypt(Packet.ObjectToByteArray(packet));
            // Получаем размер пакета и отправляем(сигнализирует начало пересылки пакета)
            remote.Send(BitConverter.GetBytes(encrypted.Length), 0, 4, SocketFlags.None);
            // Отправляем пакет
            remote.Send(encrypted, 0, encrypted.Length, SocketFlags.None);
        }
        // Калбек метод приема TCP подключений
        private void acceptedPMCallback(IAsyncResult ar)
        {
            try
            {
                // Получаем сокет для подключившегося
                Socket s = privateListener.EndAcceptSocket(ar);
                // Сигнализируем событием о завершении подключения
                if (AcceptedPM != null)
                    AcceptedPM(s);
                // Запускаем по новой прием подключений
                privateListener.BeginAcceptSocket(new AsyncCallback(acceptedPMCallback), null);
            }
                // Отлавливаем ошибки
            catch (ObjectDisposedException)
            { return; }
            catch (NullReferenceException)
            { return; }
            catch (Exception ex) { Console.WriteLine("SERVER AcceptPMcallback: " + ex.Message); }
        }    
        // Калбек метод приема UDP данных
        void receiveCallback(IAsyncResult ar)
        {
            try
            {
                // Получаем объект
                UdpData udpData = (UdpData)ar.AsyncState;
                // Получаем из объекта данные
                UdpClient client = udpData.workUDPclient;
                IPEndPoint ep = udpData.ipEndPoint;
                // Завершаем получение
                byte[] receivedBytes = client.EndReceive(ar, ref ep);
                // Расшифровываем
                byte[] decryptedBytes = new Security().Decrypt(receivedBytes);
                // Формируем пакет
                Packet receivedPacket = (Packet)Packet.ByteArrayToObject(decryptedBytes);
                // Дополняем полем от кого получен пакет
                receivedPacket.from = ep;
                // Определяем тип сообщения в пакете
                switch (receivedPacket.msgType)
                {
                    case MSG_TYPE.STATUS_ONLINE:
                        {
                            if (ClientStatusOnline != null)
                                ClientStatusOnline(receivedPacket);
                        }
                        break;
                    case MSG_TYPE.STATUS_OFFLINE:
                        {
                            if (ClientStatusOffline != null)
                                ClientStatusOffline(receivedPacket);
                        }
                        break;
                    case MSG_TYPE.MESSAGE:
                        {
                            if (ReceivedMulticast != null)
                                ReceivedMulticast(receivedPacket);
                        }
                        break;
                    case MSG_TYPE.FILE:
                        {
                        }
                        break;
                    default:
                        break;
                }
                // Запускаем заново асинхронный прием UDP данных
                client.BeginReceive(new AsyncCallback(receiveCallback), udpData);
            }
                // Отлавливаем ошибки
            catch (Exception ex) { Console.WriteLine("SERVER ReceiveCallback: " + ex.Message); }
        }        
    }
}
