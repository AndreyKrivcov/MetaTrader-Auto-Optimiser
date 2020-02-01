using System;

using File = System.IO.File;
using IniFileManager = Metatrader_Auto_Optimiser.Model.FileReaders.IniFileManager;

namespace Metatrader_Auto_Optimiser.Model.Terminal
{
    /// <summary>
    /// Класс работы с файлами конфигурации терминала
    /// </summary>
    class Config
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="path"></param>
        public Config(string path)
        {
            Path = path;
            CreateFileIfNotExists();

            Common = new CommonSection(this);
            Charts = new ChartsSection(this);
            Experts = new ExpertsSection(this);
            Objects = new ObjectsSection(this);
            Email = new EmailSection(this);
            StartUp = new StartUpSection(this);
            Tester = new TesterSection(this);
        }

        /// <summary>
        /// Метод создающий файл если тот не был создан ранее
        /// </summary>
        protected virtual void CreateFileIfNotExists()
        {
            if (!File.Exists(Path))
            {
                File.Create(Path).Close();
            }
        }

        /// <summary>
        /// путь к файлу
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Создание дубликата файла
        /// </summary>
        /// <param name="path">Путь к копии файла</param>
        /// <returns>Копия текущего файла</returns>
        public virtual Config DublicateFile(string path)
        {
            File.Copy(Path, path, true);
            return new Config(path);
        }

        #region Section managers
        private class Converter
        {
            private readonly string section, path;
            public Converter(string section, string path)
            {
                this.section = section;
                this.path = path;
            }

            public bool? Bool(string key)
            {
                string s = IniFileManager.GetParam(section, key, path);
                if (s == null)
                    return null;

                int n = Convert.ToInt32(s);
                if (n < 0 || n > 1)
                    throw new ArgumentException("string mast be 0 or 1");
                return n == 1;
            }
            public void Bool(string key, bool? val)
            {
                if (val.HasValue)
                    IniFileManager.WriteParam(section, key, val.Value ? "1" : "0", path);
            }

            public int? Int(string key)
            {
                string s = IniFileManager.GetParam(section, key, path);
                return s == null ? null : (int?)Convert.ToInt32(s);
            }
            public void Int(string key, int? val)
            {
                if (val.HasValue)
                    IniFileManager.WriteParam(section, key, val.Value.ToString(), path);
            }

            public double? Double(string key)
            {
                string s = IniFileManager.GetParam(section, key, path);
                return s == null ? null : (double?)Convert.ToDouble(s);
            }
            public void Double(string key, double? val)
            {
                if (val.HasValue)
                    IniFileManager.WriteParam(section, key, val.Value.ToString(), path);
            }

            public string String(string key) => IniFileManager.GetParam(section, key, path);
            public void String(string key, string value)
            {
                if (value != null)
                    IniFileManager.WriteParam(section, key, value, path);
            }

            public DateTime? DT(string key)
            {
                string s = IniFileManager.GetParam(section, key, path);
                return s == null ? null : (DateTime?)DateTime.ParseExact(s, "yyyy.MM.dd", null);
            }
            public void DT(string key, DateTime? val)
            {
                if (val.HasValue)
                    IniFileManager.WriteParam(section, key, val.Value.ToString("yyyy.MM.dd"), path);
            }
        }
        internal class CommonSection
        {
            private readonly Converter converter;
            public CommonSection(Config parent)
            {
                converter = new Converter("Common", parent.Path);
            }

            public uint? Login
            {
                get => (uint?)converter.Int("Login");
                set => converter.Int("Login", (int?)value);
            }
            public ServerAddressKeeper Server
            {
                get
                {
                    string s = converter.String("Server");
                    return s == null ? null : new ServerAddressKeeper(s);
                }
                set
                {
                    if (value != null)
                        converter.String("Server", value.Address);
                }
            }
            public string Password
            {
                get => converter.String("Password");
                set => converter.String("Password", value);
            }
            public string CertPassword
            {
                get => converter.String("CertPassword");
                set => converter.String("CertPassword", value);
            }
            public bool? ProxyEnable
            {
                get => converter.Bool("ProxyEnable");
                set => converter.Bool("ProxyEnable", value);
            }
            public ENUM_ProxyType? ProxyType
            {
                get => (ENUM_ProxyType?)converter.Int("ProxyType");
                set => converter.Int("ProxyType", (int?)value);
            }
            public ServerAddressKeeper ProxyAddress
            {
                get
                {
                    string s = converter.String("ProxyAddress");
                    return s == null ? null : new ServerAddressKeeper(s);
                }
                set
                {
                    if (value != null)
                        converter.String("ProxyAddress", value.Address);
                }
            }
            public string ProxyLogin
            {
                get => converter.String("ProxyLogin");
                set => converter.String("ProxyLogin", value);
            }
            public string ProxyPassword
            {
                get => converter.String("ProxyPassword");
                set => converter.String("ProxyPassword", value);
            }
            public bool? KeepPrivate
            {
                get => converter.Bool("KeepPrivate");
                set => converter.Bool("KeepPrivate", value);
            }
            public bool? NewsEnable
            {
                get => converter.Bool("NewsEnable");
                set => converter.Bool("NewsEnable", value);
            }
            public bool? CertInstall
            {
                get => converter.Bool("CertInstall");
                set => converter.Bool("CertInstall", value);
            }
            public string MQL5Login
            {
                get => converter.String("MQL5Login");
                set => converter.String("MQL5Login", value);
            }
            public string MQL5Password
            {
                get => converter.String("MQL5Password");
                set => converter.String("MQL5Password", value);
            }
        }
        internal class ChartsSection
        {
            private readonly Converter converter;
            public ChartsSection(Config parent)
            {
                converter = new Converter("Charts", parent.Path);
            }

