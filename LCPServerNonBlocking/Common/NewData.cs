using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCPServerNonBlocking.Common
{
    public class NewData
    {
        public uint seq;
        public byte[] data = new byte[1024];

        public NewData()
        {
            seq = 0;
            data = null;
        }

        public NewData(uint seq, byte[] data)
        {
            this.seq = seq;
            this.data = data;
        }
    }
}

