using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AEEProg.ProgrammerData;

namespace AEEProg
{
    internal class Programmer
    {
        bool chipErase;
        bool verify;
        bool write;
        string memtype;
        string operation;
        string fileType;
        int maxAddress;

        private SerialPort port;
        public Programmer() {


            this.port = new()
            {
                BaudRate = 74880,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = false,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
            };
        }
    }
}
