using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Turnover
{
    static class Program
    {
        // Создаем мьютекс
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Проверяем не запущен ли уже экземпляр приложения
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                // Если не запущен - запускаем
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new mainForm());
                mutex.ReleaseMutex();
            }
                // Иначе сообщаем что процесс уже запущен
            else
            {
                MessageBox.Show("Only one instance at a time");
            }                        
        }
    }
}
