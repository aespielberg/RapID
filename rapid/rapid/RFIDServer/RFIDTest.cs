using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID
{
    static class RFIDTest
    {
        public static int RFIDMain(string[] args)
        {
            RFIDReaderThread thread = new RFIDReaderThread(1,null,null,false);
            thread.run();
            return 0;
        }
    }
}
