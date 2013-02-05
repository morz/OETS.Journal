using System;
using System.Collections.Generic;
using System.Text;

namespace OETS.Shared
{
    public class SSEventArgs : TimedEventArgs
    {
        #region private members
        protected SSocket sSocket;
        protected object metadata;
        #endregion

        #region properties
        public SSocket SSocket
        {
            get { return sSocket; }
        }
        #endregion properties

        #region constructor
        public SSEventArgs()
            : base()
        { }

        public SSEventArgs(SSocket SSocket)
            : base()
        {
            this.sSocket = SSocket;
        }
        #endregion
    }
       // ChatEventArgs
}
