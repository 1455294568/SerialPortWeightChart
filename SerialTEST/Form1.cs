using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SerialTEST
{
    public partial class Form1 : Form
    {
        private string[] BOUNDRATE = { "300", "600", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
        private Series series = null;
        ulong i = 0;
        private SerialPort serialPort;
        private double pergram = 1;
        private double zerogram = 0;
        private double curentnum = 0;
        private Queue<double> Nums = new Queue<double>();
        private Thread thread;
        private string StartStr = "=";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(BOUNDRATE);
            comboBox1.SelectedText = "115200";
            series = chart1.Series.Add("Test");
            series.ChartType = SeriesChartType.Line;
            series.XValueType = ChartValueType.Double;
            var ports = SerialPort.GetPortNames();
            comboBox2.Items.AddRange(ports);
            comboBox2.SelectedIndex = 0;
            serialPort = new SerialPort();
            thread = new Thread(new ThreadStart(SetData))
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var data = serialPort.ReadExisting();
                if(data.StartsWith(StartStr))
                {
                    if (double.TryParse(data.Replace(StartStr, ""), out double num))
                    {
                        curentnum = num;
                        lock (this)
                        {
                            Nums.Enqueue(num);
                        }
                    }
                }
                serialPort.DiscardInBuffer();
            }
            catch
            {

            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "开始获取")
            {
                if (comboBox1.Text.Length < 0 || comboBox2.Text.Length < 0)
                {
                    MessageBox.Show("请先选择比特率或串口");
                }
                else
                {
                    serialPort = new SerialPort
                    {
                        PortName = comboBox2.Text,
                        BaudRate = int.Parse(comboBox1.Text),
                        Encoding = Encoding.ASCII
                    };
                    serialPort.DataReceived += SerialPort_DataReceived;
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();
                    }
                    button4.Text = "停止获取";
                }
            }
            else
            {
                button4.Text = "开始获取";
                serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            series.Points.Clear();
            i = 0;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            zerogram = curentnum;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                bool result = double.TryParse(textBox1.Text, out double temp);
                if (result && temp != 0)
                {
                    pergram = (curentnum - zerogram) / temp;
                }
                else
                {
                    MessageBox.Show("请输入不等于0的数字!");
                }
            }
            else
            {
                MessageBox.Show("请输入数字");
            }
        }

        private void SetData()
        {
            while (true)
            {
                if (Nums.Count > 0)
                {
                    double data = 0;
                    lock (this)
                    {
                        data = Nums.Dequeue();
                    }
                    Task.Factory.StartNew(() =>
                    {
                        this.Invoke((Action)(() =>
                        {
                            try
                            {
                                series.Points.AddXY(i++, data);
                                textBox2.Text = Convert.ToString((data - zerogram) / pergram);
                                textBox2.Refresh();
                            }
                            catch { }
                        }));
                        Thread.Sleep(50);
                    });
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            button4.Text = "开始获取";
            if (serialPort != null && serialPort.IsOpen)
            {
                e.Cancel = true;
                thread.Abort();
                serialPort.Close();
                serialPort.Disposed += (o, ee) =>
                {
                    Application.Exit();
                };
                serialPort.Dispose();
            }
        }
    }
}
