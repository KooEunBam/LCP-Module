using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCPClient.Common
{
    internal class Zip
    {
        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text); // string text를 utf-8로 인코딩 함
            MemoryStream ms = new MemoryStream(); // 백업 저장소가 메모리인 스트림 생성
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true)) // 스트림을 압축하거나 압축을 푸는데 사용됨
            {                                                                           // 메모리 관리는 적절한 시기에 Dispose(해제) -> using()사용함
                zip.Write(buffer, 0, buffer.Length);  // 압축된 바이트를 gzip 스트림에 씀
            }

            ms.Position = 0; // 스트림 내의 위치를 가져오거나 설정
            MemoryStream outStream = new MemoryStream(); // 백업 저장소가 메모리인 스트림

            byte[] compressed = new byte[ms.Length]; // ms의 길이만큼 byte배열 생성
            ms.Read(compressed, 0, compressed.Length); // 현재 스트림에서 compressed 바이트 블록 읽어서 버퍼에 씀

            byte[] gzBuffer = new byte[compressed.Length + 4]; // compressed 배열 길이 + 4만큼의 배열 생성 
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length); // compressed 0번째 인덱스부터 gzbuffer 4번째 인덱스에 compressed.Length만큼 복사
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4); // buffer.length를 바이트 배열로 변환 

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
