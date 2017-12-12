using JustClimbTrial.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.Helpers
{
    public class DateTimeHelper
    {
        public static IEnumerable<string> GetYearsForComboBox()
        {
            int startYear = 2017;
            int currYear = DateTime.Now.Year;
            IEnumerable<int> years = Enumerable.Range(startYear, currYear - startYear + 1);
            return years.Select(x => x.ToString());
        }

        public static IEnumerable<string> GetMonthsForComboBox(string monthToStringFormat)
        {
            IEnumerable<int> months = Enumerable.Range(1, 12);
            return months.Select(x => x.ToString(monthToStringFormat));
        }

        public static IEnumerable<string> GetDaysForComboBox(string dayToStringFormat)
        {
            IEnumerable<int> days = Enumerable.Range(1, 31);
            return days.Select(x => x.ToString(dayToStringFormat));
        }

        public static IEnumerable<FilterHourViewModel> GetHoursForComboBox(string hourToStringFormat)
        {
            IEnumerable<int> hours = Enumerable.Range(0, 24);
            return hours.Select(x => new FilterHourViewModel
            {
                Hour = x,
                HourString = x.ToString(hourToStringFormat) + ":00-" + (x + 1).ToString(hourToStringFormat) + ":00"
            });
        }
    }
}
