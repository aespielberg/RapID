using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using smc;
using RFID;
using WiFi;

namespace rapid {
    class Program {
        static void Main(string[] args) {
            // smc.SMCProgram.SMCMain(args);
            WiFi.WifiTest.WifiMain(args);
            // RFID.RFIDTest.RFIDMain(args);
        }
    }
}
