using System;
using System.Windows.Input;

namespace JustClimbTrial.Mvvm.Infrastructure
{
    // https://wpftution.blogspot.hk/2012/05/mvvm-sample-using-datagrid-control-in.html
    public class DelegateCommand : ICommand
    {
        private Predicate<object> _canExecute;
        private Action<object> _execute;

        public DelegateCommand(Predicate<object> canExecute, 
            Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
