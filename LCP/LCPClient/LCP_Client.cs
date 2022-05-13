using LCPClient.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LCPClient
{
    internal class LCP_Client
    {
        static void Main(string[] args)
        {
            //IP & Port number for Socket Client
            Console.Write("입력받을 횟수를 입력하세요 (0입력시 무한반복) : ");
            int num = Convert.ToInt32(Console.ReadLine());
            Console.Write("전송 주기를 입력하세요 (단위 ms) : ");
            int cycle = Convert.ToInt32(Console.ReadLine());
            Console.Write("ip주소 입력하세요 : ");
            string ip = Console.ReadLine();
            Console.Write("Port번호 입력하세요 : ");
            int port = Convert.ToInt32(Console.ReadLine());

            Data_Sender(num, cycle, ip, port);
        }

        private static void Data_Sender(int num, int cycle, string ip, int port)
        {
            int seq = 1; // sequence
            int seq_overflow = 0; // sequence for overflow

            byte[] data = new byte[64]; // 64byte data 
            byte[] data_seq = new byte[4]; // int sequence to byte
            byte[] data_seq_overflow = new byte[4]; // int sequence for overflow
            byte[] datagram = new byte[1024]; // seq + compress_data
            byte[] compressed_data; // compressed 64byte data
            List<byte> datagram_list = new List<byte>(); // list <= seq + compress_data (두 배열을 합하기 위해 list활용)

            Random random = new Random();

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언
            //client.Blocking = false; // 논블로킹으로 socket옵션을 설정함

            data_seq_overflow = BitConverter.GetBytes(seq_overflow);

            if (num > 0) // 입력받을 횟수가 0보다 클때
            {
                while (seq <= num)
                {
                    random.NextBytes(data); // data에 랜덤바이트 부여
                    compressed_data = Zip.Compress(Encoding.Default.GetString(data)); // data를 String으로 변환 후 압축하고 리턴값을 data에 저장

                    data_seq = BitConverter.GetBytes(seq); // data_seq에 seq를 바이트로 변환
                    datagram_list.AddRange(data_seq); // list에 data_seq, compress_data모두 넣고
                    datagram_list.AddRange(data_seq_overflow);
                    datagram_list.AddRange(compressed_data);

                    datagram = datagram_list.ToArray(); // datagram에 두 배열 저장
                    datagram_list.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    checked
                    {
                        try
                        {
                            seq++;
                        }
                        catch (OverflowException)
                        {
                            seq_overflow++;
                            data_seq_overflow = BitConverter.GetBytes(seq_overflow);
                            seq = 0;
                        }
                    }
                    Thread.Sleep(cycle);
                }
            }
            else // 입력받은 횟수가 0 (무한반복) 일때
            {
                while (true)
                {
                    random.NextBytes(data); // data에 랜덤바이트 부여
                    compressed_data = Zip.Compress(Encoding.Default.GetString(data)); // data를 String으로 변환 후 압축하고 리턴값을 data에 저장

                    data_seq = BitConverter.GetBytes(seq); // data_seq에 seq를 바이트로 변환
                    data_seq_overflow = BitConverter.GetBytes(seq_overflow);
                    datagram_list.AddRange(data_seq); // list에 data_seq, compress_data모두 넣고
                    datagram_list.AddRange(data_seq_overflow);
                    datagram_list.AddRange(compressed_data);

                    datagram = datagram_list.ToArray(); // datagram에 두 배열 저장
                    datagram_list.Clear(); // list 초기화

                    client.SendTo(datagram, ep); // data는 압축상태, seq 전송

                    checked
                    {
                        try
                        {
                            seq++;
                        }
                        catch (OverflowException e)
                        {
                            seq_overflow++;
                            data_seq_overflow = BitConverter.GetBytes(seq_overflow);
                            seq = 0;
                        }
                    }
                    Thread.Sleep(cycle);
                }
            }
        }

    }
}
