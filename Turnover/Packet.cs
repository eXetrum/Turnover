using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Turnover
{
    // Тип сообщения
    public enum MSG_TYPE : int
    {
        STATUS_ONLINE = 0,
        STATUS_OFFLINE,
        MESSAGE,
        FILE
    }
    // Укажем что объекты типа Packet сериализуемые
    [Serializable]
    public class Packet
    {
        // Кодировка
        public static Encoding encoding = new UTF8Encoding();
        // Тип сообщения
        public MSG_TYPE msgType { get; private set; }
        // Данные в пакете
        public byte[] data { get; private set; }
        // Никнейм отправителя
        public string NickName { get; set; }
        // Порт для приватных сообщений
        public int privatePort { get; set; }
        // Конечная точка от кого пакет
        public IPEndPoint from { get; set; }
        // Конструктор
        public Packet(MSG_TYPE msgType, byte[] data, string NickName, int privatePort)
        {
            this.msgType = msgType;
            this.data = data;
            this.NickName = NickName;
            this.privatePort = privatePort;
        }
        // Методы серриализации пакета в байты и наоборот
        #region Convert Packet to bytes <=> bytes to Packet
        // Convert an object to a byte array
        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        // Convert a byte array to an Object
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
        #endregion
    }
}
