using System;
using System.Collections.Generic;
using System.Text;

namespace OETS.Shared
{
    interface IBasePacket
    {
        /// <summary>
        /// Initialize the class members using the byte array passed in as parameter.
        /// </summary>
        void Initialize(byte[] metadata);

        /// <summary>
        /// Return metadata (a byte array) constructed from the members of the class.
        /// </summary>
        byte[] GetBytes();
    }   // IBasePacket
}
