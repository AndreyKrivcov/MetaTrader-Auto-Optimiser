using System;
using System.Windows;
using System.Windows.Input;

namespace Metatrader_Auto_Optimiser.View
{
    class WindowExtention
    {
        #region Command
        public static readonly DependencyProperty ClosedCommandProperty =
            DependencyProperty.RegisterAttached("ClosedCommand",
                typeof(ICommand), typeof(WindowExtention),
                new PropertyMetadata(ClosedCommandPropertyCallback));

        public static void SetClosedCommand(UIElement obj, ICommand value)
        {
            obj.SetValue(ClosedCommandProperty, value);
        }
        public static ICommand GetClosedCommand(UIElement obj)
        {
            return (ICommand)obj.GetValue(ClosedCommandProperty);
        }

        public static void ClosedCommandPropertyCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is Window window)
            {
                if (args.OldValue != null)
                    window.Closed -= Window_Closed;
                if (args.NewValue != null)
                    window.Closed += Window_Closed;
            }
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            if (sender is UIElement element)
            {
                object param = GetClosedCommandParameter(element);
                ICommand cmd = GetClosedCommand(element);
                if (cmd.CanExecute(param))
                    cmd.Execute(param);
            }
        }
        #endregion

        #region Command parameter
        public static readonly DependencyProperty ClosedCommandParameterProperty =
            DependencyProperty.RegisterAttached("ClosedCommandParameter",
                typeof(object), typeof(WindowExtention));
        public static void SetClosedCommandParameter(UIElement obj, object value)
        {
            obj.SetValue(ClosedCommandParameterProperty, value);
        }
        public static object GetClosedCommandParameter(UIElement obj)
        {
            return obj.GetValue(ClosedCommandParameterProperty);
        }
        #endregion

    }
}
