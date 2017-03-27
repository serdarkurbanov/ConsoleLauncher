using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleLauncher.UIHelpers
{
    // converter to display short path to program if it's too long
    class ShortPathConverter : IValueConverter
    {
        // trunkate path if too long
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string cmd = value as string;

            string result = null;

            if (cmd == null)
                result = null;
            else
            {
                // shorten filename
                string filename = System.IO.Path.GetFileName(cmd);
                if (filename.Length >= 15)
                    filename = filename.Substring(0, 4) + "...." + filename.Substring(filename.Length - 4, 4);

                // shorten path
                string dirname = System.IO.Path.GetDirectoryName(cmd);
                if (dirname.Length >= 15)
                    dirname = "....";

                if (string.IsNullOrEmpty(dirname))
                    result = filename;
                else
                    result = System.IO.Path.Combine(dirname, filename);
            }

            return result;
        }

        // we never convert back
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
