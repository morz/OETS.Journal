using System;
using System.Collections.Generic;
using System.Text;

namespace OETS.Shared
{
    public class TimedEventArgs : EventArgs
    {
        #region protected members
        protected DateTime eventTime;
        #endregion

        #region properties
        public DateTime EventTime
        {
            get { return eventTime; }
        } 
	    #endregion

        #region constructor
        public TimedEventArgs()
        {
            eventTime = DateTime.Now;
        }
        #endregion
    }   // TimedEventArgs
}