            public string ProfileLast
            {
                get => converter.String("ProfileLast");
                set => converter.String("ProfileLast", value);
            }
            public int? MaxBars
            {
                get => converter.Int("MaxBars");
                set => converter.Int("MaxBars", value);
            }
            public bool? PrintColor
            {
                get => converter.Bool("PrintColor");
                set => converter.Bool("PrintColor", value);
            }
            public bool? SaveDeleted
            {
                get => converter.Bool("SaveDeleted");
                set => converter.Bool("SaveDeleted", value);
            }
        }
        internal class ExpertsSection
        {
            private readonly Converter converter;
            public ExpertsSection(Config parent)
            {
                converter = new Converter("Experts", parent.Path);
            }

            public bool? AllowLiveTrading
            {
                get => converter.Bool("AllowLiveTrading");
                set => converter.Bool("AllowLiveTrading", value);
            }
            public bool? AllowDllImport
            {
                get => converter.Bool("AllowDllImport");
                set => converter.Bool("AllowDllImport", value);
            }
            public bool? Enabled
            {
                get => converter.Bool("Enabled");
                set => converter.Bool("Enabled", value);
            }
            public bool? Account
            {
                get => converter.Bool("Account");
                set => converter.Bool("Account", value);
            }
            public bool? Profile
            {
                get => converter.Bool("Profile");
                set => converter.Bool("Profile", value);
            }
        }
        internal class ObjectsSection
        {
            private readonly Converter converter;
            public ObjectsSection(Config parent)
            {
                converter = new Converter("Objects", parent.Path);
            }

            public bool? ShowPropertiesOnCreate
            {
                get => converter.Bool("ShowPropertiesOnCreate");
                set => converter.Bool("ShowPropertiesOnCreate", value);
            }
            public bool? SelectOneClick
            {
                get => converter.Bool("SelectOneClick");
                set => converter.Bool("SelectOneClick", value);
            }
            public int? MagnetSens
            {
                get => converter.Int("MagnetSens");
                set => converter.Int("MagnetSens", value);
            }
        }
        internal class EmailSection
        {
            private readonly Converter converter;
            public EmailSection(Config parent)
            {
                converter = new Converter("Email", parent.Path);
            }

            public bool? Enable
            {
                get => converter.Bool("Enable");
                set => converter.Bool("Enable", value);
            }
            public string Server
            {
                get => converter.String("Server");
                set => converter.String("Server", value);
            }
            public string Auth
            {
                get => converter.String("Auth");
                set => converter.String("Auth", value);
            }
            public string Login
            {
                get => converter.String("Login");
                set => converter.String("Login", value);
            }
            public string Password
            {
                get => converter.String("Password");
                set => converter.String("Password", value);
            }
            public string From
            {
                get => converter.String("From");
                set => converter.String("From", value);
            }
            public string To
            {
                get => converter.String("To");
                set => converter.String("To", value);
            }
        }
        internal class StartUpSection
        {
            private readonly Converter converter;
            public StartUpSection(Config parent)
            {
                converter = new Converter("StartUp", parent.Path);
            }

