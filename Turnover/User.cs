using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Turnover
{
    [DataContract]
    public class User
    {
        public User(string ipAddress, string nickName)
        {
            this.ipAddress = ipAddress;
            this.nickName = nickName;            
        }
        public Form form;
        public TextBox textbox;

        [DataMember]
        public string ipAddress { get; set; }
        [DataMember]
        public string nickName { get; set; }

        public static User Deserialize(string json)
        {
            var _Bytes = Encoding.Unicode.GetBytes(json);
            using (MemoryStream _Stream = new MemoryStream(_Bytes))
            {
                var _Serializer = new DataContractJsonSerializer(typeof(User));
                return (User)_Serializer.ReadObject(_Stream);
            }
        }

        public static string Serialize(object instance)
        {
            using (MemoryStream _Stream = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(instance.GetType());
                _Serializer.WriteObject(_Stream, instance);
                _Stream.Position = 0;
                using (StreamReader _Reader = new StreamReader(_Stream))
                { return _Reader.ReadToEnd(); }
            }
        }

    }
}
