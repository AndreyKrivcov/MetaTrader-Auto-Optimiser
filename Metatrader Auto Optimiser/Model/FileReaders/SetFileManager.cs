using System;
using System.Collections.Generic;
using System.IO;

namespace Metatrader_Auto_Optimiser.Model.FileReaders
{
    /// <summary>
    /// Класс читающий и обновляющий данные в существующем файле с настройками робота или индикатора для прозодов оптимизации или же тестов
    /// </summary>
    class SetFileManager
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="filePath">путь к файлу с настройками</param>
        public SetFileManager(string filePath, bool createIfNotExists)
        {
            if ((FileInfo = new FileInfo(filePath)).Extension.CompareTo(".set") != 0)
                throw new ArgumentException("File mast have '.set' extention!");
            if (!File.Exists(filePath))
            {
                if (createIfNotExists)
                    File.Create(filePath).Close();
                else
                    throw new ArgumentException("File doesn`t exists");
            }
        }

        /// <summary>
        /// Поле зранящее в себе информаццию о файле
        /// </summary>
        public FileInfo FileInfo { get; }

        #region File data
        /// <summary>
        /// Коллекция с параметрами робота
        /// </summary>
        private List<ParamsItem> _params = new List<ParamsItem>();
        /// <summary>
        /// Property предоставляющий доступ к коллекции с параметрами робота
        /// </summary>
        public List<ParamsItem> Params
        {
            get
            {
                if (_params.Count == 0)
                    UpdateParams();
                return _params;
            }
            set
            {
                if (value != null && value.Count != 0)
                    _params = value;
            }
        }
        #endregion

        /// <summary>
        /// Сохранение изменений в файле
        /// </summary>
        public virtual void SaveParams()
        {
            if (_params.Count == 0)
                return;

            using (var file = new StreamWriter(FileInfo.FullName, false))
            {
                file.WriteLine(@"; saved by OptimisationManagerExtention program");
                file.WriteLine(";");
                foreach (var item in _params)
                {
                    file.WriteLine($"{item.Variable}={item.Value}||{item.Start}||{item.Step}||{item.Stop}||{(item.IsOptimize ? "Y" : "N")}");
                }
            }
        }
        /// <summary>
        /// Комирование файла по переданному пути и возвращение менеджера для данного файла
        /// </summary>
        /// <param name="pathToFile">Путь по которому будет создан дубликат текущего файла</param>
        /// <returns></returns>
        public virtual SetFileManager DublicateFile(string pathToFile)
        {
            if (new FileInfo(pathToFile).Extension.CompareTo(".set") != 0)
                throw new ArgumentException("File mast have '.set' extention!");

            File.Copy(FileInfo.FullName, pathToFile, true);
            return new SetFileManager(pathToFile, false);
        }
        /// <summary>
        /// Отчистка всех записанных данных в Params и загрузка данных из требуемого файла
        /// </summary>
        public virtual void UpdateParams()
        {
            _params.Clear();

            using (var file = FileInfo.OpenText())
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line[0].CompareTo(';') != 0 && line[0].CompareTo('#') != 0)
                    {
                        string[] key_value = line.Replace(" ", "").Split('=');
                        string[] value_data = key_value[1].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                        ParamsItem item = new ParamsItem
                        {
                            Variable = key_value[0],
                            Value = (value_data.Length > 0 ? value_data[0] : null),
                            Start = (value_data.Length > 1 ? value_data[1] : null),
                            Step = (value_data.Length > 2 ? value_data[2] : null),
                            Stop = (value_data.Length > 3 ? value_data[3] : null),
                            IsOptimize = (value_data.Length > 4 ? value_data[4].CompareTo("Y") == 0 : false)
                        };

                        _params.Add(item);
                    }
                }
            }
        }
    }

    struct ParamsItem
    {
        /// <summary>
        /// Признак нужно ли оптимизировать данную переменную робота
        /// </summary>
        public bool IsOptimize;
        /// <summary>
        /// Наименование переменной
        /// </summary>
        public string Variable;
        /// <summary>
        /// Значение переменной выбранное для теста
        /// </summary>
        public string Value;
        /// <summary>
        /// Начала перебора параметров
        /// </summary>
        public string Start;
        /// <summary>
        /// Шаг перебора параметров
        /// </summary>
        public string Step;
        /// <summary>
        /// Окончание перебора параметров
        /// </summary>
        public string Stop;
    }

}