            public string Expert
            {
                get => converter.String("Expert");
                set => converter.String("Expert", value);
            }
            public string Symbol
            {
                get => converter.String("Symbol");
                set => converter.String("Symbol", value);
            }
            public ENUM_Timeframes? Period
            {
                get => (ENUM_Timeframes?)Enum.Parse(typeof(ENUM_Timeframes), converter.String("Period"));
                set => converter.String("Period", value?.ToString());
            }
            public string Template
            {
                get => converter.String("Template");
                set => converter.String("Template", value);
            }
            public string ExpertParameters
            {
                get => converter.String("ExpertParameters");
                set => converter.String("ExpertParameters", value);
            }
            public string Script
            {
                get => converter.String("Script");
                set => converter.String("Script", value);
            }
            public string ScriptParameters
            {
                get => converter.String("ScriptParameters");
                set => converter.String("ScriptParameters", value);
            }
        }
        internal class TesterSection
        {
            private readonly Converter converter;
            public TesterSection(Config parent)
            {
                converter = new Converter("Tester", parent.Path);
            }

            public string Expert
            {
                get => converter.String("Expert");
                set => converter.String("Expert", value);
            }
            public string ExpertParameters
            {
                get => converter.String("ExpertParameters");
                set => converter.String("ExpertParameters", value);
            }
            public string Symbol
            {
                get => converter.String("Symbol");
                set => converter.String("Symbol", value);
            }
            public ENUM_Timeframes? Period
            {
                get => (ENUM_Timeframes?)Enum.Parse(typeof(ENUM_Timeframes), converter.String("Period"));
                set => converter.String("Period", value?.ToString());
            }
            public uint? Login
            {
                get => (uint?)converter.Int("Login");
                set => converter.Int("Login", (int?)value);
            }
            public ENUM_Model? Model
            {
                get => (ENUM_Model?)converter.Int("Model");
                set => converter.Int("Model", (int?)value);
            }
            public ENUM_ExecutionDelay? ExecutionMode
            {
                get => (ENUM_ExecutionDelay?)converter.Int("ExecutionMode");
                set => converter.Int("ExecutionMode", (int?)value);
            }
            public ENUM_OptimisationMode? Optimization
            {
                get => (ENUM_OptimisationMode?)converter.Int("Optimization");
                set => converter.Int("Optimization", (int?)value);
            }
            public ENUM_OptimisationCriteria? OptimizationCriterion
            {
                get => (ENUM_OptimisationCriteria?)converter.Int("OptimizationCriterion");
                set => converter.Int("OptimizationCriterion", (int?)value);
            }
            public DateTime? FromDate
            {
                get => converter.DT("FromDate");
                set => converter.DT("FromDate", value);
            }
            public DateTime? ToDate
            {
                get => converter.DT("ToDate");
                set => converter.DT("ToDate", value);
            }
            public ENUM_ForvardMode? ForwardMode
            {
                get => (ENUM_ForvardMode?)converter.Int("ForwardMode");
                set => converter.Int("ForwardMode", (int?)value);
            }
            public DateTime? ForwardDate
            {
                get => converter.DT("ForwardDate");
                set => converter.DT("ForwardDate", value);
            }
            public string Report
            {
                get => converter.String("Report");
                set => converter.String("Report", value);
            }
            public bool? ReplaceReport
            {
                get => converter.Bool("ReplaceReport");
                set => converter.Bool("ReplaceReport", value);
            }
            public bool? ShutdownTerminal
            {
                get => converter.Bool("ShutdownTerminal");
                set => converter.Bool("ShutdownTerminal", value);
            }
            public double? Deposit
            {
                get => converter.Double("Deposit");
                set => converter.Double("Deposit", value);
            }
            public string Currency
            {
                get => converter.String("Currency");
                set => converter.String("Currency", value);
            }
            public string Leverage
            {
                get => converter.String("Leverage");
                set => converter.String("Leverage", value);
            }
            public bool? UseLocal
            {
                get => converter.Bool("UseLocal");
                set => converter.Bool("UseLocal", value);
            }
            public bool? UseRemote
            {
                get => converter.Bool("UseRemote");
                set => converter.Bool("UseRemote", value);
            }
            public bool? UseCloud
            {
                get => converter.Bool("UseCloud");
                set => converter.Bool("UseCloud", value);
            }
            public bool? Visual
            {
                get => converter.Bool("Visual");
                set => converter.Bool("Visual", value);
            }
            public uint? Port
            {
                get => (uint?)converter.Int("Port");
                set => converter.Int("Port", (int?)value);
            }
        }
        #endregion

        public CommonSection Common { get; }
        public ChartsSection Charts { get; }
        public ExpertsSection Experts { get; }
        public ObjectsSection Objects { get; }
        public EmailSection Email { get; }
        public StartUpSection StartUp { get; }
        public TesterSection Tester { get; }

