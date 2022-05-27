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
    /// Interaction logic for LCPServerNonBlockingBlockingCollection.xaml
    /// </summary>
    public partial class LCPServerNonBlockingBlockingCollection : Window
    {

        private readonly AutoResetEvent autoresetevent;
        private readonly AutoResetEvent autoresetevent2;
        private readonly BlockingCollection<NewData> queue;

        private const int threadSleep = 7;
        private const int socketTimeout = 5000;
        private const int display = 200;

        private uint seqOverflowChanged;
        private uint dataOverflowChanged;

        private IPAddress ipAddress; // IP주소
        private IPEndPoint endpoint; // Port번호
        private Thread th1;
        private Thread th2;
        public LCPServerNonBlockingBlockingCollection()
        {
            this.autoresetevent = new AutoResetEvent(false);
            this.autoresetevent2 = new AutoResetEvent(false);

            this.queue = new BlockingCollection<NewData>(boundedCapacity : 600);

            this.seqOverflowChanged = 0;
            this.dataOverflowChanged = 0; ;
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

            while (true)
            {
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

                        NewData newdata = new NewData(seq, datagram);
                        queue.Add(newdata);

                        seq++; // seq증가
                        if (seq == 0) // overflow 발생 후 seq가 0이 되면
                        {
                            seq_overflow++; // overflow횟수 +1;
                        }

                        if (Dispatcher.Invoke(() => (string)StartButton.Content == "Start"))
                        {
                            socket.Close();
                            break; // while문 탈출하면 다른 while문에서 다시 소켓을 생성하게됨.
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
            List<byte> binaryList = new List<byte>(); // binaryList 생성

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

                while (!queue.IsCompleted) // Dequeue할때 queue가 null 이면 false, 값이 있다면 true
                {
                    newdata = queue.Take();

                    //int k = 0;
                    oldValue = currentValue;
                    byte[] decompress = new byte[newdata.data.Length - 4]; // 압축해제 후 배열 크기를 알 수 없어서 크게 선언하면 0값이 들어가게됨

                    for (j = 0; j < 4; j++)
                        sequenceList.Add(newdata.data[j]); // 0 ~ 3번 인덱스는 data의 sequence를 의미함
                    currentValue = BitConverter.ToUInt32(sequenceList.ToArray(), 0);
                    sequenceList.Clear();

                    for (; j < newdata.data.Length; j++) // j는 4가 되어있는 상태이므로 인덱스가 4 이후에는 data값.
                    {
                        binaryList.Add(newdata.data[j]); // 압축된 데이타를 binaryList에 삽입
                        //decompress[k] = newdata.data[j];
                        //k++;
                    }

                    //Debug.Write(decompressedData.Length);
                    //Debug.Write(Zip.Decompress(Convert.ToBase64String(decompress)));


                    if (!(oldValue == 0 && currentValue == 0)) // 처음에 oldvalue와 currentvalue가 0인 상태
                    {
                        if (!(currentValue == oldValue + 1)) // currentValue와 oldValue의 차이가 1이 아니라면 packet lost
                        {
                            packet_lost++;
                            Dispatcher.Invoke(() => dataResultTextBox.Text +=
                                "OldValue : " + oldValue.ToString() + " Current Value : " + currentValue.ToString()
                                + " Packet_Lost : " + packet_lost + "\n");
                        }
                    }
                    if (currentValue % display == 0) // display초기값 200으로 설정되어있음. display만큼 bin파일로 저장 및 log에 기록
                    {
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            "Queue_seq : " + newdata.seq + " Queue_overflow : " + seqOverflowChanged + "\n");
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            "Data_seq : " + currentValue.ToString());
                        Dispatcher.Invoke(() => queueResultTextBox.Text = queueResultTextBox.Text +
                            " Data_Overflow : " + dataOverflowChanged + "\n");

                        FileStream binFileStream = File.Open($"Test{filenumber}.bin", FileMode.Create); // bin파일 생성 후 파일 오픈상태
                        using (BinaryWriter binWriter = new BinaryWriter(binFileStream)) // binary로 작성
                        {
                            binWriter.Write(Zip.Decompress(Convert.ToBase64String(binaryList.ToArray()))); // binaryList를 배열로 변환뒤 decompression 후 파일에 작성
                            binaryList.Clear(); // List 초기화 후
                            binWriter.Close(); // Close.
                        }

                        var logger = new Logger(); // Logger클래스 생성자 
                        logger.Log("Save Completed"); // 로그에 년월일 시간 Save Completed 라고 저장.

                        filenumber++;
                    }
                    Thread.Sleep(threadSleep);
                }
                // TryDequeue(out T)가 false반환시 Idle상태.
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
