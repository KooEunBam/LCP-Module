using _12.LCP_UDPProtocol.Common;
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

namespace _12.LCP_UDPProtocol
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly AutoResetEvent autoResetEvent;
        private readonly Queue<NewData> queue; // Queue 객체
        private readonly UdpClient udpClient; // UdpClient 객체


        private const int thread_sleep = 100; // Thread 주기
        private const int thread_runningtime = 10000;

        
        private IPAddress ip_Address; // IP주소
        private IPEndPoint endpoint; // Port번호
        private byte[] data;

        private Thread th1;
        //private Thread th2;

        

        public MainWindow()
        {
            this.autoResetEvent = new AutoResetEvent(false);
            this.queue = new Queue<NewData>();
            this.udpClient = new UdpClient(); // UdpClient 객체 생성


            this.ip_Address = new IPAddress(long.Parse(IP_TextBox.Text)); // IP주소
            this.endpoint = new IPEndPoint(ip_Address, int.Parse(Port_TextBox.Text)); // Port번호
            this.data = new byte[(udpClient.Receive(ref endpoint)).Length]; 
            // 수신 데이터와 함께 상대 컴퓨터의 종단점 정보도 같이 전달받는데,
            // 이를 위해 IPEndPoint 객체를 ref파라미터로 전달함

            this.th1 = new Thread(new ThreadStart(thread1));
            //this.th2 = new Thread(new ThreadStart(thread2));

            InitializeComponent();

            th1.Start();
            //th2.Start();
        }

        private void thread1()
        {
            while (true)
            {
                //data = udpClient.Receive(ref endpoint);
                for (int i = 0; ;i++)
                {
                    if (udpClient.Available > 0)
                    {
                        NewData newData = new NewData(i, data);
                        queue.Enqueue(newData);
                    }
                    else
                    {
                        udpClient.Close();
                        break;
                    }
                }

                autoResetEvent.WaitOne(thread_sleep);
            }
        }



        //private void thread2()
        //{
        //    while (true)
        //    {
        //        Thread.Sleep(thread_sleep);
        //    } 
        //}

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if(IP_TextBox.Text == "" || Port_TextBox.Text == "")
            {
                autoResetEvent.WaitOne();
            }
            else
            {
                autoResetEvent.Set();
            }
        }
    }
}
