using System;
using System.Windows.Input;

namespace JustClimbTrial.Mvvm.Infrastructure
{
    public abstract class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);

        private bool _IsCanExecute;
        protected bool IsCanExecute
        {
            get
            {
                return _IsCanExecute;
            }

            set
            {
                if (_IsCanExecute != value)
                {
                    _IsCanExecute = value;
                    NotifyIsCanExecuteChanged();
                }
            }
        }

        protected void NotifyIsCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }        
    }
}
