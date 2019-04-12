using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Threading;

namespace MemoryProgrammer
{
    //P2 (bit 0)--> CS
    //P3 (bit 1)--> DI (MOSI)
    //P4 (bit 2)--> DO (MISO)
    //P5 (bit 3)--> SCK
    public class ParalelSPI : BitbangSPI
    {
        int periode = 10;
        //public enum BIT { LOW, HIGH};
        //public enum EDGE { RISING, FALLING};
        private byte CS_bit = 0x00;
        private byte SCLK_bit = 0x00;
        private EDGE Sampling_edge = EDGE.RISING; //sample at rising edge
        public BIT Set_Active_CS
        {
            set
            {
                if (value == BIT.LOW)
                    CS_bit = 0x00;
                if (value == BIT.HIGH)
                    CS_bit = 0x01;
            }
        }
        public BIT Set_Active_SCLK
        {
            set
            {
                if (value == BIT.LOW)
                    SCLK_bit = 0x00;
                if (value == BIT.HIGH)
                    SCLK_bit = 0x08;
            }
        }
       public EDGE Set_Sampling_At
        {
            set
            {
                Sampling_edge = value;
            }
        }    
    
        Stopwatch timePerParse;
        ParallelPort parPort = new ParallelPort();
        
        public ParalelSPI(int portAdress, int speed)
        {
            if (portAdress == 0) portAdress = 888;
            parPort.Address = portAdress;
            periode = (int)(1000000 / speed);
            if (periode < 10) periode = 10;
            if (periode > 10000000) periode = 10000000; //10S
        }
        public void open() 
        {
            String res = parPort.init();
            if (res != "") throw new Exception(res);
            //TODO
            //return true;
        }
        public void close() { }
        public void spi_init()
        {
            int byteVal = 0;
            byteVal |= (CS_bit ^ 0x01);
            byteVal |= (SCLK_bit ^ 0x08);
            parPort.SendByte(byteVal);
            delay_us(periode); // >450 TCSH
        }

        
        public void spi_set_cs()
        {
            parPort.SendByte(CS_bit);
            delay_us(periode); // >250ns TCSS
        }
        public byte spi_bit_rx()
        {
            //TODO
            byte val = 0;
            byte x;

            //rising state
            if (Sampling_edge == EDGE.RISING)
            {
                x = 0;
                x |= CS_bit; //x |= 0x01; //CS 1
                parPort.SendByte(x);
                delay_us(periode); // >450ns TCKL
                val = parPort.GetByte();
            }

            //stand state
            x = 0;
            x |= CS_bit; //x |= 0x01; //CS 1
            x |= 0x08; //SCK 1
            parPort.SendByte(x);
            delay_us(periode); // >450ns TCKH

            //falling state
            if (Sampling_edge == EDGE.FALLING)
            {
                x = 0;
                x |= CS_bit; //x |= 0x01; //CS 1
                parPort.SendByte(x);
                delay_us(periode); // >450ns TCKL
                val = parPort.GetByte();
            }
            return val;
        }
        public void spi_bit_tx(byte bit)
        {
            byte x;

            //rising state
            if (Sampling_edge == EDGE.RISING)
            {
                x = 0;
                if (bit == 1) x = 0x02; //DI 1/0, SCK 0
                x |= CS_bit; //x |= 0x01; //CS 1
                parPort.SendByte(x);
                delay_us(periode); // >450ns TCKL
            }

            //stand state
            x = 0;
            if (bit == 1) x = 0x02; //DI 1/0
            x |= CS_bit; //x |= 0x01; //CS 1
            x |= 0x08; //SCK 1
            parPort.SendByte(x);
            delay_us(periode); // >450ns TCKH

            //falling state
            if (Sampling_edge == EDGE.FALLING)
            {
                x = 0;
                if (bit == 1) x = 0x02; //DI 1/0, SCK 0
                x |= CS_bit; //x |= 0x01; //CS 1
                parPort.SendByte(x);
                delay_us(periode); // >450ns TCKL
            }
        }

        public void spi_clr_cs()
        {
            int byteVal = 0;
            byteVal |= (CS_bit ^ 0x01);
            parPort.SendByte(byteVal);
            delay_us(periode); // >450 TCSH
        }

        
        public void delay_us(int tick) {
            timePerParse = Stopwatch.StartNew();
            while (timePerParse.ElapsedTicks < tick*10) //tick = 100ns (0.1us)
                Thread.SpinWait(1);
            timePerParse.Stop();
        }

    }
}
