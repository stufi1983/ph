using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemoryProgrammer
{
    public interface BitbangSPI
    {
        void open();
        void close();
        void spi_init();
        void spi_set_cs();
        void spi_bit_tx(byte bit);
        byte spi_bit_rx();
        void spi_clr_cs();
        void delay_us(int tick);
        BIT Set_Active_CS { set; }
        BIT Set_Active_SCLK { set; }
        EDGE Set_Sampling_At { set; }

    }
    public enum BIT { LOW, HIGH }; //global, namespace (MemoryProgrammer) member
    public enum EDGE { RISING, FALLING};

}
