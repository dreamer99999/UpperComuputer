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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
namespace UpperComputer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public void GetPortNames()
        {
            string[] PortNames = System.IO.Ports.SerialPort.GetPortNames();
            ComboBoxPorts.Items.Clear();

            foreach (string PortName in PortNames)
            {
                ComboBoxPorts.Items.Add(PortName);
            }
        }
        private void ComboBoxPorts_DropDownOpened_1(object sender, EventArgs e)
        {
            GetPortNames();
        }
        private void ComboBoxBauds_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            List<int> BaudsRates = new List<int>();
            BaudsRates.Add(1200);
            BaudsRates.Add(2400);
            BaudsRates.Add(4800);
            BaudsRates.Add(9600);
            BaudsRates.Add(19200);
            BaudsRates.Add(38400);
            BaudsRates.Add(57600);
            BaudsRates.Add(74800);


            

            BaudsRates.Add(115200);
            BaudsRates.Sort((x, y) =>
            {
                return y-x;
            });
                ComboBoxBauds.ItemsSource = BaudsRates;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string selectedItem = ComboBoxPorts.SelectedItem as string;
            MessageBox.Show(selectedItem);
        }
    }

}
