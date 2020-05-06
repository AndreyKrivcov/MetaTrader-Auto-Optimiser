using Metatrader_Auto_Optimiser.Model.DirectoryManagers;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers
{
    class CommonMethods
    {
        public static Config GetConfig(OptimiserInputData optimiserInputData,
                         string setFileName,
                         DateBorders dateBorders,
                         DirectoryInfo terminalChangeableDirectory,
                         string pathToNewIniFile)
        {
            Config config = new Config(terminalChangeableDirectory.GetDirectory("config")
                                                                  .GetFiles("common.ini")
                                                                  .First().FullName)
                                                                  .DublicateFile(pathToNewIniFile);


            config.Tester.Currency = optimiserInputData.Currency;
            config.Tester.Deposit = optimiserInputData.Balance;
            config.Tester.ExecutionMode = optimiserInputData.ExecutionDelay;
            config.Tester.Expert = optimiserInputData.RelativePathToBot;
            config.Tester.ExpertParameters = setFileName;
            config.DeleteKey(ENUM_SectionType.Tester, "ForwardDate");
            config.Tester.ForwardMode = ENUM_ForvardMode.Disabled;
            config.Tester.FromDate = dateBorders.From;
            config.Tester.Leverage = $"1:{optimiserInputData.Laverage}";
            config.Tester.Model = optimiserInputData.Model;
            config.Tester.Optimization = optimiserInputData.OptimisationMode;
            config.Tester.OptimizationCriterion = ENUM_OptimisationCriteria.Balance__Profit_factor;
            config.Tester.Period = optimiserInputData.TF;
            config.DeleteKey(ENUM_SectionType.Tester, "Report");
            if (optimiserInputData.BotParams.Any(x => x.Variable == Fixed_Input_Settings.Params[InputParamName.CloseTerminalFromBot]) &&
                optimiserInputData.OptimisationMode != ENUM_OptimisationMode.Disabled)
            {
                config.Tester.ShutdownTerminal = false;
            }
            else
                config.Tester.ShutdownTerminal = true;
            config.Tester.Symbol = optimiserInputData.Symb;
            config.Tester.ToDate = dateBorders.Till;

            return config;
        }

        public static bool RunTester(Config config, TerminalManager terminalManager, bool isWait = true)
        {
            terminalManager.Config = config;
            bool ans = terminalManager.Run();
            if (ans && isWait)
                terminalManager.WaitForStop();

            return ans;
        }

        public static bool ReadFile(List<OptimisationResult> data,
                                    string pathToReportFile,
                                    string expectedCurecncy = null,
                                    double? expectedBalance = null,
                                    int? expectedLaverage = null,
                                    string expectedPathToBot = null,
                                    bool deleteAfterReading = true)
        {
            if (!File.Exists(pathToReportFile))
                return false;

            bool ans = false;
            using (ReportReader reader = new ReportReader(pathToReportFile))
            {
                if (!string.IsNullOrEmpty(expectedCurecncy) &&
                   !string.IsNullOrWhiteSpace(expectedCurecncy) &&
                   reader.Currency != expectedCurecncy)
                {
                    throw new Exception("Currency is different");
                }
                if (!string.IsNullOrEmpty(expectedPathToBot) &&
                   !string.IsNullOrWhiteSpace(expectedPathToBot) &&
                   reader.RelativePathToBot != expectedPathToBot)
                {
                    throw new Exception("Path to bot is different");
                }
                if (expectedBalance.HasValue &&
                   reader.Balance != expectedBalance.Value)
                {
                    throw new Exception("Balance is different");
                }
                if (expectedLaverage.HasValue &&
                    reader.Laverage != expectedLaverage)
                {
                    throw new Exception("Laverage is different");
                }

                while (reader.Read())
                {
                    data.Add(reader.ReportItem.Value);
                    if (!ans) ans = true;
                }
            }

            if (deleteAfterReading)
                File.Delete(pathToReportFile);

            return ans;
        }

        public static string SaveExpertSettings(string expertName,
                                              List<ParamsItem> settings,
                                              DirectoryInfo terminalChangeableDirectory)
        {
            string setFile = new FileInfo(Path.Combine(terminalChangeableDirectory
                             .GetDirectory("MQL5")
                             .GetDirectory("Profiles")
                             .GetDirectory("Tester")
                             .FullName,
                             $"{expertName}.set"))
                             .FullName;
            SetFileManager setFileManager = new SetFileManager(setFile, true)
            {
                Params = settings
            };
            setFileManager.SaveParams();

            return Path.GetFileName(setFile);
        }
    }

    enum InputParamName
    {
        CloseTerminalFromBot
    }
    class Fixed_Input_Settings
    {
        public static readonly Dictionary<InputParamName, string> Params = new Dictionary<InputParamName, string>
        {
            {InputParamName.CloseTerminalFromBot,  "close_terminal_after_finishing_optimisation"}
        };
    }

    abstract class ManagerBase : IOptimiser
    {
        public ManagerBase(string name)
        {
            Name = name;
        }
        #region Terminal manager
        /// <summary>
        /// Хранитель терминалла
        /// </summary>
        protected TerminalManager _terminal = null;
        /// <summary>
        /// Геттер / Сеттер для добавления терминалла в оптимизатор
        /// </summary>
        public virtual TerminalManager TerminalManager
        {
            get => _terminal;
            set
            {
                // Если добавляентя null - не чего не делаем
                if (value == null)
                    return;
                // Если сейчас оптимизация в процессе - то останавливаем ее перед добавлением нового терминала
                if (IsOptimisationInProcess)
                    Stop();

                // Если заданный ранее терминалл открыт - закрываем его перед заменой параметра
                if (_terminal != null && _terminal.IsActive)
                {
                    _terminal.Close();
                    _terminal.WaitForStop();
                }
                // ЗАменяем параметр
                _terminal = value;
            }
        }
        #endregion

        public virtual bool IsOptimisationInProcess { get; protected set; } = false;

        public virtual string Name { get; }

        #region Report getters
        /// <summary>
        /// Отчет оптимизаций для всех указанных исторических временных промежутков
        /// </summary>
        public virtual List<OptimisationResult> AllOptimisationResults { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Форвард тесты
        /// </summary>
        public virtual List<OptimisationResult> ForwardOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Исторические тесты
        /// </summary>
        public virtual List<OptimisationResult> HistoryOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Валюта
        /// </summary>
        public virtual string Currency { get; protected set; } = null;
        /// <summary>
        /// Баланс
        /// </summary>
        public virtual double Balance { get; protected set; }
        /// <summary>
        /// Кредитное плечо
        /// </summary>
        public virtual int Laverage { get; protected set; }
        /// <summary>
        /// Путь к роботу относительно директории с экспертами
        /// </summary>
        public virtual string PathToBot { get; protected set; } = null;
        /// <summary>
        /// Путь к рабочей директории с изменяемыми файтами
        /// </summary>
        public virtual string OptimiserWorkingDirectory { get; protected set; } = null;
        #endregion

        public event Action<IOptimiser> OptimisationProcessFinished;
        protected void OnOptimisationProcessFinished(IOptimiser optimiser)
        {
            OptimisationProcessFinished?.Invoke(optimiser);
        }
        public event Action<string, double> ProcessStatus;
        protected void OnProcessStatus(string status, double value)
        {
            ProcessStatus?.Invoke(status, value);
        }

        public virtual void ClearOptimiser()
        {
            if (TerminalManager.IsActive || IsOptimisationInProcess)
            {
                throw new Exception("Can`t clear optimiser becouse of terminal is active or optimisation is in process");
            }

            AllOptimisationResults.Clear();
            ForwardOptimisations.Clear();
            HistoryOptimisations.Clear();

            Currency = null;
            PathToBot = null;
            OptimiserWorkingDirectory = null;
            Balance = 0;
            Laverage = 0;
        }

        public virtual void CloseSettingsWindow() { }

        public virtual void LoadSettingsWindow() { }

        public abstract void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrifix);

        public virtual void Stop()
        {
            ProcessStatus("Stoped", 100);

            if (TerminalManager.IsActive)
            {
                TerminalManager.Close();
                TerminalManager.WaitForStop();
            }

            IsOptimisationInProcess = false;
            ClearOptimiser();
            OptimisationProcessFinished(this);
        }
    }
}
