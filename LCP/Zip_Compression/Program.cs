using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string str_8 = "1234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234";
            //string str_8 = "TESTTEST";
            string str_16 = "TESTTESTTESTTEST";
            string str_32 = "TESTTESTTESTTESTTESTTESTTESTTEST";
            string str_64 = "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST";
            string str_128 = "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST";
            string str_256 = "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST"
                + "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST";
            string str_512 = "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST"
                + "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST"
                + "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST"
                + "TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST";

            byte[] buffer_8 = Compress(str_8);
            Console.WriteLine("8byte Buffer 압축 후 length: " + buffer_8.Length);

            buffer_8 = Decompress(Convert.ToBase64String(buffer_8));

            Console.WriteLine("압축 해제 length: " + buffer_8.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_8) + "\n\n");

            byte[] buffer_16 = Compress(str_16);
            Console.WriteLine("16byte Buffer 압축 후 length: " + buffer_16.Length);

            buffer_16 = Decompress(Convert.ToBase64String(buffer_16));

            Console.WriteLine("압축 풀기 length: " + buffer_16.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_16) + "\n\n");

            byte[] buffer_32 = Compress(str_32);
            Console.WriteLine("32byte Buffer 압축 후 length: " + buffer_32.Length);

            buffer_32 = Decompress(Convert.ToBase64String(buffer_32));

            Console.WriteLine("압축 풀기 length: " + buffer_32.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_32) + "\n\n");

            byte[] buffer_64 = Compress(str_64);
            Console.WriteLine("64byte Buffer 압축 후 length: " + buffer_64.Length);

            buffer_64 = Decompress(Convert.ToBase64String(buffer_64));

            Console.WriteLine("압축 풀기 length: " + buffer_64.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_64) + "\n\n");

            byte[] buffer_128 = Compress(str_128);
            Console.WriteLine("128byte Buffer 압축 후 length: " + buffer_128.Length);

            buffer_128 = Decompress(Convert.ToBase64String(buffer_128));

            Console.WriteLine("압축 풀기 length: " + buffer_128.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_128) + "\n\n");

            byte[] buffer_256 = Compress(str_256);
            Console.WriteLine("256byte Buffer 압축 후 length: " + buffer_256.Length);

            buffer_256 = Decompress(Convert.ToBase64String(buffer_256));

            Console.WriteLine("압축 풀기 length: " + buffer_256.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_256) + "\n\n");

            byte[] buffer_512 = Compress(str_512);
            Console.WriteLine("512byte Buffer 압축 후 length: " + buffer_512.Length);

            buffer_512 = Decompress(Convert.ToBase64String(buffer_512));

            Console.WriteLine("압축 풀기 length: " + buffer_512.Length + "\n결과: " + Encoding.UTF8.GetString(buffer_512) + "\n\n");
        }

        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);

            return gzBuffer;
        }

        public static byte[] Decompress(string compressedText)
        {
            byte[] gzBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

                byte[] buffer = new byte[msgLength];

                ms.Position = 0;
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);
                }

                return buffer;
            }
        }
    }
}
