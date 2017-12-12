using System;
using System.Windows.Data;
using System.Windows.Input;

namespace JustClimbTrial.Mvvm.Infrastructure
{
    // Multi-filtered WPF DataGrid with MVVM
    // https://www.codeproject.com/Articles/442498/Multi-filtered-WPF-DataGrid-with-MVVM
    public class GeneralFilter : ViewModelBase
    {
        /* properties */

        private CollectionViewSource _cvs;
        private FilterEventHandler _filterEventHandler;
        public ICommand RemoveFilterCommand { get; private set; }
        public bool CanRemoveFilter
        {
            get { return GetValue(() => CanRemoveFilter); }
            set { SetValue(() => CanRemoveFilter, value); }
        }

        /* end of properties */


        /* constructor */

        public GeneralFilter(CollectionViewSource cvs, Action<object, FilterEventArgs> filterEventHandler)
        {
            _cvs = cvs;
            _filterEventHandler = new FilterEventHandler(filterEventHandler);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
        }

        /* end of constructor */


        /* methods */

        // the not used parameter is kept here to satisfy 
        // the interface of RelayCommand
        public void RemoveFilter(object parameter = null)
        {
            if (CanRemoveFilter)
            {
                _cvs.Filter -= _filterEventHandler;
                CanRemoveFilter = false;
            }
        }

        /* Notes on Adding Filters:
         *   Each filter is added by subscribing a filter method to the Filter event
         *   of the CVS.  Filters are applied in the order in which they were added. 
         *   To prevent adding filters mulitple times ( because we are using drop down lists
         *   in the view), the CanRemove***Filter flags are used to ensure each filter
         *   is added only once.  If a filter has been added, its corresponding CanRemove***Filter
         *   is set to true.       
         *   
         *   If a filter has been applied already (for example someone selects "Canada" to filter by country
         *   and then they change their selection to another value (say "Mexico") we need to undo the previous
         *   country filter then apply the new one.  This does not completey Reset the filter, just
         *   allows it to be changed to another filter value. This applies to the other filters as well
         */

        public void AddFilter()
        {
            if (CanRemoveFilter)
            {
                _cvs.Filter -= _filterEventHandler;
                _cvs.Filter += _filterEventHandler;
            }
            else
            {
                _cvs.Filter += _filterEventHandler;
                CanRemoveFilter = true;
            }
        }

        /* end of methods */
    }
}
