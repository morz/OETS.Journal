using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OETS.Shared.Structures
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct ping_template
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string PingRequest;

        public bool values;

        public ping_template(string pr, bool at)
        {
            this.PingRequest = pr;
            this.values = at;
        }

        public string ToString(string ip)
        {
            return (String.Format("({0}::[{1},{2}])", ip, PingRequest, values));
        }
    }
}
