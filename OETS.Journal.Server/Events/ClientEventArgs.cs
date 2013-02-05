using System;
using System.Collections.Generic;
using System.Text;
using OETS.Shared;

namespace OETS.Server
{
    public class ClientEventArgs : TimedEventArgs
    {
        #region private members
        private ClientManager clientManager;
        private object args;
        #endregion

        #region constructor
        public ClientEventArgs(ClientManager clientManager) : base()
        {
            this.clientManager = clientManager;
        }

        public ClientEventArgs(ClientManager clientManager, object args) : base()
        {
            this.clientManager = clientManager;
            this.args = args;
        }
        #endregion 

        #region properties
        public ClientManager ClientManager
        {
            get { return clientManager; }
        }

        public object Args
        {
            get { return args; }
        }
        #endregion
    }   // ClientEventArgs
}
