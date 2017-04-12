using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using OxyPlot;
using ConsoleLauncher.Processes;
using System.Collections.ObjectModel;
using OxyPlot.Axes;

namespace ConsoleLauncher.UIHelpers
{
    class RecordToDataPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<ResourceUsageRecord> records = value as ObservableCollection<ResourceUsageRecord>;

            if (records == null)
                return null;

            return records.Select(x => new DataPoint(DateTimeAxis.ToDouble(x.Time), x.Data)).ToList();
        }

        // we dont' convert back
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
