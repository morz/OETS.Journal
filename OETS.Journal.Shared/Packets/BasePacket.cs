using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OETS.Shared
{
    /// <summary>
    /// A simple packet, contains only a string response.
    /// </summary>
    public class BasePacket : IBasePacket
    {
        #region private members
        protected string response;       
        #endregion

        #region properties
        public string Response
        {
            get { return response; }
            set { response = value; }
        }

        #endregion

        #region constructor
        public BasePacket()
        { }

        public BasePacket(string response)
        {
            this.response = response;
        }
        #endregion

        #region protected ResizeDataBuffer
        protected static void ResizeDataBuffer(ref byte[] data, int ptr, int sizeNeeded)
        {
            int total = ptr + sizeNeeded;
            while (total >= data.Length)
                Array.Resize<byte>(ref data, data.Length * 2);
        }
        #endregion

        #region Initialize
        /// <summary>
        /// Using the data byte array initialize the data members of a packet.
        /// </summary>
        public void Initialize(byte[] data)
        {
            Type type = this.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            int ptr = 0;
            int size = 0;

            try
            {
                for (int i = 0; i < fields.Length; ++i)
                {
                    FieldInfo fi = fields[i];

                    // get size of the next member from the byte array
                    size = BitConverter.ToInt32(data, ptr);
                    ptr += 4;

                    if (fi.FieldType == typeof(System.String))
                    {
                        fi.SetValue(this, Encoding.Unicode.GetString(data, ptr, size));
                        ptr += size;
                    }
                    else if (fi.FieldType == typeof(OETS.Shared.Structures.ping_template)
                        || fi.FieldType == typeof(OETS.Shared.Structures.error_template)
                        )
                    {
                        byte[] rawdatas = new byte[size];
                        Buffer.BlockCopy(data, ptr, rawdatas, 0, size);
                        object l = Byte2Struct(rawdatas, fi.FieldType);
                        fi.SetValue(this, l);
                        ptr += size;
                    }
                    else
                    {

                        ptr += size;
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.Write(exc.Message + " - " + exc.StackTrace);
            }
        }
        #endregion

        #region GetBytes
        /// <summary>
        /// Return a byte array of the data members of a packet.
        /// The order of the data members stored in the byte array is 
        /// the order of declaration.
        /// </summary>
        public byte[] GetBytes()
        {
            byte[] data = new byte[4];
            int ptr = 0;
            int size = 0;

            Type type = this.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            try
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo fi = fields[i];

                    if (fi.FieldType == typeof(System.String))
                    {
                        // get the string value of the field
                        string strField = (string)fi.GetValue(this);

                        // get size of from string, copy size into array
                        size = Encoding.Unicode.GetByteCount(strField);
                        BasePacket.ResizeDataBuffer(ref data, ptr, size);
                        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, data, ptr, 4);
                        ptr += 4;   // GetBytes returns the size as an array of 4 bytes

                        // copy string value into array
                        BasePacket.ResizeDataBuffer(ref data, ptr, size);
                        Buffer.BlockCopy(Encoding.Unicode.GetBytes(strField), 0, data, ptr, size);
                        ptr += size;
                    }
                    if (fi.FieldType == typeof(OETS.Shared.Structures.ping_template)
                        || fi.FieldType == typeof(OETS.Shared.Structures.error_template)
                        )
                    {
                        object strField = fi.GetValue(this);
                        int rawsize = Marshal.SizeOf(strField);
                        byte[] rawdatas = new byte[rawsize];
                        byte[] l = Struct2Byte(strField);

                        BasePacket.ResizeDataBuffer(ref data, ptr, rawsize);
                        Buffer.BlockCopy(BitConverter.GetBytes(rawsize), 0, data, ptr, 4);
                        ptr += 4;

                        Buffer.BlockCopy(l, 0, data, ptr, rawsize);
                        ptr += rawsize;
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.Write(exc.Message + " - " + exc.StackTrace);
            }

            byte[] retData = new byte[ptr];
            Array.Copy(data, retData, ptr);

            return retData;
        }
        #endregion

        public static object Byte2Struct(byte[] rawdatas, Type anytype)
        {
            int rawsize = Marshal.SizeOf(anytype);
            if (rawsize > rawdatas.Length)
                return null;
            GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            object retobj = Marshal.PtrToStructure(buffer, anytype);
            handle.Free();
            return retobj;

        }

        public static byte[] Struct2Byte(object anything)
        {
            int rawsize = Marshal.SizeOf(anything);
            byte[] rawdatas = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdatas, GCHandleType.Pinned);
            IntPtr buffer = handle.AddrOfPinnedObject();
            Marshal.StructureToPtr(anything, buffer, false);
            handle.Free();
            return rawdatas;
        } 
    }   // BasePacket
}
