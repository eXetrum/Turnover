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
        //public IPEndPoint privateIPEndPoint { get; private set; }
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

        #region Encrypt/Decrypt
        public static byte[] Encrypt(byte[] clearBytes, string EncryptionKey = "123")
        {
            ///
            EncryptionKey = Properties.Settings.Default.SECRET_KEY;
            ///

            byte[] encrypted;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }); // еще один плюс шарпа в наличие таких вот костылей.
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encrypted = ms.ToArray();
                }
            }
            return encrypted;
        }

        public static byte[] Decrypt(byte[] cipherBytes, string EncryptionKey = "123")
        {
            ///
            EncryptionKey = Properties.Settings.Default.SECRET_KEY;
            ///
            byte[] decryptedBytes = null;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }
            return decryptedBytes;
        }
        #endregion

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
