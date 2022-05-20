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
    public partial class LCPClientNonBlock : Window
    {
        private readonly Random random;

        public LCPClientNonBlock()
        {
            this.random = new Random();

            InitializeComponent();
        }

        private async void DataSender(int num, int cycle, string ip, int port)
        {
            
            uint dataSequence = 0; // data sequence
            //int dataSequence = 2147483640; // data sequence overflow test
            uint dataSequenceOverflow = 0; // data sequence to check overflow
            int count = 1; // count for data
            int tenSecTimer = 10000; // ten sec to ms

            byte[] data = new byte[64]; // 64byte data 
            byte[] byteDataSeq = new byte[4]; // int sequence to byte
            byte[] datagram = new byte[1024]; // seq + compress_data
            byte[] compressedData; // compressed 64byte data
            List<byte> datagramList = new List<byte>(); // list <= seq + compress_data (두 배열을 합하기 위해 list활용)

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언

            if (num > 0) // 전송 횟수가 0보다 클 경우
            {
                while (count <= num)
                {
                    if ((string)startButton.Content == "Start")
                    {
                        break;
                    } // start 버튼의 content가 start인 경우 1. 처음시작, 2. Stop버튼 누른경우
                      // 2번만 해당하며, while문을 탈출 하더라도 click시 함수 인자값을 다시 넣으므로 실행에 문제없음

                    random.NextBytes(data); // data를 랜덤 바이트로 채움
                    compressedData = Zip.Compress(Encoding.Default.GetString(data)); // data를 압축

                    byteDataSeq = BitConverter.GetBytes(dataSequence); // dataSequence를 byte로 변환
                    datagramList.AddRange(byteDataSeq); // list에 data_seq, compress_data모두 넣고
                    datagramList.AddRange(compressedData);

                    datagram = datagramList.ToArray(); // datagram에 두 배열 저장
                    datagramList.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    if ((tenSecTimer - (cycle * count)) < 0) // 10초 + cycle 마다 수행함.
                    {
                        count = 0;

                        if (dataSequenceOverflow > 0)
                        {
                            transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                                $"데이타 보낸 수 : {dataSequence}, 오버플로우 횟수 : {dataSequenceOverflow}\n";
                        }
                        else
                        {
                            transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                                $"데이타 보낸 수 : {dataSequence}\n";
                        }
                    }

                    dataSequence++;

                    if(dataSequence == 0) // 증가된 dataSequence값이 0이면, 이전값은 최대 정수가 들어갔다는 의미
                    {
                        dataSequenceOverflow++;
                    }

                    count++;
                    await Task.Delay(cycle);
                }
                transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                    $"데이타 보낸 수 : {dataSequence}, 오버플로우 횟수 : {dataSequenceOverflow}"; // while문 탈출시 출력됨.
            }
            else if (num == 0) // 무한 반복 수행
            {
                while (true)
                {
                    if ((string)startButton.Content == "Start")
                    {
                        break;
                    }

                    random.NextBytes(data); // data 임의의 바이트로 채움
                    compressedData = Zip.Compress(Encoding.Default.GetString(data)); // data 압축

                    byteDataSeq = BitConverter.GetBytes(dataSequence);
                    datagramList.AddRange(byteDataSeq); // list에 data_seq, compress_data모두 넣고
                    datagramList.AddRange(compressedData);

                    datagram = datagramList.ToArray(); // datagram에 두 배열 저장
                    datagramList.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    if ((tenSecTimer - (cycle * count)) < 0) // 10초 + cycle 마다 수행함.
                    {
                        count = 0;

                        if (dataSequenceOverflow > 0)
                        {
                            transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                                $"데이타 보낸 수 : {dataSequence}, 오버플로우 횟수 : {dataSequenceOverflow}\n";
                        }
                        else
                        {
                            transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                                $"데이타 보낸 수 : {dataSequence}\n";
                        }
                    }

                    dataSequence++;

                    if (dataSequence == 0) // 증가된 dataSequence값이 0이면, 이전값은 최대 정수가 들어갔다는 의미
                    {
                        dataSequenceOverflow++;
                    }

                    count++;
                    await Task.Delay(cycle);
                }
                transactionQueueTextBox.Text = transactionQueueTextBox.Text +
                    $"데이타 보낸 수 : {dataSequence}, 오버플로우 횟수 : {dataSequenceOverflow}";
            }
        }

        //------------------------------------------------------------------------------------
        // Start 버튼 누르면, 인자값들이 빈칸이라면 작동 x
        // Start 버튼 누르면 초기화 및 Stop 버튼으로 바뀜. 
        // Stop  버튼 누르면(Start 버튼을 누르면 Stop으로 바뀜) 멈춤
        //------------------------------------------------------------------------------------
        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ipTextBox.Text) || string.IsNullOrEmpty(portTextBox.Text)
                || string.IsNullOrEmpty(transactionPeriodTextBox.Text)
                || string.IsNullOrEmpty(transactionTimeTextBox.Text)) ; // 비어있을시 작동 x
            else
            {
                if ((string)startButton.Content == "Stop")
                {
                    startButton.Content = "Start";
                }
                else
                {
                    transactionQueueTextBox.Text = "";
                    startButton.Content = "Stop";
                    DataSender(Convert.ToInt32(transactionTimeTextBox.Text), Convert.ToInt32(transactionPeriodTextBox.Text),
                        ipTextBox.Text, Convert.ToInt32(portTextBox.Text));
                }
            }
        }

        //------------------------------------------------------------------------------------
        // TextBox의 Text가 바뀌면 끝까지 스크롤해서 아래까지 내림.
        //------------------------------------------------------------------------------------
        private void transactionQueueTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            transactionQueueTextBox.ScrollToEnd();
        }

        //------------------------------------------------------------------------------------
        // 창을 닫을시 쓰레드 및 어플리케이션 종료하는 함수
        //------------------------------------------------------------------------------------
        private void WindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }
    }
}
