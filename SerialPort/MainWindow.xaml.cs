using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UpperComputer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.IO.Ports.SerialPort serialPort = null;
        private System.IO.Ports.SerialPort serialPortVirtual = null;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            ComboBoxParity.ItemsSource = Enum.GetValues(typeof(Parity));
            ComboBoxParity.SelectedIndex = 0;
            ComboBoxStopBits.ItemsSource = Enum.GetValues(typeof(StopBits));
            ComboBoxStopBits.SelectedIndex = 1;
            textBox_out.Text = "";
            textBox_in.AcceptsReturn = true;
            textBox_in.TextWrapping = TextWrapping.Wrap;
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

        private void ComboBoxPorts_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            string[] PortNames = System.IO.Ports.SerialPort.GetPortNames();
            comboBox.Items.Clear();

            foreach (string PortName in PortNames)
            {
                comboBox.Items.Add(PortName);
            }
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
            if (serialPort == null || !serialPort.IsOpen)
            {
                return;
            }

            string encodingName = comboBox_encoding.Text;
            byte[] byteData = Encoding.GetEncoding(encodingName).GetBytes(data);

            // 发送数据到串口
            serialPort.Write(byteData, 0, byteData.Length);
        }


        private async void serial_open_button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var portName = ComboBoxPorts.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(portName))
            {
                MessageBox.Show("未选择串口号！");
                return;
            }

            var baudRate = Convert.ToInt32((ComboBoxBauds.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "9600");
            var dataBits = Convert.ToInt32((ComboBoxDataBits.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "8");
            var parity = (Parity)ComboBoxParity.SelectedIndex;
            var stopBits = (StopBits)ComboBoxStopBits.SelectedIndex;

            if (serialPort == null)
            {
                serialPort = new System.IO.Ports.SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    ReadTimeout = 5000,
                    WriteTimeout = 5000,
                    ReceivedBytesThreshold = 1
                };

                serialPort.DataReceived += port_DataReceived;

                try
                {
                    await Task.Run(() => serialPort.Open());
                    button.Content = "关闭串口";
                }
                catch (IOException ex)
                {
                    MessageBox.Show("串口操作错误：" + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show("串口访问被拒绝：" + ex.Message);
                }
            }
            else
            {
                try
                {
                    await Task.Run(() =>
                    {
                        serialPort.Close();
                        serialPort.Dispose();
                        serialPort = null;
                    });
                    button.Content = "打开串口";
                }
                catch (IOException ex)
                {
                    MessageBox.Show("串口操作错误：" + ex.Message);
                }
            }
        }
        private byte[] receiveBuffer = new byte[4096];
        private Encoding receiveEncoding;

        void port_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int nRead = serialPort.BytesToRead;
            if (nRead > 0)
            {
                // 在UI线程上更新计数器和文本框内容
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    int receiveCount = Convert.ToInt32(receive_count.Content);
                    int bytesRead = 0;
                    receive_count.Content = receiveCount + 1;
                    try
                    {
                        bytesRead = serialPort.Read(receiveBuffer, 0, nRead);
                        if (data_forward.IsChecked==true && serialPortVirtual!=null && serialPortVirtual.IsOpen)
                        {
                            serialPortVirtual.Write(receiveBuffer, 0, bytesRead);
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    if (receiveEncoding == null || receiveEncoding.EncodingName != comboBox_encoding.Text)
                    {
                        receiveEncoding = Encoding.GetEncoding(comboBox_encoding.Text);
                    }
                    if (radio_hex.IsChecked == true)
                    {
                        for (int i = 0; i < bytesRead; i++)
                        {
                            textBox_out.AppendText(string.Format("{0:X2} ", receiveBuffer[i]));
                        }
                    }
                    else if (radio_string.IsChecked == true)
                    {
                        textBox_out.AppendText(receiveEncoding.GetString(receiveBuffer, 0, bytesRead));
                    }
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
        /// 发送自定义帧
        public async Task SendFrameAsync(byte func_code, byte[] data, int data_len)
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
            Dispatcher.Invoke(new Action(() =>
            {
                foreach (byte b in frame)
                {
                    textBox_Hex.Text += b.ToString("X2"); // 将每个字节转换为两位大写十六进制表示的字符串
                    textBox_Hex.Text += " ";
                }
                textBox_Hex.Text += "\n";
            }));

            // 发送帧到串口
            int length = frame.Length;
            int sendBytesNum = length / 4 * 4; // 每四个字节发送一次，计算需要发送的字节数
            byte[] tempBytes = new byte[4];
            int j;
            for (j = 0; j < sendBytesNum; j += 4)
            {
                Array.Copy(frame, j, tempBytes, 0, 4); // 每次取出四个字节
                serialPort.Write(tempBytes, 0, 4); // 发送四个字节
                await Task.Delay(200);
            }
            if (j < length)
            {
                Array.Copy(frame, j, tempBytes, 0, length - j); // 取出剩余不足四个字节的数据
                serialPort.Write(tempBytes, 0, length - j); // 发送不足四个字节的数据
                await Task.Delay(200);
            }
        }


        private async void button_speed_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_wheel.Text) && string.IsNullOrEmpty(textBox_speed_P.Text) && string.IsNullOrEmpty(textBox_speed_I.Text) && string.IsNullOrEmpty(textBox_speed_D.Text))
            {
                MessageBox.Show("值未输入完全");
                return;
            }
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
                await Task.Run(()=> SendFrameAsync(0x03, resultBytes, resultBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private async void button_angle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_angle_P.Text) && string.IsNullOrEmpty(textBox_angle_I.Text) && string.IsNullOrEmpty(textBox_angle_D.Text))
            {
                MessageBox.Show("值未输入完全");
                return;
            }

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
                await Task.Run(() => SendFrameAsync(0x04, resultBytes, resultBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }

        }

        private async void button_distance_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_disatance_P.Text) && string.IsNullOrEmpty(textBox_disatance_I.Text) && string.IsNullOrEmpty(textBox_disatance_D.Text))
            {
                MessageBox.Show("值未输入完全");
                return;
            }

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
                await Task.Run(() => SendFrameAsync(0x05, resultBytes, resultBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private async void button_distance1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_distance.Text))
            {
                MessageBox.Show("请输入距离值");
                return;
            }
            float distance_f = float.Parse(textBox_distance.Text);
            byte[] dBytes = BitConverter.GetBytes(distance_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                await Task.Run(() => SendFrameAsync(0x06, dBytes, dBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private async void button_angel1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_angle.Text))
            {
                MessageBox.Show("请输入角度值");
                return;
            }
            float angle_f = float.Parse(textBox_angle.Text);
            byte[] aBytes = BitConverter.GetBytes(angle_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                await Task.Run(() => SendFrameAsync(0x07, aBytes, aBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }

        private async void button_speed1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_speed.Text))
            {
                MessageBox.Show("请输入速度值");
                return;
            }
            float speed_f = float.Parse(textBox_speed.Text);
            byte[] sBytes = BitConverter.GetBytes(speed_f);
            if (serialPort != null && serialPort.IsOpen)
            {
                await Task.Run(()=> SendFrameAsync(0x08, sBytes, sBytes.Length));
            }
            else
            {
                MessageBox.Show("未打开串口");
            }
        }
        private const int MaxTextBoxLines = 200;
        private bool _scrollToEnd = true;
        private void textBox_out_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_scrollToEnd)
            {
                textBox_out.ScrollToEnd();
            }
            if (textBox_out.LineCount > 200)
            {
                int index = textBox_out.GetCharacterIndexFromLineIndex(200);
                textBox_out.Text = textBox_out.Text.Remove(0, index);
            }
        }

        private void textBox_out_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _scrollToEnd = false;
        }

        private void textBox_out_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _scrollToEnd = true;
        }


        private async void data_forward_Checked(object sender, RoutedEventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                data_forward.IsChecked = false;
                return;
            }
            serialPortVirtual = new System.IO.Ports.SerialPort(ComboBoxVirtualPorts.Text, Convert.ToInt32(ComboBoxVirtualBauds.Text), Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 5000,
                WriteTimeout = 5000,
                ReceivedBytesThreshold = 1
            };

            serialPortVirtual.DataReceived += port_DataReceived;

            try
            {
                await Task.Run(() => serialPortVirtual.Open());
            }
            catch (IOException ex)
            {
                MessageBox.Show("虚拟串口操作错误：" + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("虚拟串口访问被拒绝：" + ex.Message);
            }
        }

        private async void data_forward_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (serialPortVirtual != null && serialPortVirtual.IsOpen)
                    {
                        serialPortVirtual.Close();
                        serialPortVirtual.Dispose();
                        serialPortVirtual = null;
                    }
                });
            }
            catch (IOException ex)
            {
                MessageBox.Show("串口操作错误：" + ex.Message);
            }
        }
    }
}