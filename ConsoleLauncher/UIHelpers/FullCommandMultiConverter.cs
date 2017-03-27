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
    class FullCommandTwoValueConverter : IMultiValueConverter
    {
        // convert to full cmd
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return null;

            string cmd = values[0] as string;
            ObservableCollection<string> args = values[1] as ObservableCollection<string>;

            return cmd + args.Aggregate("", (x, y) => string.Concat(x, " ", y));
        }
        
        // convert from full cmd to args and cmd
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string fullCmd = value as string;

            if (string.IsNullOrEmpty(fullCmd))
                return new object[] { "", new ObservableCollection<string>() };

            var parts = fullCmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string cmd = "";

            ObservableCollection<string> args = new ObservableCollection<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i];
                if (i == 0)
                    cmd = s;
                else
                    args.Add(s);
            }

            return new object[] { cmd, args };
        }
    }

    class FullCommandThreeValueConverter : IMultiValueConverter
    {
        // convert to full cmd
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3)
                return null;

            string cmd = values[1] as string;
            ObservableCollection<string> args = values[2] as ObservableCollection<string>;

            return cmd + args.Aggregate("", (x, y) => string.Concat(x, " ", y));
        }

        // convert from full cmd to args and cmd
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string fullCmd = value as string;

            if (string.IsNullOrEmpty(fullCmd))
                return new object[] { "", new ObservableCollection<string>() };

            var parts = fullCmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string cmd = "";

            ObservableCollection<string> args = new ObservableCollection<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i];
                if (i == 0)
                    cmd = s;
                else
                    args.Add(s);
            }

            return new object[] { cmd, cmd, args };
        }
    }
}
