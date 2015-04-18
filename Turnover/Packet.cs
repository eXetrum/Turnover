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
    public enum MSG_TYPE : int
    {
        STATUS_ONLINE = 0,
        STATUS_OFFLINE,
        MESSAGE,
        FILE
    }
    [Serializable]
    public class Packet
    {
        public static Encoding encoding = new UTF8Encoding();

        public MSG_TYPE msgType { get; private set; }
        public byte[] data { get; private set; }
        public string NickName { get; set; }
        public int privatePort { get; set; }

        public IPEndPoint from { get; set; }

        public Packet(MSG_TYPE msgType, byte[] data, string NickName, int privatePort/*IPEndPoint privateIPEndPoint*/)
        {
            this.msgType = msgType;
            this.data = data;
            this.NickName = NickName;
            this.privatePort = privatePort;
            //this.privateIPEndPoint = privateIPEndPoint;
        }

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
