using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleLauncher.UIHelpers
{
    class FullCommandConverter : IValueConverter
    {
        // gets full comand with arguments
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Processes.Process p = value as Processes.Process;
            if (p == null)
                return null;

            return p.Command + p.Arguments.Aggregate("", (x, y) => string.Concat(x, " ", y));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
            //string fullCommand = value as string;

            //if (string.IsNullOrEmpty(fullCommand))
            //    return null;

            //Processes.Process p = new Processes.Process()
        }
    }
}
