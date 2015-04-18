using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Turnover
{
    // Наследуемся от текстового бокса, для того чтобы добавить автоматический лог в файл
    public class LogBox : TextBox
    {
        // Путь к лог файлу
        string filePath;
        // Конструктор принимающий ип пользователя
        public LogBox(string userIP)
        {
            // Задаем параметры бокса
            this.Name = "privateBox_" + userIP;
            this.Multiline = true;
            this.ReadOnly = true;
            this.BackColor = Color.White;
            this.ScrollBars = ScrollBars.Vertical;
            // Читаем старые данные или создаем новый пустой лог файл 
            try
            {
                // Получаем путь к папке с ехе. Склеиваем с "Logs"
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                // Получаем полный путь к лог файлу
                filePath = Path.Combine(path, userIP + ".txt");
                // Проверяем есть ли уже директория логов
                if (!Directory.Exists(path))
                {
                    // Если нету - создаем папку
                    Directory.CreateDirectory(path);
                    // Создаем файл
                    File.Create(filePath);
                }
                    // Если директория уже есть
                else
                {
                    // Читаем содержимое лог файла и заносим в поле текстбокса
                    this.Text = File.ReadAllText(filePath);
                }
            }
                // Отлавливаем ошибки
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        // Метод добавления данных в текстовое поле
        public void Append(string text)
        {
            // Вызываем родительский метод добавления строки в текстбокс
            this.AppendText(text);
            // И дополним добавлением в файл такой же строки
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.Write(text);
            }
        }
    }
}
