using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleLauncher.UIHelpers
{
    class AggregateConverter : IValueConverter
    {
        // aggregate strings into one
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<string> strs = value as IEnumerable<string>;

            if (strs == null)
                return null;

            return strs.Aggregate("", (x, y) => string.Concat(x, " ", y));
        }

        // we don't convert back
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
