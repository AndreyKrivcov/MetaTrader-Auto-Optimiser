using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace ReportManager
{
    /// <summary>
    /// Читатель отчетов
    /// </summary>
    public class ReportReader : IDisposable
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        public ReportReader(string path)
        {
            // загружаем дакумент
            document.Load(path);

            // Получаем дату создания файла
            Created = DateTime.ParseExact(document["Optimisatin_Report"].Attributes["Created"].Value, "dd.MM.yyyy HH:mm:ss", null);
            // Получаем enumerator
            enumerator = document["Optimisatin_Report"]["Optimisation_Results"].ChildNodes.GetEnumerator();

            // Функция получения параметра
            string xpath(string Name) { return $"/Optimisatin_Report/Optimiser_Settings/Item[@Name='{Name}']"; }

            // Получаем путь до робота
            RelativePathToBot = document.SelectSingleNode(xpath("Bot")).InnerText;

            // Получаем баланс и валюту депозита
            XmlNode Deposit = document.SelectSingleNode(xpath("Deposit"));
            Balance = Convert.ToDouble(Deposit.InnerText.Replace(",", "."), formatInfo);
            Currency = Deposit.Attributes["Currency"].Value;

            // Получаем кредитное плечо
            Laverage = Convert.ToInt32(document.SelectSingleNode(xpath("Laverage")).InnerText);
        }
        /// <summary>
        /// Провайдер формата двоичных чисел
        /// </summary>
        private readonly NumberFormatInfo formatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };

        #region DataKeepers
        /// <summary>
        /// Представление файла с отчетом в формете ООП
        /// </summary>
        private readonly XmlDocument document = new XmlDocument();
        /// <summary>
        /// Еллекция нодов докусента (коллекция строк в excel таблице)
        /// </summary>
        private readonly System.Collections.IEnumerator enumerator;
        #endregion
        /// <summary>
        /// прочитанный текущий элемент отчета
        /// </summary>
        public ReportItem? ReportItem { get; private set; } = null;

        #region Optimiser settings
        /// <summary>
        /// Путь до робота
        /// </summary>
        public string RelativePathToBot { get; }
        /// <summary>
        /// Баланс
        /// </summary>
        public double Balance { get; }
        /// <summary>
        /// Валюта
        /// </summary>
        public string Currency { get; }
        /// <summary>
        /// Плече
        /// </summary>
        public int Laverage { get; }
        #endregion
        /// <summary>
        /// Дата роздания файла
        /// </summary>
        public DateTime Created { get; }
        /// <summary>
        /// Метод читающий файл
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            if (enumerator == null)
                return false;

            // Читаем следующий элемент
            bool ans = enumerator.MoveNext();
            if (ans)
            {
                // Текущая нода
                XmlNode result = (XmlNode)enumerator.Current;
                // текущий элемент отчета
                var Item = new ReportItem
                {
                    BotParams = new Dictionary<string, string>(),
                    Symbol = result.Attributes["Symbol"].Value,
                    TF = Convert.ToInt32(result.Attributes["TF"].Value),
                    DateBorders = new DateBorders(Convert.ToUInt64(result.Attributes["Start_DT"].Value).UnixDTToDT(),
                                                  Convert.ToUInt64(result.Attributes["Finish_DT"].Value).UnixDTToDT()),
                    OptimisationCoefficients = new Coefficients
                    {
                        AltmanZScore = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Altman Z Score")).InnerText.Replace(",", "."), formatInfo),
                        Payoff = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Payoff")).InnerText.Replace(",", "."), formatInfo),
                        ProfitFactor = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Profit factor")).InnerText.Replace(",", "."), formatInfo),
                        AverageProfitFactor = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Average Profit factor")).InnerText.Replace(",", "."), formatInfo),
                        RecoveryFactor = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Recovery factor")).InnerText.Replace(",", "."), formatInfo),
                        AverageRecoveryFactor = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("Average Recovery factor")).InnerText.Replace(",", "."), formatInfo),
                        TotalTrades = Convert.ToInt32(result["Coefficients"].SelectSingleNode(SelectItem("Total trades")).InnerText),
                        PL = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("PL")).InnerText.Replace(",", "."), formatInfo),
                        DD = Convert.ToDouble(result["Coefficients"].SelectSingleNode(SelectItem("DD")).InnerText.Replace(",", "."), formatInfo),
                        VaR = new VaRData
                        {
                            Mx = Convert.ToDouble(result["Coefficients"]["VaR"].SelectSingleNode(SelectItem("Mx")).InnerText.Replace(",", "."), formatInfo),
                            Std = Convert.ToDouble(result["Coefficients"]["VaR"].SelectSingleNode(SelectItem("Std")).InnerText.Replace(",", "."), formatInfo),
                            Q_90 = Convert.ToDouble(result["Coefficients"]["VaR"].SelectSingleNode(SelectItem("90")).InnerText.Replace(",", "."), formatInfo),
                            Q_95 = Convert.ToDouble(result["Coefficients"]["VaR"].SelectSingleNode(SelectItem("95")).InnerText.Replace(",", "."), formatInfo),
                            Q_99 = Convert.ToDouble(result["Coefficients"]["VaR"].SelectSingleNode(SelectItem("99")).InnerText.Replace(",", "."), formatInfo)
                        },
                        MaxPLDD = new MaxPLDD
                        {
                            Profit = new MaxPLDD.Max
                            {
                                Value = Convert.ToDouble(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("Profit")).InnerText.Replace(",", "."), formatInfo),
                                TotalTrades = Convert.ToInt32(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("Total Profit Trades")).InnerText),
                                ConsecutivesTrades = Convert.ToInt32(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("Consecutive Wins")).InnerText)
                            },
                            DD = new MaxPLDD.Max
                            {
                                Value = Convert.ToDouble(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("DD")).InnerText.Replace(",", "."), formatInfo),
                                TotalTrades = Convert.ToInt32(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("Total Loose Trades")).InnerText),
                                ConsecutivesTrades = Convert.ToInt32(result["Coefficients"]["Max_PL_DD"].SelectSingleNode(SelectItem("Consecutive Loose")).InnerText)
                            }
                        },
                        TradingDays = new Dictionary<DayOfWeek, DailyData>
                        {
                            {
                                DayOfWeek.Monday,
                                GetDay(result["Coefficients"]["Trading_Days"]["Mn"])
                            },
                            {
                                DayOfWeek.Tuesday,
                                GetDay(result["Coefficients"]["Trading_Days"]["Tu"])
                            },
                            {
                                DayOfWeek.Wednesday,
                                GetDay(result["Coefficients"]["Trading_Days"]["We"])
                            },
                            {
                                DayOfWeek.Thursday,
                                GetDay(result["Coefficients"]["Trading_Days"]["Th"])
                            },
                            {
                                DayOfWeek.Friday,
                                GetDay(result["Coefficients"]["Trading_Days"]["Fr"])
                            }
                        }
                    }
                };

                var custom_item = result["Coefficients"].SelectSingleNode(SelectItem("Custom"));
                if (custom_item != null)
                    Item.OptimisationCoefficients.Custom = Convert.ToDouble(custom_item.InnerText.Replace(",", "."), formatInfo);
                else
                    Item.OptimisationCoefficients.Custom = 0;

                // Заполняем параметры робота
                foreach (XmlNode item in result.ChildNodes)
                {
                    if (item.Name == "Item")
                        Item.BotParams.Add(item.Attributes["Name"].Value, item.InnerText);
                }
                ReportItem = Item;
            }

            return ans;
        }
        /// <summary>
        /// Метод мозврящающий строку выбирающую элемент по его имени (аттрибут Name)
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private string SelectItem(string Name) => $"Item[@Name='{Name}']";
        /// <summary>
        /// Получаем значение результата торгов выбранного дня
        /// </summary>
        /// <param name="dailyNode">Нода данного дня</param>
        /// <returns></returns>
        private DailyData GetDay(XmlNode dailyNode)
        {
            return new DailyData
            {
                Profit = new DailyData.Side
                {
                    Value = Convert.ToDouble(dailyNode.SelectSingleNode(SelectItem("Profit")).InnerText.Replace(",", "."), formatInfo),
                    Trades = Convert.ToInt32(dailyNode.SelectSingleNode(SelectItem("Number Of Profit Trades")).InnerText)
                },
                DD = new DailyData.Side
                {
                    Value = Convert.ToDouble(dailyNode.SelectSingleNode(SelectItem("DD")).InnerText.Replace(",", "."), formatInfo),
                    Trades = Convert.ToInt32(dailyNode.SelectSingleNode(SelectItem("Number Of Loose Trades")).InnerText)
                }
            };
        }
        /// <summary>
        /// Сброс читателя котировок
        /// </summary>
        public void ResetReader()
        {
            if (enumerator != null)
            {
                enumerator.Reset(); // Сбрасываем
            }
        }
        /// <summary>
        /// Отчищаем документ
        /// </summary>
        public void Dispose() => document.RemoveAll();
    }

    #region Entities
    /// <summary>
    /// Структура результата конкретного оптимизационного прохода
    /// </summary>
    public struct ReportItem
    {
        public Dictionary<string, string> BotParams; // Список параметров робота
        public Coefficients OptimisationCoefficients; // Коэффициенты робота
        public string Symbol; // Символ
        public int TF; // Таймфрейм
        public DateBorders DateBorders; // Границы дат
    }

    /// <summary>
    /// Класс границ дат
    /// </summary>
    public class DateBorders : IComparable
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="from">Дата начала границы</param>
        /// <param name="till">Дата окончания границы</param>
        public DateBorders(DateTime from, DateTime till)
        {
            if (till <= from)
                throw new ArgumentException("Date 'Till' is less or equal to date 'From'");

            From = from;
            Till = till;
        }
        /// <summary>
        /// С
        /// </summary>
        public DateTime From { get; }
        /// <summary>
        /// По
        /// </summary>
        public DateTime Till { get; }

        /// <summary>
        /// Метод сопоставляющий форвардные и исторические оптимизации
        /// </summary>
        /// <param name="History">Массив исторических оптимизаций</param>
        /// <param name="Forward">Массив форвардных оптимизаций</param>
        /// <returns>Сортированный список историческая - форвардная оптимизации</returns>
        public static Dictionary<DateBorders, DateBorders> CompareHistoryToForward(List<DateBorders> History, List<DateBorders> Forward)
        {
            // массив сопоставимых оптимизаций
            Dictionary<DateBorders, DateBorders> ans = new Dictionary<DateBorders, DateBorders>();

            // Сортируем переданные параметры
            History.Sort();
            Forward.Sort();

            // Создаем цикл по историческим оптимизациям
            int i = 0;
            foreach (var item in History)
            {
                if (ans.ContainsKey(item))
                    continue;

                ans.Add(item, null); // Добавляем историческую оптимизацию
                if (Forward.Count <= i)
                    continue; // Если массив форвардныех оптимизаций меньше индекса - продолжаем цикл

                // Цикл по форвардным оптимизациям
                for (int j = i; j < Forward.Count; j++)
                {
                    // Если в массиве результатов содержится текущяя форвардная оптимизация - то пропускаем
                    if (ans.ContainsValue(Forward[j]) ||
                        Forward[j].From < item.Till)
                    {
                        continue;
                    }

                    // Сопоставляем форвардную и историческую оптимизации
                    ans[item] = Forward[j];
                    i = j + 1;
                    break;
                }
            }

            return ans;
        }

        #region Equal
        /// <summary>
        /// Оператор сравнения на равенство
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат</returns>
        public static bool operator ==(DateBorders b1, DateBorders b2)
        {
            bool ans;
            if (b2 is null && b1 is null) ans = true;
            else if (b2 is null || b1 is null) ans = false;
            else ans = b1.From == b2.From && b1.Till == b2.Till;

            return ans;
        }
        /// <summary>
        /// Оператор сравнения на неравенство
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат сравнения</returns>
        public static bool operator !=(DateBorders b1, DateBorders b2) => !(b1 == b2);
        #endregion

        #region (Grater / Less) than
        /// <summary>
        /// Сравнение текущий элемент больше прошлого
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат</returns>
        public static bool operator >(DateBorders b1, DateBorders b2)
        {
            if (b1 == null || b2 == null)
                return false;

            if (b1.From == b2.From)
                return (b1.Till > b2.Till);
            else
                return (b1.From > b2.From);
        }
        /// <summary>
        /// Сравнение текущий элемент меньше прошлого
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат</returns>
        public static bool operator <(DateBorders b1, DateBorders b2)
        {
            if (b1 == null || b2 == null)
                return false;

            if (b1.From == b2.From)
                return (b1.Till < b2.Till);
            else
                return (b1.From < b2.From);
        }
        #endregion

        #region Equal or (Grater / Less) than
        /// <summary>
        /// Сравнение больше или равно
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат</returns>
        public static bool operator >=(DateBorders b1, DateBorders b2) => (b1 == b2 || b1 > b2);
        /// <summary>
        /// Сравнение меньше или равно
        /// </summary>
        /// <param name="b1">Элемент 1</param>
        /// <param name="b2">Элемент 2</param>
        /// <returns>Результат</returns>
        public static bool operator <=(DateBorders b1, DateBorders b2) => (b1 == b2 || b1 < b2);
        #endregion

        #region override base methods (from object)
        /// <summary>
        /// Перегрузка сравнения на кавенство
        /// </summary>
        /// <param name="obj">Элемент с которым сравниваем</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is DateBorders other)
                return this == other;
            else
                return base.Equals(obj);
        }
        /// <summary>
        /// Приводим данный класс к строке и возвращаем его хешкод
        /// </summary>
        /// <returns>Хешкод строки</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        /// <summary>
        /// Перевод в строку текущего класса
        /// </summary>
        /// <returns>Строка дата С - дата По</returns>
        public override string ToString()
        {
            return $"{From}-{Till}";
        }
        #endregion

        /// <summary>
        /// Сравниваем текущий элемент с переданным
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is DateBorders borders)
            {
                if (this == borders)
                    return 0;
                else if (this < borders)
                    return -1;
                else
                    return 1;
            }
            else
            {
                throw new ArgumentException("object is not DateBorders");
            }
        }
    }
    /// <summary>
    /// Список коэффициентов
    /// </summary>
    public struct Coefficients
    {
        public double Payoff, ProfitFactor, AverageProfitFactor,
                      RecoveryFactor, AverageRecoveryFactor, PL, DD,
                      AltmanZScore, Custom;
        public int TotalTrades;
        public VaRData VaR;
        public MaxPLDD MaxPLDD;
        public Dictionary<DayOfWeek, DailyData> TradingDays;
    }
    /// <summary>
    /// коэффициенты VaR
    /// </summary>
    public struct VaRData
    {
        public double Q_90, Q_95, Q_99, Mx, Std;
    }
    /// <summary>
    /// Крайние точки (суммарная прибыль / убытки
    /// </summary>
    public struct MaxPLDD
    {
        /// <summary>
        /// Структура хранящая данные по прибыли или убытку
        /// </summary>
        public struct Max
        {
            public double Value;
            public int TotalTrades, ConsecutivesTrades;
        }

        public Max Profit, DD;
    }
    /// <summary>
    /// Данные торгов по конкретному дню
    /// </summary>
    public struct DailyData
    {
        /// <summary>
        /// Данные по прибыли или же убытку
        /// </summary>
        public struct Side
        {
            public double Value;
            public int Trades;
        }

        public Side Profit, DD;
    }
    #endregion
}
