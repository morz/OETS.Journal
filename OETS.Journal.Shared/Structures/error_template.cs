using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OETS.Shared.Structures
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct error_template
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 254)]
        public string ERRORMSG;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string ERRORCAT;

        public error_template(string msg, string category)
        {
            this.ERRORMSG = msg;
            this.ERRORCAT = category;
        }
    }
}
