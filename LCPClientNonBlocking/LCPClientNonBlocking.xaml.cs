using LCPClientNonBlocking.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace LCPClientNonBlocking
{
    /// <summary>
    /// Interaction logic for LCPClientNonBlocking.xaml
    /// </summary>
    public partial class LCPClientNB : Window
    {
        private readonly Random random;

        public LCPClientNB()
        {
            this.random = new Random();

            InitializeComponent();
        }

        private async void Data_Sender(int num, int cycle, string ip, int port)
        {
            int data_sequence = 1; // data sequence
            //int data_sequence = 2147483640; // data sequence overflow test
            int data_sequence_overflow = 0; // data sequence to check overflow
            int count = 1; // count for data
            int ten_sec_timer = 10000; // ten sec to ms

            byte[] data = new byte[64]; // 64byte data 
            byte[] byte_data_seq = new byte[4]; // int sequence to byte
            byte[] datagram = new byte[1024]; // seq + compress_data
            byte[] compressed_data; // compressed 64byte data
            List<byte> datagram_list = new List<byte>(); // list <= seq + compress_data (두 배열을 합하기 위해 list활용)

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언

            if (num > 0)
            {
                while (count <= num)
                {
                    if ((string)start_button.Content == "Start")
                    {
                        break;
                    }
                    random.NextBytes(data);
                    compressed_data = Zip.Compress(Encoding.Default.GetString(data));

                    byte_data_seq = BitConverter.GetBytes(data_sequence);
                    datagram_list.AddRange(byte_data_seq); // list에 data_seq, compress_data모두 넣고
                    datagram_list.AddRange(compressed_data);

                    datagram = datagram_list.ToArray(); // datagram에 두 배열 저장
                    datagram_list.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    if (data_sequence != 2147483647)
                    {
                        if ((ten_sec_timer - (cycle * count)) < 0)
                        {
                            count = 0;

                            if (data_sequence_overflow > 0)
                            {
                                transaction_queue_textbox.Text =
                                    $"데이타 보낸 수 : {data_sequence}, 오버플로우 횟수 : {data_sequence_overflow}";
                            }
                            else
                            {
                                transaction_queue_textbox.Text = $"데이타 보낸 수 : {data_sequence}";
                            }
                        }
                        data_sequence++;
                    }
                    else
                    {
                        data_sequence = 1;
                        data_sequence_overflow++;
                    }
                    count++;
                    await Task.Delay(cycle);
                }
            }
            else if (num == 0)
            {
                while (true)
                {
                    if ((string)start_button.Content == "Start")
                    {
                        break;
                    }

                    random.NextBytes(data);
                    compressed_data = Zip.Compress(Encoding.Default.GetString(data));

                    byte_data_seq = BitConverter.GetBytes(data_sequence);
                    datagram_list.AddRange(byte_data_seq); // list에 data_seq, compress_data모두 넣고
                    datagram_list.AddRange(compressed_data);

                    datagram = datagram_list.ToArray(); // datagram에 두 배열 저장
                    datagram_list.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    if (data_sequence != 2147483647)
                    {
                        if ((ten_sec_timer - (cycle * count)) < 0)
                        {
                            count = 0;

                            if (data_sequence_overflow > 0)
                            {
                                transaction_queue_textbox.Text =
                                    $"데이타 보낸 수 : {data_sequence}, 오버플로우 횟수 : {data_sequence_overflow}";
                            }
                            else
                            {
                                transaction_queue_textbox.Text = $"데이타 보낸 수 : {data_sequence}";
                            }
                        }
                        data_sequence++;
                    }
                    else
                    {
                        data_sequence = 1;
                        data_sequence_overflow++;
                    }
                    count++;
                    await Task.Delay(cycle);
                }
            }
        }
        private void start_button_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ip_textbox.Text) || string.IsNullOrEmpty(port_textbox.Text)
                || string.IsNullOrEmpty(transaction_period_textbox.Text)
                || string.IsNullOrEmpty(transaction_time_textbox.Text)) ;
            else
            {
                if ((string)start_button.Content == "Stop")
                {
                    start_button.Content = "Start";
                }
                else
                {
                    transaction_queue_textbox.Text = "";
                    start_button.Content = "Stop";
                    Data_Sender(Convert.ToInt32(transaction_time_textbox.Text), Convert.ToInt32(transaction_period_textbox.Text),
                        ip_textbox.Text, Convert.ToInt32(port_textbox.Text));
                }
            }
        }

        private void transaction_queue_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

            transaction_queue_textbox.ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }

    }
}
