using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Ports;
using System.Diagnostics;
using System.Threading;

namespace MemoryProgrammer
{
    public class SerialSPI : BitbangSPI
    {
        SerialPort koneksiSerial;
        int periode = 10;

        /*
         * DTR4->MOSI
         * RTS7->SCK
         * TXD3->RST
         * CTS8<-MISO
        */

        public SerialSPI(string portAddress, int speed)
        {
            periode = (int)(1000000 / speed);
            if (periode < 10) periode = 10;
            if (periode > 10000000) periode = 10000000; //10S

            koneksiSerial = new SerialPort(portAddress, 50);
            //koneksiSerial.Handshake = Handshake.None;
            //koneksiSerial.WriteTimeout = 500;
            //koneksiSerial.ReadTimeout = 500;
            //koneksiSerial.Open();
        }

        public void open()
        {
            try
            {
                koneksiSerial.Open();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message,e);
            }
        }

        public void close()
        {
            if (koneksiSerial.IsOpen)
                koneksiSerial.Close();
        }

        //public enum BIT { LOW, HIGH };
        //public enum EDGE { RISING, FALLING };
        BIT activeSCK = BIT.HIGH;
        BIT activeCS = BIT.HIGH;
        Stopwatch timePerParse;
        private EDGE Sampling_edge = EDGE.RISING; //sample at rising edge

        private byte[] csbytetx = { 0 };
        public BIT Set_Active_CS
        {
            set
            {
                activeCS = value;
            }
        }
        public BIT Set_Active_SCLK
        {
            set
            {
                activeSCK = value;
            }
        }
        public EDGE Set_Sampling_At
        {
            set
            {
                Sampling_edge = value;
            }
        }

        public void spi_init()
        {
            if (activeSCK == BIT.HIGH)
                koneksiSerial.RtsEnable = false;
            else
                koneksiSerial.RtsEnable = true;

            //TODO
            if (activeCS == BIT.HIGH) 
                csbytetx[0] = 0; 
            else
                csbytetx[0] = 0xff;
        }

        public void spi_set_cs()
        {
            //TODO
            koneksiSerial.Write(csbytetx, 0, 1);
            Thread.Sleep(30); //wait for rising byte
        }

        public void spi_clr_cs()
        {
            //TODO
            byte[] icsbytetx = {0};
            icsbytetx[0] = (byte)((csbytetx[0] ^ csbytetx[0])& 0xff);
            koneksiSerial.Write(csbytetx, 0, 1);
            Thread.Sleep(10); //wait for falling byte
        }
        public byte spi_bit_rx() { return 0; }
        public void spi_bit_tx(byte bit)
        {
            koneksiSerial.DtrEnable = (bit == 1) ? true : false; //DI 1/0
            spi_set_cs(); //CS Enable

            //low state
            if (Sampling_edge == EDGE.RISING)
            {
                koneksiSerial.RtsEnable = false; //SCK = 0
                delay_us(periode); // >450ns TCKL
            }

            ////rising
            
            ////high state
            koneksiSerial.RtsEnable = true; //SCK = 1
            delay_us(periode); // >450ns TCKL

            ////falling 
            
            ////low state
            if (Sampling_edge == EDGE.FALLING)
            {
                koneksiSerial.RtsEnable = false; //SCK = 0
                delay_us(periode); // >450ns TCKL
            }
            spi_clr_cs();
            delay_us(periode);
        }

        public void delay_us(int tick)
        {
            timePerParse = Stopwatch.StartNew();
            while (timePerParse.ElapsedTicks < tick * 10) //tick = 100ns (0.1us)
                Thread.SpinWait(1);
            timePerParse.Stop();
        }


    }
}
