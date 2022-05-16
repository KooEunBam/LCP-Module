﻿using LCPClient__WPF_.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace LCPClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LCP_Client : Window
    {
        private readonly AutoResetEvent autoResetEvent;

        private readonly Random random;



        public LCP_Client()
        {
            this.autoResetEvent = new AutoResetEvent(false);

            this.random = new Random();

            InitializeComponent();



            Data_Sender(Convert.ToInt32(transaction_time_textbox.Text), Convert.ToInt32(transaction_period_textbox.Text),
                ip_textbox.Text, Convert.ToInt32(port_textbox.Text));
        }

        private void Data_Sender(int num, int cycle, string ip, int port)
        {
            int data_sequence = 1; // data sequence
            int data_sequence_overflow = 0; // data sequence to check overflow

            byte[] data = new byte[64]; // 64byte data 
            byte[] byte_data_seq = new byte[4]; // int sequence to byte
            byte[] datagram = new byte[1024]; // seq + compress_data
            byte[] compressed_data; // compressed 64byte data
            List<byte> datagram_list = new List<byte>(); // list <= seq + compress_data (두 배열을 합하기 위해 list활용)

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언

            if (num > 0)
            {
                while (data_sequence <= num)
                {
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
                        data_sequence++;
                    }
                    else
                    {
                        data_sequence = 0;
                        data_sequence_overflow++;
                    }
                    Thread.Sleep(cycle);
                }
            }
            else if (num == 0)
            {
                while (true)
                {
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
                        data_sequence++;
                    }
                    else
                    {
                        data_sequence = 0;
                        data_sequence_overflow++;
                    }
                    Thread.Sleep(cycle);
                }
            }
            else
            {
                transaction_queue_textbox.Text = "잘못 입력했습니다.";
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

        private void start(object sender, RoutedEventArgs e)
        {

        }

        private void start_button_click(object sender, RoutedEventArgs e)
        {

        }
    }
}