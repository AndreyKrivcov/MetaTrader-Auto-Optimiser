using System.IO;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    /// <summary>
    /// Методы расширения для класса DirectoryInfo
    /// </summary>
    static class DirectoryInfoExtention
    {
        /// <summary>
        /// Метод расширения. Получает экземпляр класса DirectoryInfo для вложенной директории с переданным именем.
        /// </summary>
        /// <param name="directory">Экземпляр класса на котором вызывается метод</param>
        /// <param name="Name">Имя вложенной директории</param>
        /// <param name="createIfNotExists">Создать директорию если она не существует ?</param>
        /// <returns>
        /// Экземпляр класса DirectoryInfo для переданного имени вложенной директории 
        /// или null если не верно передано имя директории либо если ее не оказалось 
        /// и не требовалось ее создавать в случае отсутствия
        /// </returns>
        public static DirectoryInfo GetDirectory(this DirectoryInfo directory, string Name, bool createIfNotExists = false)
        {
            if (Name == null)
                return null;

            DirectoryInfo ans = new DirectoryInfo(Path.Combine(directory.FullName, Name));
            if (!ans.Exists)
            {
                if (!createIfNotExists)
                    return null;
                ans.Create();
            }
            return ans;
        }
    }
}
