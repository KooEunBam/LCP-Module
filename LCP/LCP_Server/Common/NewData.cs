using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCP.Common
{
    public class NewData
    {
        public int seq;
        public byte[] data;

        public NewData()
        {
            seq = 0;
            data = null;
        }

        public NewData(int seq, byte[] data)
        {
            this.seq = seq;
            this.data = data;
        }
    }
}
