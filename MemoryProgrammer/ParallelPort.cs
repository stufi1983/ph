using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Runtime.InteropServices;
//using System.Threading;
//using System.Diagnostics;

namespace MemoryProgrammer
{
    public class ParallelPort
    {
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        public static extern void Output(int address, int value);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        public static extern byte Input(int address);
        private int _Address = 888;
        public ParallelPort()
        { 
        }

        public int Address
        {
            get
            {
                return _Address != 0 ? _Address : 888;
            }
            set
            {
                if(value != 0)
                    _Address = value; 
            }
        }

        public void SendByte(int dataByte)
        {
            Output(_Address, dataByte);
        }
        public byte GetByte()
        {
            int val = Input(_Address+1);
            val = val >> 6;
            val &= 0x01;
            return (byte)val;
        }
    }

}
