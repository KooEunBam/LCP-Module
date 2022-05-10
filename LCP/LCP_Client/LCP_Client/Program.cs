using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using LCP_Client.Common;

namespace LCP_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //IP & Port number for Socket Client
            Console.Write("입력받을 횟수를 입력하세요 (0입력시 무한반복) : ");
            int num = Convert.ToInt32(Console.ReadLine());
            Console.Write("ip주소 입력하세요 : ");
            string ip = Console.ReadLine();
            Console.Write("Port번호 입력하세요 : ");
            int port = Convert.ToInt32(Console.ReadLine());

            printSimulator(num, ip, port);
        }

        private static void printSimulator(int num, string ip, int port)
        {
            int seq = 1; // sequence
            const int thread_sleep = 100; // Thread.Sleep

            byte[] data = new byte[64]; // 64byte data 
            byte[] dataseq = new byte[4]; // sequence to byte
            byte[] datagram = new byte[68]; // seq + data => 68 byte (총 데이타 전달량)
            List<byte> list = new List<byte>(); // list <= data + dataseq <= datagram (두 배열을 합하기 위해 list활용)


            Random random = new Random();

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언
            //client.Blocking = false; // 논블로킹으로 socket옵션을 설정함

            if (num > 0)
            {
                for (; seq <= num; seq++)
                {
                    random.NextBytes(data); // data에 랜덤바이트 부여

                    dataseq = BitConverter.GetBytes(seq); // dataseq에 seq를 바이트로 변화한 
                    list.AddRange(dataseq); // list에 dateseq, data모두 넣고
                    list.AddRange(data);
                    datagram = list.ToArray(); // datagram에 두 배열 저장

                    client.SendTo(Zip.Compress(Encoding.Default.GetString(datagram)), ep); // datagram 전송

                    Thread.Sleep(thread_sleep);
                }
            }
            else
            {
                while (true)
                {
                    random.NextBytes(data);

                    dataseq = BitConverter.GetBytes(seq);
                    list.AddRange(dataseq);
                    list.AddRange(data);
                    datagram = list.ToArray();

                    client.SendTo(Zip.Compress(Encoding.Default.GetString(datagram)), ep);
                    seq++;

                    Thread.Sleep(thread_sleep);
                }
            }
        }
    }
}
