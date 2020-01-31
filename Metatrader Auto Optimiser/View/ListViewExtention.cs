using System.Windows;

using ICommand = System.Windows.Input.ICommand;
using ListView = System.Windows.Controls.ListView;

namespace Metatrader_Auto_Optimiser.View
{
    /// <summary>
    /// Класс расширений для ListView который переводит события в команды (ICommand)
    /// класс помечен ключевым словом partial - т.е. его реализация разбита на несколько файлов.
    /// 
    /// Конкретно в данном классе - реализован перевод события ListView.DoubleClickEvent 
    /// в комманду типа ICommand
    /// </summary>
    partial class ListViewExtention
    {
        #region Command
        /// <summary>
        /// Зависимое свойство - содержащее в себе ссылку на коллбек команды
        /// Свойство задается через View в XAML разметке проекта
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand",
                typeof(ICommand), typeof(ListViewExtention),
                new PropertyMetadata(DoubleClickCommandPropertyCallback));

        /// <summary>
        /// Setter для DoubleClickCommandProperty
        /// </summary>
        /// <param name="obj">Элемент управления</param>
        /// <param name="value">Значение с которым осуществляется связка</param>
        public static void SetDoubleClickCommand(UIElement obj, ICommand value)
        {
            obj.SetValue(DoubleClickCommandProperty, value);
        }
        /// <summary>
        /// Geter для DoubleClickCommandProperty
        /// </summary>
        /// <param name="obj">Элемент управления</param>
        /// <returns>ссылку на сохраненную команду типа ICommand</returns>
        public static ICommand GetDoubleClickCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(DoubleClickCommandProperty);
        }
        /// <summary>
        /// Коллбек вызываемый после задания свойства DoubleClickCommandProperty
        /// </summary>
        /// <param name="obj">Элемент управления для которого задается свойство</param>
        /// <param name="args">события предшествующие вызову коллбека</param>
        private static void DoubleClickCommandPropertyCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is ListView lw)
            {
                if (args.OldValue != null)
                    lw.MouseDoubleClick -= Lw_MouseDoubleClick;

                if (args.NewValue != null)
                    lw.MouseDoubleClick += Lw_MouseDoubleClick;
            }
        }
        /// <summary>
        /// Коллбек события которое переводится в тип ICommand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Lw_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                object param = GetDoubleClickCommandParameter(element);
                ICommand cmd = GetDoubleClickCommand(element);
                if (cmd.CanExecute(param))
                    cmd.Execute(param);
            }
        }
        #endregion

        #region CommandParameter
        /// <summary>
        /// Зависимое свойство - содержащее в себе ссылку на параметры передаваемые в коллбек типа ICommand
        /// Свойство задается через View в XAML разметке проекта
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommandParameter",
                typeof(object), typeof(ListViewExtention));
        /// <summary>
        /// Setter для DoubleClickCommandParameterProperty
        /// </summary>
        /// <param name="obj">Элемент управления</param>
        /// <param name="value">Значение с которым осуществляется связка</param>
        public static void SetDoubleClickCommandParameter(UIElement obj, object value)
        {
            obj.SetValue(DoubleClickCommandParameterProperty, value);
        }
        /// <summary>
        /// Geter для DoubleClickCommandParameterProperty
        /// </summary>
        /// <param name="obj">Элемент управления</param>
        /// <returns>переданный параметр</returns>
        public static object GetDoubleClickCommandParameter(UIElement obj)
        {
            return obj.GetValue(DoubleClickCommandParameterProperty);
        }
        #endregion
    }
}
