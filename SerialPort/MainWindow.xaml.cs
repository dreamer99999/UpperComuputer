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
using System.Runtime.Serialization;
using ScottPlot;
using System.Data;

namespace UpperComputer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.IO.Ports.SerialPort serialPort = null;
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            ComboBoxParity.ItemsSource = Enum.GetValues(typeof(Parity));
            ComboBoxParity.SelectedIndex = 0;
            ComboBoxStopBits.ItemsSource = Enum.GetValues(typeof(StopBits));
            ComboBoxStopBits.SelectedIndex = 1;
            textBox_out.Text = "";

            double[] dataX = new double[100];
            double[] dataY = new double[100];
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                dataX[i] = random.NextDouble();
                dataY[i] = random.NextDouble();
            }
            WpfPlot1.Plot.AddScatter(dataX, dataY);
            WpfPlot1.Refresh();
        }
        /// <summary>
        /// 获取所有的串口名称
        /// </summary>
        public void GetPortNames()
        {
            string[] PortNames = System.IO.Ports.SerialPort.GetPortNames();
            ComboBoxPorts.Items.Clear();

            foreach (string PortName in PortNames)
            {
                ComboBoxPorts.Items.Add(PortName);
            }
        }
        private void ComboBoxPorts_DropDownOpened(object sender, EventArgs e)
        {
            GetPortNames();
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            send_count.Content = 0;
            receive_count.Content = 0;
        }

        private void send_button_Click(object sender, RoutedEventArgs e)
        {

            int sendCount = Convert.ToInt32(send_count.Content);
            send_count.Content = (sendCount + 1).ToString();
            SendData(textBox_in.Text);
        }

        private void SendData(string data)
        {           
            if (serialPort != null && serialPort.IsOpen)
            {
                byte[] byteData = null;
                switch (comboBox_encoding.Text)
                {
                    case "UTF-8":
                        byteData = Encoding.UTF8.GetBytes(data);
                        break;
                    case "GBK":
                        byteData= Encoding.GetEncoding("GBK").GetBytes(data);
                        break;
                    case "ASCII":
                        byteData = Encoding.ASCII.GetBytes(data);
                        break;
                }
                // 将字符串转换为字节数组
                // 发送数据到串口
                if(byteData != null)
                {
                    serialPort.Write(byteData, 0, byteData.Length);

                }
            }
        }

        private void serial_open_button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string portName = null;
            if (ComboBoxPorts.SelectedItem == null)
            {
                MessageBox.Show("未选择串口号！");
                return;
            }
            else
            {
                portName = ComboBoxPorts.SelectedItem as string;
            }
            int baudRate = Convert.ToInt32((ComboBoxBauds.SelectedItem as ComboBoxItem).Content.ToString());
            int dataBits = Convert.ToInt32((ComboBoxDataBits.SelectedItem as ComboBoxItem).Content.ToString());
            Parity parity = (Parity)ComboBoxParity.SelectedIndex;
            StopBits stopBits = (StopBits)ComboBoxStopBits.SelectedIndex;

            try
            {
                if (serialPort == null)
                {
                    serialPort = new System.IO.Ports.SerialPort(portName, baudRate, parity, dataBits, stopBits);

                    serialPort.ReadTimeout = 5000;
                    serialPort.WriteTimeout = 5000;
                    serialPort.ReceivedBytesThreshold = 1;
                    serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(port_DataReceived);
                    serialPort.Open();
                    button.Content = "关闭串口";
                }
                else
                {
                    serialPort.Close();
                    serialPort = null;
                    button.Content = "打开串口";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void port_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int nRead = serialPort.BytesToRead;
            if (nRead > 0)
            {
                byte[] data = new byte[nRead];
                serialPort.Read(data, 0, nRead);
                // 在UI线程上更新计数器和文本框内容
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (radio_hex.IsChecked == true)
                    {
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            textBox_out.Text += data[i] + "\n";
                        }
                    }
                    else if (radio_string.IsChecked == true)
                    {
                        switch (comboBox_encoding.Text)
                        {
                            case "UTF-8":
                                textBox_out.AppendText(Encoding.UTF8.GetString(data));
                                break;
                            case "GBK":
                                textBox_out.AppendText(Encoding.GetEncoding("GBK").GetString(data));
                                break;
                            case "ASCII":
                                textBox_out.AppendText(Encoding.ASCII.GetString(data));
                                break;
                        }
                    }
                    receive_count.Content = Convert.ToInt32(receive_count.Content) + 1;
                }));
            }
        }
        private void combobox_newline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string newline = combobox_newline.Text;
            switch (newline)
            {
                case "\\r\\n(CRLF)":
                    textBox_in.AcceptsReturn = true;
                    textBox_in.TextWrapping = TextWrapping.Wrap;
                    break;
                case "\\r(CR)":
                    textBox_in.AcceptsReturn = true;
                    textBox_in.TextWrapping = TextWrapping.NoWrap;
                    break;
                case "\\n(LF)":
                    textBox_in.AcceptsReturn = false;
                    textBox_in.TextWrapping = TextWrapping.Wrap;
                    break;
            }
        }

        private void Label_MouseEnter(object sender, MouseEventArgs e)
        {
            Label label = sender as Label;
            label.Foreground = Brushes.CadetBlue;
        }
        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            Label label = sender as Label;
            label.Foreground = new SolidColorBrush(Color.FromRgb(43, 129, 213));
        }
    }
}