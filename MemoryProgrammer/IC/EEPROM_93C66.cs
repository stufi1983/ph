using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemoryProgrammer.IC
{
    public class EEPROM_93C66 : SPIICClass
    {
 
        private Form1 ancessorClass;
        public EEPROM_93C66(string portName, int speed, Form1 ancessorClass)
        {
            pSPI = (BitbangSPI)new SerialSPI(portName, speed);
            init_signal(ancessorClass);
        }

        public EEPROM_93C66(int portAddress, int speed, Form1 ancessorClass)
        {
            pSPI = (BitbangSPI)new ParalelSPI(portAddress, speed);
            init_signal(ancessorClass);
        }
        private void init_signal(Form1 ancessorClass)
        {
            pSPI.Set_Active_CS = BIT.HIGH; //CS active at HIGH
            pSPI.Set_Active_SCLK = BIT.HIGH; //active at HIGH (data allow change at LOW) 
            pSPI.Set_Sampling_At = EDGE.RISING; //sampled at rising edge clock
            pSPI.spi_init(); //set CS & SCLK at idle bit setup
            this.ancessorClass = ancessorClass;
        }
        
        public void EEPROM_93C66_spi_ewen()
        {
            pSPI.spi_set_cs();
            pSPI.spi_bit_tx(1);
            pSPI.spi_bit_tx(0);
            pSPI.spi_bit_tx(0);

            for (byte i = 0; i <= 7; i++)
            {
                pSPI.spi_bit_tx(1);
            }
            pSPI.spi_bit_tx(0); //dummy
            pSPI.spi_clr_cs();
            pSPI.delay_us(12 * 10); //TCSL + 12cycles
        }
        public void EEPROM_93C66_spi_send_data(int memAddress, int hlbytes)
        {
            int hbytes = (hlbytes >> 8) & 0xFF;            
            int lbytes = hlbytes  & 0xFF;
            EEPROM_93C66_spi_send_data(memAddress, hbytes, lbytes);
        }
        public UInt32 EEPROM_93C66_spi_read_data(int memAddress)
        {
            //TODO
            pSPI.spi_set_cs();
            pSPI.spi_bit_tx(1);
            pSPI.spi_bit_tx(1);
            pSPI.spi_bit_tx(0);

            for (int i = 7; i >= 0; i--) // byte 00 - 01 = ff
                pSPI.spi_bit_tx((byte)((memAddress >> i) & 1));

            UInt32 rx = 0;
            for (int i = 15; i >= 0; i--)
            {
                rx <<= 1;
                rx |= pSPI.spi_bit_rx();
            }


            pSPI.spi_clr_cs();

            return rx;
        }
        public void EEPROM_93C66_spi_send_data(int memAddress, int hbytes, int lbytes)
        {
            pSPI.spi_set_cs();
            pSPI.spi_bit_tx(1);
            pSPI.spi_bit_tx(0);
            pSPI.spi_bit_tx(1);

            for (int i = 7; i >= 0; i--) // byte 00 - 01 = ff
                pSPI.spi_bit_tx((byte)((memAddress >> i) & 1));
            for (int i = 7; i >= 0; i--)
                pSPI.spi_bit_tx((byte)((hbytes >> i) & 1));
            for (int i = 7; i >= 0; i--)
                pSPI.spi_bit_tx((byte)((lbytes >> i) & 1));

            pSPI.spi_clr_cs();

            pSPI.spi_bit_tx(0); //dummy 1 cycle

            for (int j = 0; j < 3; j++)//wait for 9*3 clock cycles
            {
                pSPI.spi_set_cs();
                for (int i = 0; i < 9; i++)
                    pSPI.spi_bit_tx(0);
                pSPI.spi_clr_cs();
            }
        }
        
        public void EEPROM_93C66_writeAll(ref string[] split)
        {
            EEPROM_93C66_spi_ewen();
            int address = -1;
            for (int i = 0; i < split.Length - 1; i += 2)
            {
                address++;
                int hbytes = Convert.ToInt32(split[i], 16);
                int lbytes = Convert.ToInt32(split[i + 1], 16);

                EEPROM_93C66_spi_send_data(address, hbytes, lbytes);

                ancessorClass.progress = (int)(i * 100 / split.Length);
            }
            ancessorClass.progress = 100;
        }
        
    }
}
