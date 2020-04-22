using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using System.Threading;

namespace ReportManager
{
    /// <summary>
    /// Класс который используется в терминале и в методах расширения 
    /// для постепенного добавления данных результатов оптимизаций в файл
    /// </summary>
    public class ReportWriter
    {
        /// <summary>
        /// Метод создающий файл если он не был создан
        /// </summary>
        /// <param name="pathToBot">Путь до робота</param>
        /// <param name="currency">Валюта депозита</param>
        /// <param name="balance">Баланс</param>
        /// <param name="laverage">Кредитное плечо</param>
        /// <param name="pathToFile">Путь до файла</param>
        private static void CreateFileIfNotExists(string pathToBot, string currency, double balance, int laverage, string pathToFile)
        {
            if (File.Exists(pathToFile))
                return;
            using (var xmlWriter = new XmlTextWriter(pathToFile, null))
            {
                // установка формата документа
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.IndentChar = '\t';
                xmlWriter.Indentation = 1;

                xmlWriter.WriteStartDocument();

                // Создаем корень документа
                #region Document root
                xmlWriter.WriteStartElement("Optimisatin_Report");

                // Пишем дату создания
                xmlWriter.WriteStartAttribute("Created");
                xmlWriter.WriteString(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                xmlWriter.WriteEndAttribute();

                #region Optimiser settings section 
                // Настройки оптимизатора
                xmlWriter.WriteStartElement("Optimiser_Settings");

                // Путь к роботу
                WriteItem(xmlWriter, "Bot", pathToBot);
                // Депозит
                WriteItem(xmlWriter, "Deposit", balance.ToString(), new Dictionary<string, string> { { "Currency", currency } });
                // Кредитное плечо
                WriteItem(xmlWriter, "Laverage", laverage.ToString());

                xmlWriter.WriteEndElement();
                #endregion

                #region Optimisation resultssection
                // корневая нода списка результатов оптимизации
                xmlWriter.WriteStartElement("Optimisation_Results");
                xmlWriter.WriteEndElement();
                #endregion

                xmlWriter.WriteEndElement();
                #endregion

                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            }
        }

        /// <summary>
        /// Запись элемента в файл
        /// </summary>
        /// <param name="writer">Писатель</param>
        /// <param name="Name">Имя элемента</param>
        /// <param name="Value">Значение элемента</param>
        /// <param name="Attributes">Аттрибыты</param>
        private static void WriteItem(XmlTextWriter writer, string Name, string Value, Dictionary<string, string> Attributes = null)
        {
            writer.WriteStartElement("Item");

            writer.WriteStartAttribute("Name");
            writer.WriteString(Name);
            writer.WriteEndAttribute();

            if (Attributes != null)
            {
                foreach (var item in Attributes)
                {
                    writer.WriteStartAttribute(item.Key);
                    writer.WriteString(item.Value);
                    writer.WriteEndAttribute();
                }
            }

            writer.WriteString(Value);

            writer.WriteEndElement();
        }
        /// <summary>
        /// Запись аттрибутов в файл
        /// </summary>
        /// <param name="item">нода</param>
        /// <param name="xmlDoc">Документ</param>
        /// <param name="Attributes">Аттрибуты</param>
        private static void FillInAttributes(XmlNode item, XmlDocument xmlDoc, Dictionary<string, string> Attributes)
        {
            if (Attributes != null)
            {
                foreach (var attr in Attributes)
                {
                    XmlAttribute attribute = xmlDoc.CreateAttribute(attr.Key);
                    attribute.Value = attr.Value;
                    item.Attributes.Append(attribute);
                }
            }
        }
        /// <summary>
        /// Добавить секцию
        /// </summary>
        /// <param name="xmlDoc">Документ</param>
        /// <param name="xpath_parentSection">xpath для выбора родительской ноды</param>
        /// <param name="sectionName">Имя секции</param>
        /// <param name="Attributes">Аттрибут</param>
        private static void AppendSection(XmlDocument xmlDoc, string xpath_parentSection,
                                          string sectionName, Dictionary<string, string> Attributes = null)
        {
            XmlNode section = xmlDoc.SelectSingleNode(xpath_parentSection);
            XmlNode item = xmlDoc.CreateElement(sectionName);

            FillInAttributes(item, xmlDoc, Attributes);

            section.AppendChild(item);
        }
        /// <summary>
        /// Запись элемента
        /// </summary>
        /// <param name="xmlDoc">Документ</param>
        /// <param name="xpath_parentSection">xpath для выбора родительской ноды</param>
        /// <param name="name">Имя элемента</param>
        /// <param name="value">значение</param>
        /// <param name="Attributes">Аттрибуты</param>
        private static void WriteItem(XmlDocument xmlDoc, string xpath_parentSection, string name,
                                      string value, Dictionary<string, string> Attributes = null)
        {
            XmlNode section = xmlDoc.SelectSingleNode(xpath_parentSection);
            XmlNode item = xmlDoc.CreateElement(name);
            item.InnerText = value;

            FillInAttributes(item, xmlDoc, Attributes);

            section.AppendChild(item);
        }
        /// <summary>
        /// временный хранитель (накопитель) данных
        /// </summary>
        private static ReportItem ReportItem;
        /// <summary>
        /// отчистка временного зранителя данных
        /// </summary>
        public static void ClearReportItem()
        {
            ReportItem = new ReportItem();
        }
        public static void SetReportItem(ReportItem item) { ReportItem = item; }
        /// <summary>
        /// Добавление параметра робота
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        public static void AppendBotParam(string name, string value)
        {
            if (ReportItem.BotParams == null)
                ReportItem.BotParams = new Dictionary<string, string>();

            if (!ReportItem.BotParams.ContainsKey(name))
                ReportItem.BotParams.Add(name, value);
            else
                ReportItem.BotParams[name] = value;
        }
        /// <summary>
        /// Добавление основного списка коэффициентов
        /// </summary>
        /// <param name="payoff"></param>
        /// <param name="profitFactor"></param>
        /// <param name="averageProfitFactor"></param>
        /// <param name="recoveryFactor"></param>
        /// <param name="averageRecoveryFactor"></param>
        /// <param name="totalTrades"></param>
        /// <param name="pl"></param>
        /// <param name="dd"></param>
        /// <param name="altmanZScore"></param>
        public static void AppendMainCoef(double customCoef,
                                            double payoff,
                                            double profitFactor,
                                            double averageProfitFactor,
                                            double recoveryFactor,
                                            double averageRecoveryFactor,
                                            int totalTrades,
                                            double pl,
                                            double dd,
                                            double altmanZScore)
        {

            ReportItem.OptimisationCoefficients.Custom = customCoef;
            ReportItem.OptimisationCoefficients.Payoff = payoff;
            ReportItem.OptimisationCoefficients.ProfitFactor = profitFactor;
            ReportItem.OptimisationCoefficients.AverageProfitFactor = averageProfitFactor;
            ReportItem.OptimisationCoefficients.RecoveryFactor = recoveryFactor;
            ReportItem.OptimisationCoefficients.AverageRecoveryFactor = averageRecoveryFactor;
            ReportItem.OptimisationCoefficients.TotalTrades = totalTrades;
            ReportItem.OptimisationCoefficients.PL = pl;
            ReportItem.OptimisationCoefficients.DD = dd;
            ReportItem.OptimisationCoefficients.AltmanZScore = altmanZScore;
        }
        /// <summary>
        /// Добавление VaR
        /// </summary>
        /// <param name="Q_90"></param>
        /// <param name="Q_95"></param>
        /// <param name="Q_99"></param>
        /// <param name="Mx"></param>
        /// <param name="Std"></param>
        public static void AppendVaR(double Q_90, double Q_95,
                                     double Q_99, double Mx, double Std)
        {
            ReportItem.OptimisationCoefficients.VaR.Q_90 = Q_90;
            ReportItem.OptimisationCoefficients.VaR.Q_95 = Q_95;
            ReportItem.OptimisationCoefficients.VaR.Q_99 = Q_99;
            ReportItem.OptimisationCoefficients.VaR.Mx = Mx;
            ReportItem.OptimisationCoefficients.VaR.Std = Std;
        }
        /// <summary>
        /// Добавление суммарных PL / DD и сопутствующих значений
        /// </summary>
        /// <param name="profit"></param>
        /// <param name="dd"></param>
        /// <param name="totalProfitTrades"></param>
        /// <param name="totalLooseTrades"></param>
        /// <param name="consecutiveWins"></param>
        /// <param name="consecutiveLoose"></param>
        public static void AppendMaxPLDD(double profit, double dd,
                                         int totalProfitTrades, int totalLooseTrades,
                                         int consecutiveWins, int consecutiveLoose)
        {
            ReportItem.OptimisationCoefficients.MaxPLDD.Profit.Value = profit;
            ReportItem.OptimisationCoefficients.MaxPLDD.DD.Value = dd;
            ReportItem.OptimisationCoefficients.MaxPLDD.Profit.TotalTrades = totalProfitTrades;
            ReportItem.OptimisationCoefficients.MaxPLDD.DD.TotalTrades = totalLooseTrades;
            ReportItem.OptimisationCoefficients.MaxPLDD.Profit.ConsecutivesTrades = consecutiveWins;
            ReportItem.OptimisationCoefficients.MaxPLDD.DD.ConsecutivesTrades = consecutiveLoose;
        }
        /// <summary>
        /// Добавление конертного для
        /// </summary>
        /// <param name="day"></param>
        /// <param name="profit"></param>
        /// <param name="dd"></param>
        /// <param name="numberOfProfitTrades"></param>
        /// <param name="numberOfLooseTrades"></param>
        public static void AppendDay(int day,
                                     double profit, double dd,
                                     int numberOfProfitTrades,
                                     int numberOfLooseTrades)
        {
            if (day == 6 || day == 0)
                return;

            if (ReportItem.OptimisationCoefficients.TradingDays == null)
                ReportItem.OptimisationCoefficients.TradingDays = new Dictionary<DayOfWeek, DailyData>();

            switch (day)
            {
                case 1:
                    {
                        ReportItem.OptimisationCoefficients.TradingDays
                            .Add(DayOfWeek.Monday, GetDailyData(profit, dd, numberOfProfitTrades, numberOfLooseTrades));
                    }
                    break;
                case 2:
                    {
                        ReportItem.OptimisationCoefficients.TradingDays
                            .Add(DayOfWeek.Tuesday, GetDailyData(profit, dd, numberOfProfitTrades, numberOfLooseTrades));
                    }
                    break;
                case 3:
                    {
                        ReportItem.OptimisationCoefficients.TradingDays
                            .Add(DayOfWeek.Wednesday, GetDailyData(profit, dd, numberOfProfitTrades, numberOfLooseTrades));
                    }
                    break;
                case 4:
                    {
                        ReportItem.OptimisationCoefficients.TradingDays
                            .Add(DayOfWeek.Thursday, GetDailyData(profit, dd, numberOfProfitTrades, numberOfLooseTrades));
                    }
                    break;
                case 5:
                    {
                        ReportItem.OptimisationCoefficients.TradingDays
                            .Add(DayOfWeek.Friday, GetDailyData(profit, dd, numberOfProfitTrades, numberOfLooseTrades));
                    }
                    break;
            }
        }
        /// <summary>
        /// Получение структуры данных дневных торгов
        /// из переданныз данных
        /// </summary>
        /// <param name="profit"></param>
        /// <param name="dd"></param>
        /// <param name="numberOfProfitTrades"></param>
        /// <param name="numberOfLooseTrades"></param>
        /// <returns></returns>
        private static DailyData GetDailyData(double profit, double dd,
                                              int numberOfProfitTrades,
                                              int numberOfLooseTrades)
        {
            return new DailyData
            {
                Profit = new DailyData.Side
                {
                    Value = profit,
                    Trades = numberOfProfitTrades
                },
                DD = new DailyData.Side
                {
                    Value = dd,
                    Trades = numberOfLooseTrades
                }
            };
        }
        /// <summary>
        /// Запись результатов торгов в файл
        /// </summary>
        /// <param name="pathToBot">Путь к боту</param>
        /// <param name="currency">Валюта депозита</param>
        /// <param name="balance">Баланс</param>
        /// <param name="laverage">Кредитное плечо</param>
        /// <param name="pathToFile">Путь до файла</param>
        /// <param name="symbol">Символ</param>
        /// <param name="tf">Таймфрейм</param>
        /// <param name="StartDT">Дата начала торгов</param>
        /// <param name="FinishDT">Дата завершения торгов</param>
        public static void Write(string pathToBot, string currency, double balance,
                                 int laverage, string pathToFile, string symbol, int tf,
                                 ulong StartDT, ulong FinishDT)
        {
            // Создаем файл если он не существует
            CreateFileIfNotExists(pathToBot, currency, balance, laverage, pathToFile);

            ReportItem.Symbol = symbol;
            ReportItem.TF = tf;

            // Создаем дакумент и читем с его помощью файл
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathToFile);

            #region Append result section
            // Пишем запрос на переход в секцию с резкльтатами оптимизаций 
            string xpath = "Optimisatin_Report/Optimisation_Results";
            // Добавляем новую секцию с резкльтатами оптимизации
            AppendSection(xmlDoc, xpath, "Result",
                          new Dictionary<string, string>
                          {
                              { "Symbol", symbol },
                              { "TF", tf.ToString() },
                              { "Start_DT", StartDT.ToString() },
                              { "Finish_DT", FinishDT.ToString() }
                          });
            // Добавляем секцию с коэффициентами оптимизаций
            AppendSection(xmlDoc, $"{xpath}/Result[last()]", "Coefficients");
            // Добавляем секцию с  VaR
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients", "VaR");
            // Добавляем секцию с суммарными PL / DD
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients", "Max_PL_DD");
            // Добавляем секцию с результатами торгов по дням
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients", "Trading_Days");
            // Добавляем секцию с результатами торгов в Пн
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients/Trading_Days", "Mn");
            // Добавляем секцию с результатами торгов во Вт
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients/Trading_Days", "Tu");
            // Добавляем секцию с результатами торгов в Ср
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients/Trading_Days", "We");
            // Добавляем секцию с результатами торгов в Чт
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients/Trading_Days", "Th");
            // Добавляем секцию с результатами торгов в Пт
            AppendSection(xmlDoc, $"{xpath}/Result[last()]/Coefficients/Trading_Days", "Fr");
            #endregion


            #region Append Bot params
            // Пробегаемся по параметрам робота
            if (ReportItem.BotParams != null)
            {
                foreach (var item in ReportItem.BotParams)
                {
                    // Пишем выбранный параметр робота
                    WriteItem(xmlDoc, "Optimisatin_Report/Optimisation_Results/Result[last()]",
                              "Item", item.Value, new Dictionary<string, string> { { "Name", item.Key } });
                }
            }
            #endregion

            #region Append main coef
            // Задаем путь к ноде с коэффициентами
            xpath = "Optimisatin_Report/Optimisation_Results/Result[last()]/Coefficients";

            // Сохраняем коэфициенты
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.Custom.ToString(), new Dictionary<string, string> { { "Name", "Custom" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.Payoff.ToString(), new Dictionary<string, string> { { "Name", "Payoff" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.ProfitFactor.ToString(), new Dictionary<string, string> { { "Name", "Profit factor" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.AverageProfitFactor.ToString(), new Dictionary<string, string> { { "Name", "Average Profit factor" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.RecoveryFactor.ToString(), new Dictionary<string, string> { { "Name", "Recovery factor" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.AverageRecoveryFactor.ToString(), new Dictionary<string, string> { { "Name", "Average Recovery factor" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.TotalTrades.ToString(), new Dictionary<string, string> { { "Name", "Total trades" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.PL.ToString(), new Dictionary<string, string> { { "Name", "PL" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.DD.ToString(), new Dictionary<string, string> { { "Name", "DD" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.AltmanZScore.ToString(), new Dictionary<string, string> { { "Name", "Altman Z Score" } });
            #endregion

            #region Append VaR
            // Задаем путь к ноде с VaR
            xpath = "Optimisatin_Report/Optimisation_Results/Result[last()]/Coefficients/VaR";

            // Созраняем результаты VaR
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.VaR.Q_90.ToString(), new Dictionary<string, string> { { "Name", "90" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.VaR.Q_95.ToString(), new Dictionary<string, string> { { "Name", "95" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.VaR.Q_99.ToString(), new Dictionary<string, string> { { "Name", "99" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.VaR.Mx.ToString(), new Dictionary<string, string> { { "Name", "Mx" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.VaR.Std.ToString(), new Dictionary<string, string> { { "Name", "Std" } });
            #endregion

            #region Append max PL and DD
            // Задаем путь к ноде с суммарной PL / DD
            xpath = "Optimisatin_Report/Optimisation_Results/Result[last()]/Coefficients/Max_PL_DD";

            // Сохраняем коэффициенты
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.Profit.Value.ToString(), new Dictionary<string, string> { { "Name", "Profit" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.DD.Value.ToString(), new Dictionary<string, string> { { "Name", "DD" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.Profit.TotalTrades.ToString(), new Dictionary<string, string> { { "Name", "Total Profit Trades" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.DD.TotalTrades.ToString(), new Dictionary<string, string> { { "Name", "Total Loose Trades" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.Profit.ConsecutivesTrades.ToString(), new Dictionary<string, string> { { "Name", "Consecutive Wins" } });
            WriteItem(xmlDoc, xpath, "Item", ReportItem.OptimisationCoefficients.MaxPLDD.DD.ConsecutivesTrades.ToString(), new Dictionary<string, string> { { "Name", "Consecutive Loose" } });
            #endregion

            #region Append Days
            foreach (var item in ReportItem.OptimisationCoefficients.TradingDays)
            {
                // Задаем путь к ноде конкретного дня
                xpath = "Optimisatin_Report/Optimisation_Results/Result[last()]/Coefficients/Trading_Days";
                // Выбираем день
                switch (item.Key)
                {
                    case DayOfWeek.Monday: xpath += "/Mn"; break;
                    case DayOfWeek.Tuesday: xpath += "/Tu"; break;
                    case DayOfWeek.Wednesday: xpath += "/We"; break;
                    case DayOfWeek.Thursday: xpath += "/Th"; break;
                    case DayOfWeek.Friday: xpath += "/Fr"; break;
                }

                // Созраняем результаты
                WriteItem(xmlDoc, xpath, "Item", item.Value.Profit.Value.ToString(), new Dictionary<string, string> { { "Name", "Profit" } });
                WriteItem(xmlDoc, xpath, "Item", item.Value.DD.Value.ToString(), new Dictionary<string, string> { { "Name", "DD" } });
                WriteItem(xmlDoc, xpath, "Item", item.Value.Profit.Trades.ToString(), new Dictionary<string, string> { { "Name", "Number Of Profit Trades" } });
                WriteItem(xmlDoc, xpath, "Item", item.Value.DD.Trades.ToString(), new Dictionary<string, string> { { "Name", "Number Of Loose Trades" } });
            }
            #endregion

            // Перезаписываем файл с внесенными изменениями
            xmlDoc.Save(pathToFile);

            // Отчищаем переменную где зранились записанные в файл результаты
            ClearReportItem();
        }
        /// <summary>
        /// Запись в файл с блокировкой через именованный мьютекс
        /// </summary>
        /// <param name="mutexName">Имя мьютекса</param>
        /// <param name="pathToBot">Путь к боту</param>
        /// <param name="currency">Валюта депозита</param>
        /// <param name="balance">Баланс</param>
        /// <param name="laverage">Кредитное плечо</param>
        /// <param name="pathToFile">Путь до файла</param>
        /// <param name="symbol">Символ</param>
        /// <param name="tf">Таймфрейм</param>
        /// <param name="StartDT">Дата начала торгов</param>
        /// <param name="FinishDT">Дата завершения торгов</param>
        /// <returns></returns>
        public static string MutexWriter(string mutexName, string pathToBot, string currency, double balance,
                                       int laverage, string pathToFile, string symbol, int tf,
                                       ulong StartDT, ulong FinishDT)
        {
            string ans = "";
            // Блокировка мьютекса
            Mutex m = new Mutex(false, mutexName);
            m.WaitOne();
            try
            {
                // запись в файл
                Write(pathToBot, currency, balance, laverage, pathToFile, symbol, tf, StartDT, FinishDT);
            }
            catch (Exception e)
            {
                // Ловим ошибку если она была
                ans = e.Message;
            }

            // Освобождаем мьютекс
            m.ReleaseMutex();
            // Возвращаем тикст ошибки
            return ans;
        }
    }

}
