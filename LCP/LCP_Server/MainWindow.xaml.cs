using LCP.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Net.Sockets;
using System.Net;

namespace LCP
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private readonly AutoResetEvent autoresetevent;
        private readonly AutoResetEvent autoresetevent2;
        private readonly Socket socket; // UdpClient 객체
        private readonly Queue<NewData> queue;
        private readonly object lockobject;
        private readonly Mutex mutex;

        private const int thread_sleep = 100; // Thread 주기

        private IPAddress ipAddress; // IP주소
        private IPEndPoint endpoint; // Port번호
        private Thread th1;
        private Thread th2;


        public MainWindow()
        {
            this.autoresetevent = new AutoResetEvent(false);
            this.autoresetevent2 = new AutoResetEvent(false);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // Udp객체 생성
            this.queue = new Queue<NewData>();
            this.lockobject = new object();
            this.mutex = new Mutex();

            this.th1 = new Thread(new ThreadStart(Receive_Thread));
            this.th2 = new Thread(new ThreadStart(FileSave_Thread));

            InitializeComponent();

            th1.Start();
            th2.Start();
        }


        private void Receive_Thread()
        {
            autoresetevent.WaitOne(); // 신호대기

            int seq = 0;
            int recv;
            byte[] data = new byte[1024]; //  can hold maximum byte 1024, what we receive and send

            Dispatcher.Invoke(() =>
            {
                this.ipAddress = IPAddress.Parse(IP_TextBox.Text);
                this.endpoint = new IPEndPoint(ipAddress, Convert.ToInt32(Port_TextBox.Text));
            });

            socket.Bind(endpoint); // endpoint로 들어오는 connection들은 모두 바인딩함 / bind any incoming connection to that socket   

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, Dispatcher.Invoke(() => Convert.ToInt32(Port_TextBox.Text))); // sender을 통해 한 커넥션을 대기하고, wait for an incoming connection
            EndPoint tmpRemote = (EndPoint)sender; // sender에 한개 들어오면, tmpRemote에 저장  as soon as it gets one, wire it up to this tmpRemote

            while (true)
            {
                recv = socket.ReceiveFrom(data, ref tmpRemote); // recv는 배열 길이, tmpRemote에 받은 값들을 data에 저장, it stores all the client into socket variable
                NewData newdata = new NewData(seq, data);

                lock (lockobject)
                {
                    queue.Enqueue(newdata);
                }
                seq++; // seq값 증가
                // Thread sleep을 하면 UDP 손실이 일어날 수 있음
            }
        }

        private void FileSave_Thread()
        {
            int seq = 0; // 출력갯수
            while (true)
            {
                mutex.WaitOne(); // queue.Count , Dequeue() -> mutex필요함
                if (queue.Count() != 0)
                {
                    NewData newdata = queue.Dequeue();
                    mutex.ReleaseMutex();
                    Dispatcher.Invoke(() =>
                        Result_TextBox.Text += Encoding.UTF8.GetString(newdata.data) + "\n"); // 전송받은 데이타 출력
                    seq++; // 출력갯수 + 1
                }
                if (seq == 70) // 70개 출력하면 textbox초기화 후 i값 초기화 (메모리 과부화 현상)
                {
                    Dispatcher.Invoke(() => Result_TextBox.Text = "");
                    seq = 0;
                }
                else if (seq % 10 == 0)
                {
                    //파일로 저장
                }
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
                    StartButton.Content = "Stop";
                } // Start버튼이 있는 상태라면, Set보내주며, Stop으로 바꿔줌
            }
        }

        private void Result_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Result_TextBox.ScrollToEnd(); // 텍스트박스의 텍스트가 바뀌면 스크롤다운함
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }
    }
}
