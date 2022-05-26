using LCPServerNonBlocking.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// Interaction logic for LCPServerNonBlockingConcurrentQueue.xaml
    /// </summary>
    public partial class LCPServerNonBlockingConcurrentQueue : Window
    {
        private readonly AutoResetEvent autoresetevent;
        private readonly AutoResetEvent autoresetevent2;
        private readonly ConcurrentQueue<NewData> queue;
        private readonly ConcurrentQueue<NewData> queueCopy;

        private const int threadSleep = 7;
        private const int socketTimeout = 5000;
        private const int display = 200;

        private uint seqOverflowChanged;
        private uint dataOverflowChanged;

        private IPAddress ipAddress; // IP주소
        private IPEndPoint endpoint; // Port번호
        private Thread th1;
        private Thread th2;

        public LCPServerNonBlockingConcurrentQueue()
        {
            this.autoresetevent = new AutoResetEvent(false);
            this.autoresetevent2 = new AutoResetEvent(false);

            this.queue = new ConcurrentQueue<NewData>();
            this.queueCopy = new ConcurrentQueue<NewData>();

            this.seqOverflowChanged = 0;
            this.dataOverflowChanged = 0;;
            this.th1 = new Thread(new ThreadStart(ReceiveThread));
            this.th2 = new Thread(new ThreadStart(FileSaveThread));

            InitializeComponent();

            th1.Start();
            th2.Start();
        }

        private void ReceiveThread()
        {
            int recv;
            uint seq = 0;
            uint seq_overflow = 0;
            byte[] datagram = new byte[250];
            //uint seq = 4294967295; // for overflow test
            //int dataCount = 0;
            List<byte> list = new List<byte>(); // for enqueue seq test

            while (true) {
                autoresetevent.WaitOne();

                if (Dispatcher.Invoke(() => (string)StartButton.Content == "Stop")) 
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // UdpClient 객체

                    this.ipAddress = IPAddress.Parse(Dispatcher.Invoke(() => ipTextBox.Text));
                    this.endpoint = new IPEndPoint(ipAddress, Dispatcher.Invoke(() => Convert.ToInt32(portTextBox.Text)));

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
                        try
                        {
                            recv = socket.ReceiveFrom(datagram, ref tmpRemote);
                        }
                        catch (SocketException)
                        {
                            Thread.Sleep(threadSleep);  // Nonblocking 모드에서 읽을 데이터가 없으면 SocketException 리턴함
                            continue;
                        }

                        ////------------------------------------------------------------------------
                        //// To Test Datagram Sequence
                        ////------------------------------------------------------------------------
                        //for (int i = 0; i < 4; i++)
                        //{
                        //    list.Add(datagram[i]);
                        //}
                        //Debug.Write(Convert.ToString(BitConverter.ToUInt32(list.ToArray(), 0)) + " ");
                        //list.Clear();

                        NewData newdata = new NewData(seq, datagram);
                        queue.Enqueue(newdata);

                        ////------------------------------------------------------------------------
                        //// Enqueue 갯수 확인
                        ////------------------------------------------------------------------------
                        //Debug.Write(Convert.ToString(queue.Count()) + " ");

                        seq++; // seq증가
                        if (seq == 0) // overflow 발생 후 seq가 0이 되면
                        {
                            seq_overflow++; // overflow횟수 +1;
                        }

                        if(Dispatcher.Invoke(() => (string)StartButton.Content == "Start"))
                        {
                            socket.Close();
                            break;
                        }
                        Thread.Sleep(threadSleep);
                    }
                } 
            }
        }

        private void FileSaveThread()
        {
            uint packet_lost = 0;
            uint oldValue = 0;
            uint currentValue = 0;
            uint filenumber = 1;
            int j = 0; // for index

            List<byte> sequenceList = new List<byte>(); // sequenceList 생성
            List<byte> binaryList = new List<byte>();

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

                    filenumber = 1;
                    autoresetevent2.WaitOne();
                }         

                while (queue.TryDequeue(out newdata))
                {
                    //int k = 0;
                    oldValue = currentValue;
                    byte[] decompress = new byte[newdata.data.Length - 4];

                    for (j = 0; j < 4; j++)
                        sequenceList.Add(newdata.data[j]);
                    currentValue = BitConverter.ToUInt32(sequenceList.ToArray(), 0);
                    sequenceList.Clear();
                          
                    for (; j < newdata.data.Length; j++)
                    {
                        binaryList.Add(newdata.data[j]);
                        //decompress[k] = newdata.data[j];
                        //k++;
                    }

                    //Debug.Write(decompressedData.Length);
                    //Debug.Write(Zip.Decompress(Convert.ToBase64String(decompress)));

                    
                    if (!(oldValue == 0 && currentValue == 0)) // 처음에 oldvalue와 currentvalue가 0인 상태
                    {
                        if (!(currentValue == oldValue + 1))
                        {
                            packet_lost++;
                            Dispatcher.Invoke(() => dataResultTextBox.Text +=
                                "OldValue : " + oldValue.ToString() + " Current Value : " + currentValue.ToString()
                                + " Packet_Lost : " + packet_lost + "\n");
                        }
                    }
                    if (currentValue % display == 0)
                    {
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            "Queue_seq : " + newdata.seq + " Queue_overflow : " + seqOverflowChanged + "\n");
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            "Data_seq : " + currentValue.ToString());
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            " Data_Overflow : " + dataOverflowChanged + "\n");

                        FileStream binFileStream = File.Open($"Test{filenumber}.bin", FileMode.Create);
                        using (BinaryWriter binWriter = new BinaryWriter(binFileStream))
                        {
                            binWriter.Write(Zip.Decompress(Convert.ToBase64String(binaryList.ToArray())));
                            binaryList.Clear();
                            binWriter.Close();
                        }

                        var logger = new Logger();
                        logger.Log("Save Completed");

                        filenumber++;
                    }   
                }
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