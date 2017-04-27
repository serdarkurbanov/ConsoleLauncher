using ConsoleLauncher.Processes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleLauncher.UIHelpers
{
    class MinMinus10PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<ResourceUsageRecord> records = value as ObservableCollection<ResourceUsageRecord>;

            if (records == null)
                return null;

            return records.Count == 0 ? 0 : records.Min(x => x.Data) * 0.9;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
