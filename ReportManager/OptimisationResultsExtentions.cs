using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ReportManager
{
    /// <summary>
    /// методы расширения для коллекции с результатами оптимизаций
    /// </summary>
    public static class OptimisationResultsExtentions
    {
        /// <summary>
        /// Метод фильтрующий оптимизации
        /// </summary>
        /// <param name="results">текущая коллекция</param>
        /// <param name="compareData">Коллекция коэффициентов и типов фильтрации</param>
        /// <returns>Отфильтрованная коллекция</returns>
        public static IEnumerable<OptimisationResult> FiltreOptimisations(this IEnumerable<OptimisationResult> results,
                                                                          IDictionary<SortBy, KeyValuePair<CompareType, double>> compareData)
        {
            // Функция сортирующая результаты
            bool Compare(double _data, KeyValuePair<CompareType, double> compareParams)
            {
                // Результат сравнения
                bool ans = false;
                // сравнение на равенство
                if (compareParams.Key.HasFlag(CompareType.EqualTo))
                {
                    ans = compareParams.Value == _data;
                }
                // Сравнение на больше чем текущий
                if (!ans && compareParams.Key.HasFlag(CompareType.GraterThan))
                {
                    ans = _data > compareParams.Value;
                }
                // Сравнение на меньше чем текущий
                if (!ans && compareParams.Key.HasFlag(CompareType.LessThan))
                {
                    ans = _data < compareParams.Value;
                }

                return ans;
            }
            // Условие сортировки
            bool Sort(OptimisationResult x)
            {
                // Цикл по переданным параметрам сортировки
                foreach (var item in compareData)
                {
                    // проверка на соответствие переданного и текущего параметра
                    if (!Compare(x.GetResult(item.Key), item.Value))
                        return false;
                }

                return true;
            }

            // Фильтрация
            return results.Where(x => Sort(x));
        }
        /// <summary>
        /// Сортировка списка с результатами оптимизации
        /// </summary>
        /// <param name="results">Текущая коллекция с результатами</param>
        /// <param name="order">Направленность сортировки</param>
        /// <param name="sortingFlags">Список коэффициентов сортировки</param>
        /// <param name="sortMethod">Метод сортировки</param>
        /// <returns></returns>
        public static IEnumerable<OptimisationResult> SortOptimisations(this IEnumerable<OptimisationResult> results,
                                                                        OrderBy order, IEnumerable<SortBy> sortingFlags)
        {
            // Получаем уникальный список флагов для сортировки
            sortingFlags = sortingFlags.Distinct();
            // Проверяем наличие флагов
            if (sortingFlags.Count() == 0)
                return null;
            // Если флаг один то сортируем чисто по этому показателю
            if (sortingFlags.Count() == 1)
            {
                if (order == GetSortingDirection(sortingFlags.ElementAt(0)))
                    return results.OrderBy(x => x.GetResult(sortingFlags.ElementAt(0)));
                else
                    return results.OrderByDescending(x => x.GetResult(sortingFlags.ElementAt(0)));
            }

            // Формируем границы минимум и максимум по переданным флагам оптимизации
            Dictionary<SortBy, MinMax> Borders = sortingFlags.ToDictionary(x => x, x => new MinMax { Max = double.MinValue, Min = double.MaxValue });

            #region create Borders min max dictionary
            // Цикл по списку проходов оптимизаций
            for (int i = 0; i < results.Count(); i++)
            {
                // Цикл по флагам сортировки
                foreach (var item in sortingFlags)
                {
                    // получаем значение текущего коэффициента
                    double value = results.ElementAt(i).GetResult(item);
                    MinMax mm = Borders[item];
                    // Задаем значения минимум и максимум
                    mm.Max = Math.Max(mm.Max, value);
                    mm.Min = Math.Min(mm.Min, value);
                    Borders[item] = mm;
                }
            }
            #endregion

            // Вес взвешенной суммы нормированных коэффициентов
            double coef = (1.0 / Borders.Count);

            // Переводим список результатов оптимизации к массиву типа List
            // Так как с ним быстрее работать
            List<OptimisationResult> listOfResults = results.ToList();
            // Цикл по результатам оптимизации
            for (int i = 0; i < listOfResults.Count; i++)
            {
                // Присваиваем значение текущему коэффициент
                OptimisationResult data = listOfResults[i];
                // Зануляем текущий коэффициент сортировки
                data.SortBy = 0;
                // Проводим цикл сформированным границам максимумов и минимумов
                foreach (var item in Borders)
                {
                    // Получаем значение текущего результата
                    double value = listOfResults[i].GetResult(item.Key);
                    MinMax mm = item.Value;

                    // Если минимум меньше нуля - сдвигаемвсе данные на велечину отрицательного минимума
                    if (mm.Min < 0)
                    {
                        value += Math.Abs(mm.Min);
                        mm.Max += Math.Abs(mm.Min);
                    }

                    // Если максимум больше нуля - делаем подсччеты
                    if (mm.Max > 0)
                    {
                        // В зависимости от метода сортировки - высчитываем коэффициент
                        if (GetSortingDirection(item.Key) == OrderBy.Descending)
                        {
                            // высчитываем коэффициент для сортировки по убыванию
                            data.SortBy += (1 - value / mm.Max) * coef;
                        }
                        else
                        {
                            // Высчитываем коэффициент для сортировки по возрастанию
                            data.SortBy += value / mm.Max * coef;
                        }
                    }
                }
                // Замещаем значение текущего коэффициента коэффициентом с параметром сортировки
                listOfResults[i] = data;
            }

            // Сортируем в зависимости от переданного типа сортировки
            if (order == OrderBy.Ascending)
                return listOfResults.OrderBy(x => x.SortBy);
            else
                return listOfResults.OrderByDescending(x => x.SortBy);
        }

        /// <summary>
        /// МЕтод который возвращает дефолтное значение типов сортировки
        /// </summary>
        /// <param name="sortBy">Коэффициент по которому осртируем данные</param>
        /// <returns>Метод сортировки</returns>
        private static OrderBy GetSortingDirection(SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.Custom: return OrderBy.Ascending;
                case SortBy.Payoff: return OrderBy.Ascending;
                case SortBy.ProfitFactor: return OrderBy.Ascending; 
                case SortBy.AverageProfitFactor: return OrderBy.Ascending; 
                case SortBy.RecoveryFactor: return OrderBy.Ascending; 
                case SortBy.AverageRecoveryFactor: return OrderBy.Ascending; 
                case SortBy.PL: return OrderBy.Ascending;
                case SortBy.DD: return OrderBy.Ascending;
                case SortBy.AltmanZScore: return OrderBy.Descending; 
                case SortBy.TotalTrades: return OrderBy.Ascending;
                case SortBy.Q_90: return OrderBy.Ascending;
                case SortBy.Q_95: return OrderBy.Ascending;
                case SortBy.Q_99: return OrderBy.Ascending;
                case SortBy.Mx: return OrderBy.Ascending;
                case SortBy.Std: return OrderBy.Descending; 
                case SortBy.MaxProfit: return OrderBy.Ascending;
                case SortBy.MaxDD: return OrderBy.Ascending;
                case SortBy.MaxProfitTotalTrades: return OrderBy.Ascending; 
                case SortBy.MaxDDTotalTrades: return OrderBy.Descending;
                case SortBy.MaxProfitConsecutivesTrades: return OrderBy.Ascending;
                case SortBy.MaxDDConsecutivesTrades: return OrderBy.Descending; 
                case SortBy.AverageDailyProfit_Mn: return OrderBy.Ascending;
                case SortBy.AverageDailyDD_Mn: return OrderBy.Descending; 
                case SortBy.AverageDailyProfitTrades_Mn: return OrderBy.Ascending;
                case SortBy.AverageDailyDDTrades_Mn: return OrderBy.Descending; 
                case SortBy.AverageDailyProfit_Tu: return OrderBy.Ascending;
                case SortBy.AverageDailyDD_Tu: return OrderBy.Descending;
                case SortBy.AverageDailyProfitTrades_Tu: return OrderBy.Ascending;
                case SortBy.AverageDailyDDTrades_Tu: return OrderBy.Descending;
                case SortBy.AverageDailyProfit_We: return OrderBy.Ascending;
                case SortBy.AverageDailyDD_We: return OrderBy.Descending;
                case SortBy.AverageDailyProfitTrades_We: return OrderBy.Ascending;
                case SortBy.AverageDailyDDTrades_We: return OrderBy.Descending;
                case SortBy.AverageDailyProfit_Th: return OrderBy.Ascending;
                case SortBy.AverageDailyDD_Th: return OrderBy.Descending;
                case SortBy.AverageDailyProfitTrades_Th: return OrderBy.Ascending;
                case SortBy.AverageDailyDDTrades_Th: return OrderBy.Descending;
                case SortBy.AverageDailyProfit_Fr: return OrderBy.Ascending;
                case SortBy.AverageDailyDD_Fr: return OrderBy.Descending;
                case SortBy.AverageDailyProfitTrades_Fr: return OrderBy.Ascending;
                case SortBy.AverageDailyDDTrades_Fr: return OrderBy.Descending;
                default: throw new ArgumentException($"Unaxpected Sortby variable {sortBy}");
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/41384035/replace-insert-delete-operations-on-ienumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<T> Replace<T>(this IEnumerable<T> enumerable, int index, T value)
        {
            int current = 0;
            foreach (var item in enumerable)
            {
                yield return current == index ? value : item;
                current++;
            }
        }
        /// <summary>
        /// Метод пишуший отчет оптимизации в файл
        /// </summary>
        /// <param name="results">текущий список с результатами оптимизации</param>
        /// <param name="pathToBot">Путь до робота относительно папки с экспертами</param>
        /// <param name="currency">Валюта депозита</param>
        /// <param name="balance">Депозит</param>
        /// <param name="laverage">Кредитное плечо</param>
        /// <param name="pathToFile">Путь к файлу</param>
        public static void ReportWriter(this IEnumerable<OptimisationResult> results, string pathToBot,
                                        string currency, double balance,
                                        int laverage, string pathToFile)
        {
            // Удаляем файл если таковой существует
            if (File.Exists(pathToFile))
                File.Delete(pathToFile);

            // Создаем писатель 
            using (var xmlWriter = new XmlTextWriter(pathToFile, null))
            {
                // Установка формата документа
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.IndentChar = '\t';
                xmlWriter.Indentation = 1;

                xmlWriter.WriteStartDocument();

                // Корневая нода документа
                xmlWriter.WriteStartElement("Optimisatin_Report");

                // Пишем аттрибуты
                WriteAttribute(xmlWriter, "Created", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));


                // Пишем настройки оптимизатора в файл
                #region Optimiser settings section 
                xmlWriter.WriteStartElement("Optimiser_Settings");

                WriteItem(xmlWriter, "Bot", pathToBot); // путь к роботу
                WriteItem(xmlWriter, "Deposit", balance.ToString(), new Dictionary<string, string> { { "Currency", currency } }); // Валюта и депозит
                WriteItem(xmlWriter, "Laverage", laverage.ToString()); // кредитное плече

                xmlWriter.WriteEndElement();
                #endregion

                // Пишем в файл сами результаты оптимизаций
                #region Optimisation result section
                xmlWriter.WriteStartElement("Optimisation_Results");

                // Цикл по результатам оптимизаций
                foreach (var item in results)
                {
                    // Пишем конкретный результат
                    xmlWriter.WriteStartElement("Result");

                    // Пишем аттрибуты данного оптимизационного прозода
                    WriteAttribute(xmlWriter, "Symbol", item.report.Symbol); // Символ
                    WriteAttribute(xmlWriter, "TF", item.report.TF.ToString()); // Таймфрейм
                    WriteAttribute(xmlWriter, "Start_DT", item.report.DateBorders.From.DTToUnixDT().ToString()); // Дата начала оптимизации
                    WriteAttribute(xmlWriter, "Finish_DT", item.report.DateBorders.Till.DTToUnixDT().ToString()); // Дата завершения оптимизации

                    // Запись результата оптимизации
                    WriteResultItem(item, xmlWriter);

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                #endregion

                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            }
        }
        /// <summary>
        /// Запись конкретного оптимизационного прозода
        /// </summary>
        /// <param name="resultItem">Значение оптимизационного прохода</param>
        /// <param name="writer">Писатель</param>
        private static void WriteResultItem(OptimisationResult resultItem, XmlTextWriter writer)
        {
            // Запись коэффициентов
            #region Coefficients
            writer.WriteStartElement("Coefficients");

            // Пишем VaR
            #region VaR
            writer.WriteStartElement("VaR");

            WriteItem(writer, "90", resultItem.GetResult(SortBy.Q_90).ToString()); // Квантиль 90
            WriteItem(writer, "95", resultItem.GetResult(SortBy.Q_95).ToString()); // Квантиль 95
            WriteItem(writer, "99", resultItem.GetResult(SortBy.Q_99).ToString()); // Квантиль 99
            WriteItem(writer, "Mx", resultItem.GetResult(SortBy.Mx).ToString()); // Среднее по PL
            WriteItem(writer, "Std", resultItem.GetResult(SortBy.Std).ToString()); // среднеквадратическое отклонение по PL

            writer.WriteEndElement();
            #endregion

            // Пишем параметры PL / DD - крайние точки
            #region Max PL DD
            writer.WriteStartElement("Max_PL_DD");
            WriteItem(writer, "Profit", resultItem.GetResult(SortBy.MaxProfit).ToString()); // Суммарная прибыль
            WriteItem(writer, "DD", resultItem.GetResult(SortBy.MaxDD).ToString()); // Суммарный убыток
            WriteItem(writer, "Total Profit Trades", ((int)resultItem.GetResult(SortBy.MaxProfitTotalTrades)).ToString()); // Общее кол - во прибыльных трейдов
            WriteItem(writer, "Total Loose Trades", ((int)resultItem.GetResult(SortBy.MaxDDTotalTrades)).ToString()); // Общее кол - во убыточных трейдов
            WriteItem(writer, "Consecutive Wins", ((int)resultItem.GetResult(SortBy.MaxProfitConsecutivesTrades)).ToString()); // Прибыльных трейдов подряд
            WriteItem(writer, "Consecutive Loose", ((int)resultItem.GetResult(SortBy.MaxDDConsecutivesTrades)).ToString()); // Убыточный трейдов подряд
            writer.WriteEndElement();
            #endregion

            // Пишем результаты торгов по дням
            #region Trading_Days

            // Метод пишуший результаты торгов
            void AddDay(string Day, double Profit, double DD, int ProfitTrades, int DDTrades)
            {
                writer.WriteStartElement(Day);

                WriteItem(writer, "Profit", Profit.ToString()); // прибыли
                WriteItem(writer, "DD", DD.ToString()); // убытки
                WriteItem(writer, "Number Of Profit Trades", ProfitTrades.ToString()); // кол - во прибыльных трейдов
                WriteItem(writer, "Number Of Loose Trades", DDTrades.ToString()); // кол - во убыточных трейдов

                writer.WriteEndElement();
            }

            writer.WriteStartElement("Trading_Days");

            // Пн
            AddDay("Mn", resultItem.GetResult(SortBy.AverageDailyProfit_Mn),
                         resultItem.GetResult(SortBy.AverageDailyDD_Mn),
                         (int)resultItem.GetResult(SortBy.AverageDailyProfitTrades_Mn),
                         (int)resultItem.GetResult(SortBy.AverageDailyDDTrades_Mn));
            // Вт
            AddDay("Tu", resultItem.GetResult(SortBy.AverageDailyProfit_Tu),
                         resultItem.GetResult(SortBy.AverageDailyDD_Tu),
                         (int)resultItem.GetResult(SortBy.AverageDailyProfitTrades_Tu),
                         (int)resultItem.GetResult(SortBy.AverageDailyDDTrades_Tu));
            // Ср
            AddDay("We", resultItem.GetResult(SortBy.AverageDailyProfit_We),
                         resultItem.GetResult(SortBy.AverageDailyDD_We),
                         (int)resultItem.GetResult(SortBy.AverageDailyProfitTrades_We),
                         (int)resultItem.GetResult(SortBy.AverageDailyDDTrades_We));
            // Чт
            AddDay("Th", resultItem.GetResult(SortBy.AverageDailyProfit_Th),
                         resultItem.GetResult(SortBy.AverageDailyDD_Th),
                         (int)resultItem.GetResult(SortBy.AverageDailyProfitTrades_Th),
                         (int)resultItem.GetResult(SortBy.AverageDailyDDTrades_Th));
            // Пт
            AddDay("Fr", resultItem.GetResult(SortBy.AverageDailyProfit_Fr),
                         resultItem.GetResult(SortBy.AverageDailyDD_Fr),
                         (int)resultItem.GetResult(SortBy.AverageDailyProfitTrades_Fr),
                         (int)resultItem.GetResult(SortBy.AverageDailyDDTrades_Fr));

            writer.WriteEndElement();
            #endregion

            // Пишем остальные коэййициенты
            WriteItem(writer, "Custom", resultItem.GetResult(SortBy.Custom).ToString());
            WriteItem(writer, "Payoff", resultItem.GetResult(SortBy.Payoff).ToString());
            WriteItem(writer, "Profit factor", resultItem.GetResult(SortBy.ProfitFactor).ToString());
            WriteItem(writer, "Average Profit factor", resultItem.GetResult(SortBy.AverageProfitFactor).ToString());
            WriteItem(writer, "Recovery factor", resultItem.GetResult(SortBy.RecoveryFactor).ToString());
            WriteItem(writer, "Average Recovery factor", resultItem.GetResult(SortBy.AverageRecoveryFactor).ToString());
            WriteItem(writer, "Total trades", ((int)resultItem.GetResult(SortBy.TotalTrades)).ToString());
            WriteItem(writer, "PL", resultItem.GetResult(SortBy.PL).ToString());
            WriteItem(writer, "DD", resultItem.GetResult(SortBy.DD).ToString());
            WriteItem(writer, "Altman Z Score", resultItem.GetResult(SortBy.AltmanZScore).ToString());

            writer.WriteEndElement();
            #endregion

            // Пишем коэффициенты робота
            #region Bot params
            foreach (var item in resultItem.report.BotParams)
            {
                WriteItem(writer, item.Key, item.Value);
            }
            #endregion
        }
        /// <summary>
        /// Запись аттрибута
        /// </summary>
        /// <param name="writer">Писатель</param>
        /// <param name="attrName">Имя аттрибута</param>
        /// <param name="attrValue">Значение аттрибута</param>
        private static void WriteAttribute(XmlTextWriter writer, string attrName, string attrValue)
        {
            writer.WriteStartAttribute(attrName);
            writer.WriteString(attrValue);
            writer.WriteEndAttribute();
        }
        /// <summary>
        /// Запись элемента
        /// </summary>
        /// <param name="writer">Писатель</param>
        /// <param name="Name">Имя параметра</param>
        /// <param name="Value">Значение параметра</param>
        /// <param name="Attributes">Список аттрибутов</param>
        private static void WriteItem(XmlTextWriter writer, string Name, string Value, Dictionary<string, string> Attributes = null)
        {
            // Пишем начало элемента
            writer.WriteStartElement("Item");

            // Пишем аттрибут - имя элемента
            WriteAttribute(writer, "Name", Name);

            // Пишем аттрибуты
            if (Attributes != null)
            {
                foreach (var item in Attributes)
                {
                    WriteAttribute(writer, item.Key, item.Value);
                }
            }

            writer.WriteString(Value);

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// структура хранения минимальных / максимальных данных
    /// </summary>
    internal struct MinMax
    {
        public double Max, Min;
    }
    /// <summary>
    /// Сортируемые коэффициенты 
    /// </summary>
    public enum SortBy
    {
        Custom,
        Payoff,
        ProfitFactor,
        AverageProfitFactor,
        RecoveryFactor,
        AverageRecoveryFactor,
        PL,
        DD,
        AltmanZScore,
        TotalTrades,
        Q_90,
        Q_95,
        Q_99,
        Mx,
        Std,
        MaxProfit,
        MaxDD,
        MaxProfitTotalTrades,
        MaxDDTotalTrades,
        MaxProfitConsecutivesTrades,
        MaxDDConsecutivesTrades,
        AverageDailyProfit_Mn,
        AverageDailyDD_Mn,
        AverageDailyProfitTrades_Mn,
        AverageDailyDDTrades_Mn,
        AverageDailyProfit_Tu,
        AverageDailyDD_Tu,
        AverageDailyProfitTrades_Tu,
        AverageDailyDDTrades_Tu,
        AverageDailyProfit_We,
        AverageDailyDD_We,
        AverageDailyProfitTrades_We,
        AverageDailyDDTrades_We,
        AverageDailyProfit_Th,
        AverageDailyDD_Th,
        AverageDailyProfitTrades_Th,
        AverageDailyDDTrades_Th,
        AverageDailyProfit_Fr,
        AverageDailyDD_Fr,
        AverageDailyProfitTrades_Fr,
        AverageDailyDDTrades_Fr
    }
    /// <summary>
    /// направление сортировки
    /// </summary>
    public enum OrderBy
    {
        Ascending,// по возрастанию
        Descending// по убыванию
    }

    /// <summary>
    /// Структура хранящая отчет прохода одной конкретной оптимизации и способ сортировки
    /// </summary>
    public struct OptimisationResult
    {
        /// <summary>
        /// Отчет прозода оптимизации
        /// </summary>
        public ReportItem report;
        /// <summary>
        /// коэффициент сортировки
        /// </summary>
        public double SortBy;

        /// <summary>
        /// Оператор неявного приведения типа от прозода оптимизации к текущему типу
        /// </summary>
        /// <param name="item">Отчет прозода оптимизации</param>
        public static implicit operator OptimisationResult(ReportItem item)
        {
            return new OptimisationResult { report = item, SortBy = 0 };
        }
        /// <summary>
        /// Оператор явного приведения типов от текущего к структуре оптимизационного прохода
        /// </summary>
        /// <param name="optimisationResult">текущий тип</param>
        public static explicit operator ReportItem(OptimisationResult optimisationResult)
        {
            return optimisationResult.report;
        }
        /// <summary>
        /// Метод возвращающий значение коэффициента по переданному типу коэффициента
        /// </summary>
        /// <param name="resultType">тип коэффициента</param>
        /// <returns>значение коэффициента</returns>
        public double GetResult(SortBy resultType)
        {
            switch (resultType)
            {
                case ReportManager.SortBy.Custom: return report.OptimisationCoefficients.Custom;
                case ReportManager.SortBy.Payoff: return report.OptimisationCoefficients.Payoff;
                case ReportManager.SortBy.ProfitFactor: return report.OptimisationCoefficients.ProfitFactor;
                case ReportManager.SortBy.AverageProfitFactor: return report.OptimisationCoefficients.AverageProfitFactor;
                case ReportManager.SortBy.RecoveryFactor: return report.OptimisationCoefficients.RecoveryFactor;
                case ReportManager.SortBy.AverageRecoveryFactor: return report.OptimisationCoefficients.AverageRecoveryFactor;
                case ReportManager.SortBy.PL: return report.OptimisationCoefficients.PL;
                case ReportManager.SortBy.DD: return report.OptimisationCoefficients.DD;
                case ReportManager.SortBy.AltmanZScore: return report.OptimisationCoefficients.AltmanZScore;
                case ReportManager.SortBy.TotalTrades: return report.OptimisationCoefficients.TotalTrades;
                case ReportManager.SortBy.Q_90: return report.OptimisationCoefficients.VaR.Q_90;
                case ReportManager.SortBy.Q_95: return report.OptimisationCoefficients.VaR.Q_95;
                case ReportManager.SortBy.Q_99: return report.OptimisationCoefficients.VaR.Q_99;
                case ReportManager.SortBy.Mx: return report.OptimisationCoefficients.VaR.Mx;
                case ReportManager.SortBy.Std: return report.OptimisationCoefficients.VaR.Std;
                case ReportManager.SortBy.MaxProfit: return report.OptimisationCoefficients.MaxPLDD.Profit.Value;
                case ReportManager.SortBy.MaxDD: return report.OptimisationCoefficients.MaxPLDD.DD.Value;
                case ReportManager.SortBy.MaxProfitTotalTrades: return report.OptimisationCoefficients.MaxPLDD.Profit.TotalTrades;
                case ReportManager.SortBy.MaxDDTotalTrades: return report.OptimisationCoefficients.MaxPLDD.DD.TotalTrades;
                case ReportManager.SortBy.MaxProfitConsecutivesTrades: return report.OptimisationCoefficients.MaxPLDD.Profit.ConsecutivesTrades;
                case ReportManager.SortBy.MaxDDConsecutivesTrades: return report.OptimisationCoefficients.MaxPLDD.DD.ConsecutivesTrades;

                case ReportManager.SortBy.AverageDailyProfit_Mn: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].Profit.Value;
                case ReportManager.SortBy.AverageDailyDD_Mn: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].DD.Value;
                case ReportManager.SortBy.AverageDailyProfitTrades_Mn: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].Profit.Trades;
                case ReportManager.SortBy.AverageDailyDDTrades_Mn: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].DD.Trades;

                case ReportManager.SortBy.AverageDailyProfit_Tu: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Tuesday].Profit.Value;
                case ReportManager.SortBy.AverageDailyDD_Tu: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Tuesday].DD.Value;
                case ReportManager.SortBy.AverageDailyProfitTrades_Tu: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Tuesday].Profit.Trades;
                case ReportManager.SortBy.AverageDailyDDTrades_Tu: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Tuesday].DD.Trades;

                case ReportManager.SortBy.AverageDailyProfit_We: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].Profit.Value;
                case ReportManager.SortBy.AverageDailyDD_We: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].DD.Value;
                case ReportManager.SortBy.AverageDailyProfitTrades_We: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].Profit.Trades;
                case ReportManager.SortBy.AverageDailyDDTrades_We: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].DD.Trades;

                case ReportManager.SortBy.AverageDailyProfit_Th: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].Profit.Value;
                case ReportManager.SortBy.AverageDailyDD_Th: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Value;
                case ReportManager.SortBy.AverageDailyProfitTrades_Th: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].Profit.Trades;
                case ReportManager.SortBy.AverageDailyDDTrades_Th: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Trades;

                case ReportManager.SortBy.AverageDailyProfit_Fr: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].Profit.Value;
                case ReportManager.SortBy.AverageDailyDD_Fr: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].DD.Value;
                case ReportManager.SortBy.AverageDailyProfitTrades_Fr: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].Profit.Trades;
                case ReportManager.SortBy.AverageDailyDDTrades_Fr: return report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].DD.Trades;

                default: throw new ArgumentException($"Unaxpected SortBy parametr {resultType}");
            }
        }
        /// <summary>
        /// Метод добавляющий параметр текущий параметр в существующий файл или же создает новый файл с текущим параметром
        /// </summary>
        /// <param name="pathToBot">Относительный путь до робота от папки с экспертами</param>
        /// <param name="currency">Валюта депозита</param>
        /// <param name="balance">Баланс</param>
        /// <param name="laverage">Кредитное плече</param>
        /// <param name="pathToFile">Путь до файла</param>
        public void WriteResult(string pathToBot,
                                string currency, double balance,
                                int laverage, string pathToFile)
        {
            try
            {
                ReportWriter.SetReportItem(report);

                string error = ReportWriter.Write(pathToBot, currency, balance, laverage, pathToFile, report.Symbol, report.TF,
                                   report.DateBorders.From.DTToUnixDT(), report.DateBorders.Till.DTToUnixDT());
                if (error != "")
                    throw new Exception(error);
            }
            catch (Exception e)
            {
                ReportWriter.ClearReportItem();
                throw e;
            }
        }
        /// <summary>
        /// Перегрузка оператора сравнения на раверство
        /// </summary>
        /// <param name="result1">сравниваемый параметр 1</param>
        /// <param name="result2">сравниваемый параметр 2</param>
        /// <returns>результат сравнения</returns>
        public static bool operator ==(OptimisationResult result1, OptimisationResult result2)
        {
            foreach (var item in result1.report.BotParams)
            {
                if (!result2.report.BotParams.ContainsKey(item.Key))
                    return false;
                if (result2.report.BotParams[item.Key] != item.Value)
                    return false;
            }

            return true;
        }
        /// <summary>
        /// Перегрузка оператора сравнения на неравенство
        /// </summary>
        /// <param name="result1">сравниваемый параметр 1</param>
        /// <param name="result2">сравниваемый параметр 2</param>
        /// <returns>результат сравнения</returns>
        public static bool operator !=(OptimisationResult result1, OptimisationResult result2)
        {
            return !(result1 == result2);
        }
        /// <summary>
        /// Перегрузка оператора сравнения базового типа
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is OptimisationResult other)
            {
                return this == other;
            }
            else
                return base.Equals(obj);
        }
        /// <summary>
        /// Метод взятия хешкода базового типа
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    /// <summary>
    /// Тип фильтрации
    /// </summary>
    [Flags]
    public enum CompareType
    {
        GraterThan = 1, // больше 
        LessThan = 2, // меньше
        EqualTo = 4 // равно
    }
}
