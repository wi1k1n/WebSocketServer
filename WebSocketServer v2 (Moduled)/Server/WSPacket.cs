using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer
{
    class WSPacket
    {
        public bool Fin { get; set; }
        public bool Rsv1 { get; set; }
        public bool Rsv2 { get; set; }
        public bool Rsv3 { get; set; }
        public byte Opcode { get; set; }
        public bool MaskFlag { get; set; }
        public UInt64 Length { get; set; }
        public byte[] Mask { get; set; }
        public byte[] Data { get; set; }
        public int HeadersLength { get; set; }

        public WSPacket ()
        {
        }
    }
}
