using System;
using System.Windows.Input;

namespace Metatrader_Auto_Optimiser.View_Model
{
    /// <summary>
    /// Реализация интерфейса ICommand - исспользуемая для
    /// связи комманд с методами из ViewModel
    /// </summary>
    class RelayCommand : ICommand
    {
        #region Fields 
        /// <summary>
        /// Делегат непосредственно выполняющий действие
        /// </summary>
        readonly Action<object> _execute;
        /// <summary>
        /// Делегат осуществляющий проверку на возможность выполнения действия
        /// </summary>
        readonly Predicate<object> _canExecute;
        #endregion // Fields

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="execute">Метод передаваемый по делегату - который является коллбеком</param>
        public RelayCommand(Action<object> execute) : this(execute, null) { }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="execute">
        /// Метод передаваемый по делегату - который является коллбеком
        /// </param>
        /// <param name="canExecute">
        /// Метод передаваемый по делегату - проверяющий возможность выполнения действия
        /// </param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute; _canExecute = canExecute;
        }

        /// <summary>
        /// Проверка на возможность выполнения действия
        /// </summary>
        /// <param name="parameter">передаваемый из View параметр</param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }
        /// <summary>
        /// Событие - вызываемое всякий раз когда меняется возможность исполнения коллбека.
        /// При срабатывании данного события, форма вновь вызывает метод "CanExecute"
        /// Событие запускается из ViewModel по мере необходимости
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        /// <summary>
        /// Метод вызывающий делегат который в свою очередь выполняет действие
        /// </summary>
        /// <param name="parameter">передаваемый из View параметр</param>
        public void Execute(object parameter) { _execute(parameter); }
    }
}
