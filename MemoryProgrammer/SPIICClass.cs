using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemoryProgrammer
{
    public abstract class SPIICClass
    {
        protected BitbangSPI pSPI;

        public virtual void StartTransfer() 
        { 
            
                pSPI.open(); 
        }
        public virtual void StopTransfer() 
        { 
            pSPI.close(); 
        }
    }
}
