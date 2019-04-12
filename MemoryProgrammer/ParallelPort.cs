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
        [DllImport("inpout32.dll")]
        private static extern UInt32 IsInpOutDriverOpen();
        [DllImport("inpout32.dll")]
        private static extern void Out32(int PortAddress, int Data);
        [DllImport("inpout32.dll")]
        private static extern char Inp32(int PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "IsInpOutDriverOpen")]
        private static extern UInt32 IsInpOutDriverOpen_x64();
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        public static extern void Output(int address, int value);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        public static extern byte Input(int address);
        private int _Address = 888;
        bool inited = false;
        bool m_bX64 = false;
        public string init() 
        {
            String lblMessage = "";
            try
            {
                uint nResult = 0;
                try
                {
                    nResult = IsInpOutDriverOpen();
                }
                catch (BadImageFormatException)
                {
                    nResult = IsInpOutDriverOpen_x64();
                    if (nResult != 0)
                        m_bX64 = true;

                }

                if (nResult == 0)
                {
                    lblMessage = "Unable to open InpOut32 driver";
                }
                else
                {
                    inited = true;
                }
            }
            catch (DllNotFoundException ex)
            {
                lblMessage = ex.Message + " Unable to find InpOut32.dll";
            }

            return lblMessage;
        }
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
            if (!inited) return;
            if(m_bX64)
                Output(_Address, dataByte);
            else
                Out32(_Address, dataByte);

        }
        public byte GetByte()
        {
            if (!inited) return 0;
            int val = 0;
            if (m_bX64) val = Input(_Address + 1);
            else val = Inp32(_Address + 1);
            val = val >> 6;
            val &= 0x01;
            return (byte)val;
        }
    }

}
