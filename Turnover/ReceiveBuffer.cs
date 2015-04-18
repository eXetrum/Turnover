using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Turnover
{
    // Буфер приема
    public class ReceiveBuffer
    {
        // Размер буфера
        public const int BUFFER_SIZE = 1024;
        // Общее количество данных для приема
        public long ToReceive;
        // Буфер для "кусочного" приема
        public byte[] Buffer;
        // Стрим в который сохраняем все принятые данные
        public MemoryStream memStream;
        // Конструктор принимает общее количество принимаемых данных
        public ReceiveBuffer(long toRec)
        {
            ToReceive = toRec;
            Buffer = new byte[BUFFER_SIZE];
            memStream = new MemoryStream();
        }
        // При завершении использования буфера позволим Garbage Collector'у собрать все неиспользуемые объекты
        public void Dispose()
        {
            Buffer = null;
            ToReceive = 0;
            if (memStream != null && memStream.CanWrite)
            {
                memStream.Close();
                memStream.Dispose();
                memStream = null;
            }
        }
    }
}
