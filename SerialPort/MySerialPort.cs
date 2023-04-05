using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace UpperComputer
{
    internal class MySerialPort
    {
        public static System.IO.Ports.SerialPort serialPort = null;
        private static readonly object locker = new object();

        public static System.IO.Ports.SerialPort GetInstance(string PortName, int BaudRate, int DataBits, Parity Parity, StopBits StopBits)
        {
            lock (locker)
            {
                if (serialPort == null)
                    serialPort = new System.IO.Ports.SerialPort();
                serialPort.PortName = PortName;
                serialPort.BaudRate = BaudRate;
                serialPort.DataBits = DataBits;
                serialPort.Parity = Parity;
                serialPort.StopBits = StopBits;
            }
            return serialPort;
        }
        public void Open()
        {
            
        }
        public void Close()
        {

        }
        public void read()
        {

        }
        public void write()
        {

        }
    }
}
