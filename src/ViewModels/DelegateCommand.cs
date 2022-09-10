using System;
using System.Windows.Input;

namespace ActorPipeline.ViewModels
{
    internal class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Func<T, bool> canExecute;

        public DelegateCommand(Action<T> execute) : this(execute, CanAlwaysExecute)
        {
        }

        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) 
        {
            return canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool CanAlwaysExecute(T _) => true;
    }

    internal class DelegateCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public DelegateCommand(Action execute) : this(execute, CanAlwaysExecute)
        {
        }

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object _)
        {
            return canExecute();
        }

        public void Execute(object _)
        {
            execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool CanAlwaysExecute() => true;
    }
}
