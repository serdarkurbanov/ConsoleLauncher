using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConsoleLauncher.UI
{
    /// <summary>
    /// Interaction logic for ProcessEditor.xaml
    /// </summary>
    public partial class ProcessEditor : MetroWindow
    {
        public ProcessEditor()
        {
            InitializeComponent();
        }
        public Processes.Process Process { get; set; }

        // cancel 
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        // ok
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        // checkbox got checked => process name is set the same as process cmd
        private void PART_IsDefaultNameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Process.Name = Process.Command;
        }
    }
}
