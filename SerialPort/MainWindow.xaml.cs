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
using System.IO;
using System.Web.UI.WebControls;
using ScottPlot.Drawing.Colormaps;
using System.Collections;
using System.Threading;

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
                        byteData = Encoding.GetEncoding("GBK").GetBytes(data);
                        break;
                    case "ASCII":
                        byteData = Encoding.ASCII.GetBytes(data);
                        break;
                }
                // 将字符串转换为字节数组
                // 发送数据到串口
                if (byteData != null && byteData.Length == Encoding.Default.GetByteCount(data))
                {
                    serialPort.Write(byteData, 0, byteData.Length);

                }
            }
        }

        private void serial_open_button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
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
                    try
                    {
                        serialPort.Open();
                        button.Content = "关闭串口";
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
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
                        for (int i = 0; i < data.Length; i++)
                        {
                            textBox_out.AppendText(string.Format("{0:X2} ", data[i]));
                        }
                    }
                    else if (radio_string.IsChecked == true)
                    {
                        switch (comboBox_encoding.Text)
                        {
                            case "UTF-8":
                                textBox_out.AppendText(Encoding.UTF8.GetString(data));
                                break;
                            case "GB2312":
                                textBox_out.AppendText(Encoding.GetEncoding("GB2312").GetString(data));
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
            System.Windows.Controls.Label label = sender as System.Windows.Controls.Label;
            label.Foreground = Brushes.CadetBlue;
        }
        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            System.Windows.Controls.Label label = sender as System.Windows.Controls.Label;
            label.Foreground = new SolidColorBrush(Color.FromRgb(43, 129, 213));
        }

        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            textBox_out.Text = string.Empty;
        }
        const byte FRAME_HEADER = 0xAA;
        const byte FRAME_FOOTER = 0x55;
        // 计算校验和，采用异或校验
        public byte CalcChecksum(byte[] data, int length)
        {
            byte checksum = 0;
            for (byte i = 0; i < length; i++)
            {
                checksum ^= data[i];
            }
            return checksum;
        }
        // 发送自定义帧
        public void SendFrame(byte func_code, byte[] data, int data_len)
        {
            // 计算帧长和校验和
            byte frame_len = (byte)(1 + 1 + 1 + data_len + 1 + 1); // 帧头 + 帧长 + 功能码 + 数据 + 校验和 + 帧尾
            byte checksum = CalcChecksum(data, data_len);
            // 定义帧缓冲区
            byte[] frame = new byte[frame_len];
            // 组装帧
            byte index = 0;
            frame[index++] = FRAME_HEADER; // 帧头
            frame[index++] = frame_len;    // 帧长
            frame[index++] = func_code;    // 功能码
            for (int i = 0; i < data_len; i++)
            { // 数据
                frame[index++] = data[i];
            }
            frame[index++] = checksum;     // 校验和
            frame[index++] = FRAME_FOOTER; // 帧尾
            foreach (byte b in frame)
            {
                textBox_Hex.Text += b.ToString("X2"); // 将每个字节转换为两位大写十六进制表示的字符串
                textBox_Hex.Text += " ";
            }
            textBox_Hex.Text += "\n";
            // 发送帧到串口
            int length = frame.Length;
            int sendBytesNum = length / 4 * 4; // 每四个字节发送一次，计算需要发送的字节数
            byte[] tempBytes = new byte[4];
            int j;
            for (j = 0; j < sendBytesNum; j += 4)
            {
                Array.Copy(frame, j, tempBytes, 0, 4); // 每次取出四个字节
                serialPort.Write(tempBytes, 0, 4); // 发送四个字节
                Thread.Sleep(200);
            }
            if (j < length)
            {
                Array.Copy(frame, j, tempBytes, 0, length - j); // 取出剩余不足四个字节的数据
                serialPort.Write(tempBytes, 0, length - j); // 发送不足四个字节的数据
                Thread.Sleep(200);

            }
        }

        private void button_speed_Click(object sender, RoutedEventArgs e)
        {
            byte wheelValue = byte.Parse(textBox_wheel.Text);
            float pValue = float.Parse(textBox_speed_P.Text);
            float iValue = float.Parse(textBox_speed_I.Text);
            float dValue = float.Parse(textBox_speed_D.Text);

            byte[] pBytes = BitConverter.GetBytes(pValue);
            byte[] iBytes = BitConverter.GetBytes(iValue);
            byte[] dBytes = BitConverter.GetBytes(dValue);

            byte[] resultBytes = new byte[1 + pBytes.Length + iBytes.Length + dBytes.Length];
            int index = 0;
            resultBytes[index++] = wheelValue;
            Array.Copy(pBytes, 0, resultBytes, index, pBytes.Length);
            index += pBytes.Length;
            Array.Copy(iBytes, 0, resultBytes, index, iBytes.Length);
            index += iBytes.Length;
            Array.Copy(dBytes, 0, resultBytes, index, dBytes.Length);
            if(serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x03, resultBytes, resultBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private void button_angle_Click(object sender, RoutedEventArgs e)
        {

            float pValue = float.Parse(textBox_angle_P.Text);
            float iValue = float.Parse(textBox_angle_I.Text);
            float dValue = float.Parse(textBox_angle_D.Text);

            byte[] pBytes = BitConverter.GetBytes(pValue);
            byte[] iBytes = BitConverter.GetBytes(iValue);
            byte[] dBytes = BitConverter.GetBytes(dValue);

            byte[] resultBytes = new byte[pBytes.Length + iBytes.Length + dBytes.Length];
            int index = 0;
            Array.Copy(pBytes, 0, resultBytes, index, pBytes.Length);
            index += pBytes.Length;
            Array.Copy(iBytes, 0, resultBytes, index, iBytes.Length);
            index += iBytes.Length;
            Array.Copy(dBytes, 0, resultBytes, index, dBytes.Length);
            if (serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x04, resultBytes, resultBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }

        }

        private void button_distance_Click(object sender, RoutedEventArgs e)
        {

            float pValue = float.Parse(textBox_disatance_P.Text);
            float iValue = float.Parse(textBox_disatance_I.Text);
            float dValue = float.Parse(textBox_disatance_D.Text);

            byte[] pBytes = BitConverter.GetBytes(pValue);
            byte[] iBytes = BitConverter.GetBytes(iValue);
            byte[] dBytes = BitConverter.GetBytes(dValue);

            byte[] resultBytes = new byte[pBytes.Length + iBytes.Length + dBytes.Length];
            int index = 0;
            Array.Copy(pBytes, 0, resultBytes, index, pBytes.Length);
            index += pBytes.Length;
            Array.Copy(iBytes, 0, resultBytes, index, iBytes.Length);
            index += iBytes.Length;
            Array.Copy(dBytes, 0, resultBytes, index, dBytes.Length);
            if (serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x05, resultBytes, resultBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private void button_distance1_Click(object sender, RoutedEventArgs e)
        {
            float distance_f = float.Parse(textBox_distance.Text);
            byte[] dBytes = BitConverter.GetBytes(distance_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x06, dBytes, dBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private void button_angel1_Click(object sender, RoutedEventArgs e)
        {
            float angle_f = float.Parse(textBox_angle.Text);
            byte[] aBytes = BitConverter.GetBytes(angle_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x07, aBytes, aBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private void button_speed1_Click(object sender, RoutedEventArgs e)
        {
            float speed_f = float.Parse(textBox_speed.Text);
            byte[] sBytes = BitConverter.GetBytes(speed_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                SendFrame(0x08, sBytes, sBytes.Length);
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }
    }
}