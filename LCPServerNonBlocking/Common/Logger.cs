using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCPServerNonBlocking.Common
{
    public class Logger
    {
        private string CurrentDirectory
        {
            get;
            set;
        }

        private string FileName 
        {
            get;
            set; 
        }

        private string FilePath 
        {
            get;
            set;
        }

        public Logger()
        {
            this.CurrentDirectory = Directory.GetCurrentDirectory(); // get current Directory of application
            this.FileName = "Log.ini";
            this.FilePath = this.CurrentDirectory + "/" + this.FileName;
        }

        public void Log(string Message)
        {
            using(StreamWriter w = File.AppendText(this.FilePath)) // 텍스트 파일을 읽고 쓸 수 있도록 함
            {
                w.WriteLine($"xx{DateTime.Now} : {Message}");
            }
        }
    }
}
