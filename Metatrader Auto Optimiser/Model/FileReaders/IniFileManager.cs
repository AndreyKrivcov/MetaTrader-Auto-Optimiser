using System;
using System.Runtime.InteropServices;
using System.Text;

using File = System.IO.File;

namespace Metatrader_Auto_Optimiser.Model.FileReaders
{
    class IniFileManager
    {
        private const int SIZE = 1024; //Максимальный размер (для чтения значения из файла)
        /// <summary>
        /// WinAPI - Извлекает строку из указанной секции инициализационного файла
        /// <param name="AppName">
        /// Имя секции содержащей имя ключа. Если параметр null то функция копирует все именя секций в подставленный буфер
        /// </param>
        /// <param name="KeyName">
        /// Имя ключа с которым ассоциируется извлекаемая строка. Если параметр null то все ключевые имяна в секции заданной как AppName копируются в буффер ReturnedString
        /// </param>
        /// <param name="Default"> 
        /// Строка по умолчанию. Если имя ключа не может быть найдено в инициализационном файле то функция копирует строку по умолчанию а буффер ReturnedString
        /// </param>
        /// <param name="ReturnedString">
        /// Указатель на буффер принимающий возвращаемую строку
        /// </param>
        /// <param name="Size">
        /// Размер буффера переданного по параметру ReturnedString.
        /// </param>
        /// <param name="FileName">
        /// Имя инициализационного файла. Если параметр не содержит полного пути к файлу, 
        /// то система производит поиск файла в директории Windows
        /// </param>
        /// <returns>
        /// Количество скопированных символов не считая null-termonated символа
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private extern static int GetPrivateProfileString(string AppName, string KeyName, string Default, StringBuilder ReturnedString, int Size, string FileName);
        /// <summary>
        /// Удобная обертка для WinAPI функции GetPrivateProfileString
        /// </summary>
        /// <param name="section">наименование секции</param>
        /// <param name="key">ключ</param>
        /// <returns>запрашиваемый параметр или null если ключь не был найден</returns>
        public static string GetParam(string section, string key, string path)
        {
            //Для получения значения
            StringBuilder buffer = new StringBuilder(SIZE);

            //Получить значение в buffer
            if (GetPrivateProfileString(section, key, null, buffer, SIZE, path) == 0)
                ThrowCErrorMeneger("GetPrivateProfileStrin", Marshal.GetLastWin32Error(), path);

            //Вернуть полученное значение
            return buffer.Length == 0 ? null : buffer.ToString();
        }
        /// <summary>
        /// Выброс ошибки
        /// </summary>
        /// <param name="methodName">Имя метода</param>
        /// <param name="er">Код ошибки</param>
        private static void ThrowCErrorMeneger(string methodName, int er, string path)
        {
            if (er > 0)
            {
                if (er == 2)
                {
                    if (!File.Exists(path))
                        throw new Exception($"{path} - File doesn1t exist");
                }
                else
                {
                    throw new Exception($"{methodName} error {er} " +
                        $"See System Error Codes (https://docs.microsoft.com/ru-ru/windows/desktop/Debug/system-error-codes) for detales");
                }
            }
        }

        /// <summary>
        /// WinAPI - Копирует строку в определенную секцию инициализационного файла
        /// </summary>
        /// <param name="AppName">
        /// Имя секции в которую строка будет скопированна. Если секция не существует, то она создастся. Имя секции регистро-независимо.
        /// Строка может состоять из любых комбинаций букв верхнего и нижнего регистра  
        /// </param>
        /// <param name="KeyName">
        /// Имя ключа которое будет ассоциировано со строкой. Если ключь не содержится в секции, то он создастся. 
        /// Если данный параметр null - то вся секция включая всё в секции - удаляется
        /// </param>
        /// <param name="Str">
        /// "null-termonated" строка (завершается на '/0') для записи в файл. Если параметр null - Если параметр null, то параметр переданный в качестве ключа KeyName - будет удален  
        /// </param>
        /// <param name="FileName">
        /// Имя инициализационного файла. Строка будет хаписана с исспользованием Unicode, если файл был создан с исспользованием Unicode символов. 
        /// Иначе будет исспользована ANSI кодировка
        /// </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private extern static int WritePrivateProfileString(string AppName, string KeyName, string Str, string FileName);
        /// <summary>
        /// Удобная обертка для WinAPI WritePrivateProfileString
        /// </summary>
        /// <param name="section">Секция</param>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        public static void WriteParam(string section, string key, string value, string path)
        {
            //Записать значение в INI-файл
            if (WritePrivateProfileString(section, key, value, path) == 0)
                ThrowCErrorMeneger("WritePrivateProfileString", Marshal.GetLastWin32Error(), path);
        }
    }
}
