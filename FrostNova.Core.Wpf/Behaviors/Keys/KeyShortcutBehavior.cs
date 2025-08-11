using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class KeyShortcutBehavior : TriggerAction<UIElement>
    {
        public Key ShortcutKey { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(KeyShortcutBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if (parameter is KeyEventArgs e &&
                e.Key == ShortcutKey &&
                Keyboard.Modifiers == Modifiers)
            {
                if (Command?.CanExecute(null) == true)
                {
                    Command.Execute(null);
                    e.Handled = true; // ★ ここで後続入力を防ぐ
                }
            }
        }
    }
}
