using LCPServerNonBlocking.Common;
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

namespace LCPServerNonBlocking
{
    /// <summary>
    /// Interaction logic for LCPServerNonBlocking.xaml
    /// </summary>
    public partial class LCPServerNonBlock : Window
    {
        private readonly AutoResetEvent autoresetevent;
        private readonly AutoResetEvent autoresetevent2;
        private readonly Socket socket; // UdpClient 객체
        private readonly Queue<NewData> queue;
        private readonly object lockobject;

        private const int threadSleep = 5;
        private const int socketTimeout = 5000;

        private int seqOverflowChanged;
        private int dataOverflowChanged;

        private IPAddress ipAddress; // IP주소
        private IPEndPoint endpoint; // Port번호
        private Thread th1;
        private Thread th2;

        public LCPServerNonBlock()
        {
            this.autoresetevent = new AutoResetEvent(false);
            this.autoresetevent2 = new AutoResetEvent(false);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // Udp객체 생성
            this.queue = new Queue<NewData>();
            this.lockobject = new object();

            this.seqOverflowChanged = 0;
            this.dataOverflowChanged = 0;
            this.th1 = new Thread(new ThreadStart(ReceiveThread));
            this.th2 = new Thread(new ThreadStart(FileSaveThread));

            InitializeComponent();

            th1.Start();
            th2.Start();
        }

        private void ReceiveThread()
        {
            autoresetevent.WaitOne(); // 신호대기

            uint seq = 0;
            //int seq = 4294967295;

            int recv;
            //List<byte> list = new List<byte>();
            
            byte[] data = new byte[1024];

            Dispatcher.Invoke(() =>
            {
                this.ipAddress = IPAddress.Parse(ipTextBox.Text);
                this.endpoint = new IPEndPoint(ipAddress, Convert.ToInt32(portTextBox.Text));
            });

            socket.Bind(endpoint);
            // endpoint로 들어오는 connection들은 모두 바인딩함 / bind any incoming connection to that socket
            socket.Blocking = false;
            // socket non-blocking 

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, Dispatcher.Invoke(() => Convert.ToInt32(portTextBox.Text)));
            // sender을 통해 한 커넥션을 대기하고, wait for an incoming connection
            EndPoint tmpRemote = (EndPoint)sender;
            // sender에 한개 들어오면, tmpRemote에 저장  as soon as it gets one, wire it up to this tmpRemote

            while (true)
            {
                if(Dispatcher.Invoke(() => (string)(StartButton.Content) == "Start"))
                {
                    socket.Close();
                    autoresetevent.WaitOne();

                    //if(Dispatcher.Invoke(() => (string)(StartButton.Content) == "Stop"))
                    //{
                    //    Socket re_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    //    Dispatcher.Invoke(() =>
                    //    {
                    //        IPAddress re_ipaddress = IPAddress.Parse(ipTextBox.Text);
                    //        IPEndPoint re_endpoint = new IPEndPoint(re_ipaddress, Convert.ToInt32(portTextBox.Text));
                    //        endpoint = re_endpoint;
                    //    });
                    //    re_socket.Bind(endpoint);
                    //    re_socket.Blocking = false;
                    //    IPEndPoint re_sender = new IPEndPoint(IPAddress.Any, Dispatcher.Invoke(() => Convert.ToInt32(portTextBox.Text)));
                    //    EndPoint re_tmpRemote = (EndPoint)re_sender;
                    //}
                }
                try
                {
                    recv = socket.ReceiveFrom(data, ref tmpRemote);
                }
                catch (SocketException e)
                {
                    Thread.Sleep(threadSleep);
                    continue; // Nonblocking 모드에서 읽을 데이터가 없으면 SocketException 리턴함
                }

                NewData newdata = new NewData(Convert.ToUInt32(seq), data);

                lock (lockobject)
                {
                    queue.Enqueue(newdata);
                }

                if (newdata.seq != uint.MaxValue) // int.MaxValue = 2147483647
                {
                    seq++; // seq값 증가
                }
                else
                {
                    seq = 0;
                    seqOverflowChanged++;
                }
                Thread.Sleep(threadSleep);
            }
        }

        private void FileSaveThread()
        {
            uint packet_lost = 0;
            uint oldValue = 0;
            uint currentValue = 0;

            List<byte> sequenceList = new List<byte>(); // sequenceList 생성

            while (true)
            {
                NewData newdata = new NewData(); // NewData객체 생성

                if ((string)Dispatcher.Invoke(() => StartButton.Content) == "Start") // 버튼이 Start상태라면 신호 대기
                {
                    Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                        "Queue_seq : " + newdata.seq + " Queue_overflow : " + seqOverflowChanged + "\n");
                    Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                        "Data_seq : " + currentValue.ToString());
                    Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                        " Data_Overflow : " + dataOverflowChanged);
                    Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                        "\nPacket_Lost : " + packet_lost + "\n");

                    autoresetevent2.WaitOne();
                }


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
                        sequenceList.Add(newdata.data[i]); // list에 sequence (4byte)부터 넣어서 0번째 인덱스 부터 3번째 인덱스까지 추가함
                    }
                    if (BitConverter.ToInt32(sequenceList.ToArray(), 0) == int.MaxValue)
                    {
                        dataOverflowChanged++;
                    }
                    oldValue = currentValue;
                    currentValue = BitConverter.ToUInt32(sequenceList.ToArray(), 0);

                    if (!(currentValue == oldValue + 1))
                    {
                        packet_lost++;
                        Dispatcher.Invoke(() => dataResultTextBox.Text += "");   
                    }
                    else
                    {

                    }
                    //if(currentValue - oldValue != 1)
                    //{
                    //    if(currentValue - oldValue < 0)
                    //    {
                    //        packet_lost += uint.MaxValue - oldValue + currentValue;
                    //    }
                    //    else
                    //    {
                    //        if(currentValue > 0)
                    //            packet_lost += (currentValue - oldValue - 1);
                    //    }
                    //}

                    if (currentValue % 100 == 0)
                    {
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            "Queue_seq : " + newdata.seq + " Queue_overflow : " + seqOverflowChanged + "\n");
                        Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                            "Data_seq : " + currentValue.ToString());
                        Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                            " Data_Overflow : " + dataOverflowChanged);
                        Dispatcher.Invoke(() => dataResultTextBox.Text = dataResultTextBox.Text +
                            "\nPacket_Lost : " + packet_lost + "\n");
                    }
                    sequenceList.Clear();

                    //for (int i = 4; i < newdata.data.Length; i++) // 4부터 마지막 인덱스까지는 압축한 data
                    //{
                    //    sequenceList.Add(newdata.data[i]); // list에 압축한 데이타를 추가
                    //}
                    //decompress_data = Zip.Decompress(Convert.ToBase64String(sequenceList.ToArray())); // Decompress 
                    //sequenceList.Clear(); // list clear
                }

                // DeCompress -> .zip save
                //Zip.Decompress(Encoding.Default.GetString(newdata.data));


                Thread.Sleep(threadSleep);
            }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (ipTextBox.Text == "" || portTextBox.Text == "") ;  // TextBox가 비어있는 경우 동작하지 않음
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

        private void QueueResultTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            queueResultTextBox.ScrollToEnd();
        }

        private void DataResultTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            dataResultTextBox.ScrollToEnd();
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }
    }
}

