using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OETS.Shared.Structures;

namespace OETS.Shared
{
    public partial class ErrorPacket: BasePacket
    {
        public error_template Data { get; set; }

        public ErrorPacket()
        {
        }

        public ErrorPacket(string MSG, string CAT)
            : base(MSG)
        {
            Data = new error_template(MSG, CAT);
        }
    }
}
