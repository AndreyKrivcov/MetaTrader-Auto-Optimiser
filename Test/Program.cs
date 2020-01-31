using System;
using System.Runtime.InteropServices;
using System.Text;
using ReportManager;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //   ReaderWriterTest();

            ReportWriter.Write("Path to bot", "Currency", 1000, 1, "EmptyFile.xml", "_Symbol", 1, DateTime.Now.DTToUnixDT(), DateTime.Now.DTToUnixDT());

            List<DateBorders> History = new List<DateBorders>
            {
                new DateBorders(new DateTime(2016,06,10),new DateTime(2017,06,14)),
                new DateBorders(new DateTime(2016,03,10),new DateTime(2017,03,09)),
                new DateBorders(new DateTime(2015,12,10),new DateTime(2016,12,09)),
                new DateBorders(new DateTime(2016,09,12),new DateTime(2017,09,08)),
            };
            List<DateBorders> Forward = new List<DateBorders>
            {
                new DateBorders(new DateTime(2017,03,10),new DateTime(2017,06,14)),
                new DateBorders(new DateTime(2017,06,15),new DateTime(2017,09,08)),
                new DateBorders(new DateTime(2016,12,12),new DateTime(2017,03,09)),
            };


            var historyToForward = DateBorders.CompareHistoryToForward(History, Forward);

            ReaderWriterTest2("Forward.xml");
        }

        static void ReaderWriterTest2(string path)
        {
            List<OptimisationResult> or = new List<OptimisationResult>();

            string pathToBot;
            string currency;
            double balance;
            int laverage;

            using (ReportReader reader = new ReportReader(path))
            {
                pathToBot = reader.RelativePathToBot;
                currency = reader.Currency;
                balance = reader.Balance;
                laverage = reader.Laverage;

                while (reader.Read())
                {
                    or.Add(reader.ReportItem.Value);
                }
            }

            or.ReportWriter(pathToBot, currency, balance, laverage, $"new new {path}");

            using (ReportReader reader = new ReportReader($"new {path}"))
            {
                pathToBot = reader.RelativePathToBot;
                currency = reader.Currency;
                balance = reader.Balance;
                laverage = reader.Laverage;

                while (reader.Read())
                {
                    or.Add(reader.ReportItem.Value);
                }
            }

        }

        static void ReaderWriterTest()
        {
            ulong Time = 1568419197;
            DateTime DT = Time.UnixDTToDT();
            Console.WriteLine(DT.ToString("yyyy.MM.dd HH:mm:ss"));
            ulong _DT = DT.DTToUnixDT();
            Console.WriteLine(_DT == Time);


            string path = "My testXml 2.xml";
            WriteTestReport(path);

            // List<OptimisationResult>
            // ReportManager.
            List<OptimisationResult> or = new List<OptimisationResult>();

            using (ReportReader reader = new ReportReader("Percentage2_Report.xml"))
            {
                while (reader.Read())
                {
                    or.Add(reader.ReportItem.Value);
                }
            }

            Dictionary<SortBy, KeyValuePair<CompareType, double>> data = new Dictionary<SortBy, KeyValuePair<CompareType, double>>
            {
                {SortBy.ProfitFactor, new KeyValuePair<CompareType, double>(CompareType.EqualTo | CompareType.GraterThan, 1) },
                {SortBy.RecoveryFactor, new KeyValuePair<CompareType, double>(CompareType.GraterThan, 1) }
            };

            or = or.FiltreOptimisations(data).ToList();

            List<SortBy> sortingFlags = new List<SortBy>
            {
                SortBy.RecoveryFactor
            };
            or = or.SortOptimisations(OrderBy.Descending, sortingFlags).ToList();
        }

        static void WriteTestReport(string path)
        {

            File.Delete(path);

            for (int i = 0; i < 5; i++)
            {
                ReportWriter.AppendBotParam("Param_1", "1");
                ReportWriter.AppendBotParam("Param_2", "2.0");
                ReportWriter.AppendBotParam("Param_3", "3");
                ReportWriter.AppendMainCoef(2.5, 4, 5, 20, 53, 55, 100000, -12000, 0.5);
                ReportWriter.AppendVaR(1, 2, 3, 4, 5);
                ReportWriter.AppendMaxPLDD(1, 2, 3, 4, 5, 6);
                ReportWriter.AppendDay(1, 1, 2, 3, 4);
                ReportWriter.AppendDay(2, 2, 3, 4, 5);
                ReportWriter.AppendDay(3, 3, 4, 5, 6);
                ReportWriter.AppendDay(4, 4, 5, 6, 7);
                ReportWriter.AppendDay(5, 5, 6, 7, 8);

                ReportWriter.Write("Relative path to my bot", "Rur",
                                    100000, 1, path, "Si Splice", 2, 0, 1);

            }
        }
    }
}
