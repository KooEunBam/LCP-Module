using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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
            int seq = 0; // sequence
            const int thread_sleep = 100;

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port); // 서버의 주소 지정
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // udp 소켓 client 선언
            //client.Blocking = false; // 논블로킹으로 socket옵션을 설정함

            if (num > 0)
            {
                for (; seq < num; seq++)
                {
                    //try
                    //{
                    //    byte[] packetData = Encoding.UTF8.GetBytes(seq + "번째 "); // 문자열을 바이트배열로 변환
                    //    client.SendTo(packetData, ep);
                    //}
                    //catch (SocketException e)
                    //{
                    //    if (e.ErrorCode == 10035)
                    //    {
                    //        // WSAWORLDBLOCK: 리소스가 일시적으로 사용이 불가능하다 .
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("{0} : {1}", e.ErrorCode, e.Message);
                    //        client.Close();
                    //        Environment.Exit(-1);
                    //    }
                    byte[] packetData = Encoding.UTF8.GetBytes(seq + "번째 "); // 문자열을 바이트배열로 변환
                    client.SendTo(packetData, ep);
                    Thread.Sleep(thread_sleep);
                }
            }
            else if (num == 0)
            {
                while (true)
                {
                    byte[] packetData = Encoding.UTF8.GetBytes(seq + "번째 "); // 문자열을 바이트배열로 변환
                    client.SendTo(packetData, ep);
                    seq++;
                    Thread.Sleep(thread_sleep);
                }
            }
        }
    }
}
