using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace OETS.Shared
{
    /// <summary>
    /// Used to send and receive a packet that contains a 
    /// string (used to query the client list, to send error messages, etc.)
    /// </summary>
    public class ResponsePacket : BasePacket
    {
        #region private members
        protected string from;
        protected string to;
        #endregion

        #region properties
        public string From
        {
            get { return from; }
            set { from = value; }
        }

        public string To
        {
            get { return to; }
            set { to = value; }
        }

        #endregion

        #region constructor
        public ResponsePacket()
        { }

        /// <summary>
        /// Создания Ответного пакета
        /// </summary>
        /// <param name="from">от кого</param>
        /// <param name="to">кому</param>
        /// <param name="response">ответ</param>
        public ResponsePacket(string from, string to, string response) :
            base(response)
        {
            this.from = from;
            this.to = to;
        }
         #endregion
    }   // ResponsePacket
}
