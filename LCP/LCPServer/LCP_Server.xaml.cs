using LCPServer.Common;
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
using System.Windows.Shapes;

namespace LCPServer
{
    /// <summary>
    /// Interaction logic for LCP_Server.xaml
    /// </summary>
    public partial class LCP_Server : Window
    {
        private readonly AutoResetEvent autoresetevent;
        private readonly AutoResetEvent autoresetevent2;
        private readonly Socket socket; // UdpClient 객체
        private readonly Queue<NewData> queue;
        private readonly object lockobject;

        private const int thread_sleep = 1;

        private IPAddress ipAddress; // IP주소
        private IPEndPoint endpoint; // Port번호
        private Thread th1;
        private Thread th2;

        public LCP_Server()
        {
            this.autoresetevent = new AutoResetEvent(false);
            this.autoresetevent2 = new AutoResetEvent(false);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // Udp객체 생성
            this.queue = new Queue<NewData>();
            this.lockobject = new object();

            this.th1 = new Thread(new ThreadStart(Receive_Thread));
            this.th2 = new Thread(new ThreadStart(FileSave_Thread));

            InitializeComponent();

            th1.Start();
            th2.Start();
        }


        private void Receive_Thread()
        {
            autoresetevent.WaitOne(); // 신호대기

            int seq = 1;
            int recv;
            //List<byte> list = new List<byte>();
            byte[] data = new byte[1024];

            Dispatcher.Invoke(() =>
            {
                this.ipAddress = IPAddress.Parse(IP_TextBox.Text);
                this.endpoint = new IPEndPoint(ipAddress, Convert.ToInt32(Port_TextBox.Text));
            });

            socket.Bind(endpoint);
            // endpoint로 들어오는 connection들은 모두 바인딩함 / bind any incoming connection to that socket   

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, Dispatcher.Invoke(() => Convert.ToInt32(Port_TextBox.Text)));
            // sender을 통해 한 커넥션을 대기하고, wait for an incoming connection
            EndPoint tmpRemote = (EndPoint)sender;
            // sender에 한개 들어오면, tmpRemote에 저장  as soon as it gets one, wire it up to this tmpRemote

            while (true)
            {
                recv = socket.ReceiveFrom(data, ref tmpRemote);

                // recv는 배열 길이, tmpRemote에 받은 값들을 data에 저장, it stores all the client into socket variable
                //list.AddRange(data);
                NewData newdata = new NewData(seq, data);

                lock (lockobject)
                {
                    queue.Enqueue(newdata);
                }
                seq++; // seq값 증가
                       // Thread sleep을 하면 UDP 손실이 일어날 수 있음
                Thread.Sleep(thread_sleep);
            }
        }

        private void FileSave_Thread()
        {
            int packet_lost = 0;
            int overflow_changed = 0;

            List<byte> sequence_list = new List<byte>(); // sequence_list 생성
            List<int> packet_lost_check = new List<int>();
            packet_lost_check.Add(0); // 초기에 비교할 숫자 추가

            while (true)
            {
                if ((string)Dispatcher.Invoke(() => StartButton.Content) == "Start") // 버튼이 Start상태라면 신호 대기
                {
                    autoresetevent2.WaitOne();
                }

                NewData newdata = new NewData(); // NewData객체 생성

                lock (lockobject)
                {
                    if (queue.Count() != 0)
                    {
                        newdata = queue.Dequeue(); // 큐에 데이터가 있는 상태라면 Dequeue
                    }
                }

                if (newdata.data != null)
                {
                    //byte[] decompress_data;

                    for (int i = 0; i < 4; i++)
                    {
                        sequence_list.Add(newdata.data[i]); // list에 sequence (4byte)부터 넣어서 0번째 인덱스 부터 3번째 인덱스까지 추가함
                    }
                    Dispatcher.Invoke(() => Result_TextBox.Text = Result_TextBox.Text +
                        "Data_seq : " + BitConverter.ToInt32(sequence_list.ToArray(), 0).ToString());
                    packet_lost_check.Add(BitConverter.ToInt32(sequence_list.ToArray(), 0)); // packet lost check 리스트에 data_seq값 추가
                    sequence_list.Clear();

                    for (int i = 4; i < 8; i++) // 4 - 8번째는 overflow가 되면 증가할 정수형 숫자
                    {
                        sequence_list.Add(newdata.data[i]);
                    }
                    Dispatcher.Invoke(() => Result_TextBox.Text = Result_TextBox.Text +
                        " Data_Overflow : " + BitConverter.ToInt32(sequence_list.ToArray(), 0).ToString());

                    if (packet_lost_check.Last() - packet_lost_check[packet_lost_check.Count - 2] == 1) ; // 리스트의 마지막 요소와 그 전 요소의 차이가 1이면 packet lost 없음
                    else
                    {
                        packet_lost += packet_lost_check.Last() - packet_lost_check[packet_lost_check.Count - 2] - 1; // 2이상 차이날 경우 packet lost 발생
                    }
                    sequence_list.Clear();
                    Dispatcher.Invoke(() => Result_TextBox.Text = Result_TextBox.Text +
                        " Packet_Lost : " + packet_lost + "\n");

                    //for (int i = 4; i < newdata.data.Length; i++) // 4부터 마지막 인덱스까지는 압축한 data
                    //{
                    //    sequence_list.Add(newdata.data[i]); // list에 압축한 데이타를 추가
                    //}
                    //decompress_data = Zip.Decompress(Convert.ToBase64String(sequence_list.ToArray())); // Decompress 
                    //sequence_list.Clear(); // list clear
                }
                // DeCompress -> .zip save
                //Zip.Decompress(Encoding.Default.GetString(newdata.data));

                Thread.Sleep(thread_sleep);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (IP_TextBox.Text == "" || Port_TextBox.Text == "") ;  // TextBox가 비어있는 경우 동작하지 않음
            else
            {
                if ((string)StartButton.Content == "Stop") // stop버튼 상태에서 클릭시, textbox 초기화 후 start로 바꿔줌 (초기화 하지 않으면 freezing현상있음)
                {
                    StartButton.Content = "Start";
                }
                else
                {
                    autoresetevent.Set();
                    autoresetevent2.Set();
                    StartButton.Content = "Stop";
                } // Start버튼이 있는 상태라면, Set보내주며, Stop으로 바꿔줌
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }

        private void Result_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Result_TextBox.ScrollToEnd();
        }
    }
}
