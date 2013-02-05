using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OETS.Shared.Structures;

namespace OETS.Shared
{
    public partial class PingPacket: BasePacket
    {
        public ping_template Data
        {
            get;
            set;
        }

        public PingPacket()
        {
        }

        public PingPacket(ping_template Data)
            : base(Data.PingRequest)
        {
            this.Data = Data;
        }
    }
}