        /// <summary>
        /// Удаление секции
        /// </summary>
        /// <param name="section">выбранная на удаление секция</param>
        public void DeleteSection(ENUM_SectionType section)
        {
            IniFileManager.WriteParam(section.ToString(), null, null, Path);
        }
        /// <summary>
        /// Удаление ключа
        /// </summary>
        /// <param name="section">секция из которой будет удаляться ключь</param>
        /// <param name="key">Удаляемый ключь</param>
        public void DeleteKey(ENUM_SectionType section, string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is not vailed");

            IniFileManager.WriteParam(section.ToString(), key, null, Path);
        }

    }

    #region Accessory objects
    /// <summary>
    /// Вид секции
    /// </summary>
    enum ENUM_SectionType
    {
        Common,
        Charts,
        Experts,
        Objects,
        Email,
        StartUp,
        Tester
    }

    /// <summary>
    /// Тип форвардного тестирования
    /// </summary>
    enum ENUM_ForvardMode
    {
        Disabled,
        Half, //1/2
        Third, // 1/3
        Fourth, // 1/4
        Custom
    }

    /// <summary>
    /// Виды эмуляции задержки исполнения ордера
    /// </summary>
    enum ENUM_ExecutionDelay
    {
        Without_Delay = 0,
        Random_Delay = -1,
        ms_2 = 2,
        ms_1 = 1,
        ms_5 = 5,
        ms_10 = 10,
        ms_20 = 20,
        ms_50 = 50,
        ms_100 = 100,
        ms_500 = 500,
        ms_1000 = 1000
    }

    /// <summary>
    /// Виды способа подачи графика в робот при тестировании / оптимизации
    /// </summary>
    enum ENUM_Model
    {
        Every_tick,
        OHLC_1_minute,
        Open_pricies_only,
        Math_calculations,
        Every_tick_based_on_real_ticks
    }

    /// <summary>
    /// Критерии оптимизационного отбора при оптимизации
    /// </summary>
    enum ENUM_OptimisationCriteria
    {
        Balance,
        Balance__Profit_factor,
        Balance__Expected_Payoff,
        Balance__Min_DD,
        Balance__Recovery_factor,
        Balance__Sharp_ratio,
        Custom
    }

    /// <summary>
    /// Таймфреймы
    /// </summary>
    enum ENUM_Timeframes
    {
        M1 = 1,
        M2 = 2,
        M3 = 3,
        M4 = 4,
        M5 = 5,
        M6 = 6,
        M10 = 10,
        M12 = 12,
        M15 = 15,
        M20 = 20,
        M30 = 30,
        H1 = 16385,
        H2 = 16386,
        H3 = 16387,
        H4 = 16388,
        H6 = 16390,
        H8 = 16392,
        H12 = 16396,
        D1 = 16408,
        W1 = 32769,
        MN1 = 49153
    }

    /// <summary>
    /// Способы оптимизации робота
    /// </summary>
    enum ENUM_OptimisationMode
    {
        Disabled,
        Slow_complete_algorithm,
        Fast_genetic_based_algorithm,
        All_symbols_selected_in_Market_Watch
    }

    /// <summary>
    /// Тип прокси сервера
    /// </summary>
    enum ENUM_ProxyType
    {
        SOCKS4,
        SOCKS5,
        HTTP
    }

    /// <summary>
    /// IPv4 адрес сервера и порт
    /// </summary>
    class ServerAddressKeeper
    {
        public ServerAddressKeeper(IPv4Adress ip, uint port)
        {
            IP = ip;
            Port = port;
        }
        public ServerAddressKeeper(string adress)
        {
            if (string.IsNullOrEmpty(adress) || string.IsNullOrWhiteSpace(adress))
                throw new ArgumentException("adress is incorrect");

            string[] data = adress.Split(':');

            if (data.Length != 2)
                throw new ArgumentException("adress is incorrect");

            IP = new IPv4Adress(data[0]);
            Port = Convert.ToUInt32(data[1]);
        }

        public IPv4Adress IP { get; }
        public uint Port { get; }

        public string Address => $"{IP.ToString()}:{Port}";
    }

    /// <summary>
    /// IPv4 адрес сервера
    /// </summary>
    struct IPv4Adress
    {
        public IPv4Adress(string adress)
        {
            string[] ip = adress.Split('.');
            if (ip.Length != 4)
                throw new ArgumentException("ip is incorrect");

            part_1 = (char)Convert.ToInt32(ip[0]);
            part_2 = (char)Convert.ToInt32(ip[1]);
            part_3 = (char)Convert.ToInt32(ip[2]);
            part_4 = (char)Convert.ToInt32(ip[3]);
        }

        public char part_1;
        public char part_2;
        public char part_3;
        public char part_4;

        public new string ToString()
        {
            return $"{(int)part_1}.{(int)part_2}.{(int)part_3}.{(int)part_4}";
        }
    }
    #endregion
}
