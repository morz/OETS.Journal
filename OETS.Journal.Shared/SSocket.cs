using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using OETS.Shared.Opcodes;
using OETS.Shared.Util;

namespace OETS.Shared
{
    /// <summary>
    /// Wraps a socket and the data that is sent.
    /// This is common for both server and client application, both 
    /// should be using this class for sending or receiving data.
    /// </summary>
    public class SSocket : IDisposable
    {
        public event EventHandler Sent;
        public event EventHandler Received;
        public event EventHandler Disconnected;

        #region private members
        private Socket socket;
        private IPAddress target = IPAddress.None;
        private OpcoDes command;                // a command to be sent
        private string metatype;                    // will contain the metadata type for reflection
        private object metadata;
        private ulong sentBytes;
        private ulong receivedBytes;
        #endregion

        #region properties
        public bool Connected
        {
            get
            {
                if (socket != null && socket.Connected) return true;
                else return false;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (socket != null && socket.Connected) return (IPEndPoint)socket.RemoteEndPoint;
                else return null;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (socket != null && socket.Connected) return (IPEndPoint)socket.LocalEndPoint;
                else return null;
            }
        }

        public IPAddress Target
        {
            get { return target; }
            set { target = value; }
        }

        public OpcoDes Command
        {
            get { return command; }
            set { command = value; }
        }

        public string Metatype
        {
            get { return metatype; }
            set { metatype = value; }
        }

        public object Metadata
        {
            get { return metadata; }
            set { metadata = value; }
        }

        public ulong SentBytes
        {
            get { return sentBytes; }
        }

        public ulong ReceivedBytes
        {
            get { return receivedBytes; }
        }
        #endregion

        #region IDisposable Members
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (socket != null)
                {
                    if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // remove from finalization queue
        }
        #endregion

        #region constructors
        public SSocket(
            ref Socket socket,
            IPAddress target, OpcoDes command, 
            string metatype, object metadata) 
            : this(ref socket)
        {
            this.target = target;
            this.command = command;
            this.metatype = metatype;
            this.metadata = metadata;
        }

        public SSocket(ref Socket socket)
        {
            this.socket = socket;
        }
        #endregion

        #region copy constructor
        /// <summary>
        /// Deep clone of a ChatSocket object without the network stream cloned.
        /// It is used when a new ChatSocket object is passed on to methods, events that 
        /// do not need a network stream.
        /// </summary>
        public SSocket(SSocket cp) 
        {
            this.target = cp.Target;
            this.command = cp.Command;
            this.metatype = cp.Metatype;
            this.metadata = cp.Metadata;
            this.sentBytes = cp.SentBytes;
            this.receivedBytes = cp.ReceivedBytes;
        }
        #endregion

        #region Send

        public virtual void Send()
        {
            if (socket == null)
                return;
            try
            {
                var commandData = BitConverter.GetBytes((int)command);

                /*
                byte[] ipLen = new byte[4];
                byte[] ipBuffer = Encoding.Unicode.GetBytes(target.ToString());
                ipLen = BitConverter.GetBytes((int)ipBuffer.Length);
                full_packet_size += ipLen.Length + ipBuffer.Length;
                */
                var typeData = Encoding.Unicode.GetBytes(metatype);
                var typeLen = BitConverter.GetBytes(typeData.Length);

                Type metaType = metadata.GetType();
                MethodInfo mi = metaType.GetMethod("GetBytes");
                var metaData = (byte[])mi.Invoke(metadata, null);
                var metaLen = BitConverter.GetBytes(metaData.Length);

                byte[] buffer = new byte[commandData.Length +
                    typeLen.Length + typeData.Length + metaLen.Length + metaData.Length+1];

                System.Buffer.BlockCopy(commandData, 0, buffer, 0, commandData.Length);
                int offset = commandData.Length;
                /*
                System.Buffer.BlockCopy(ipLen, 0, buffer, offset, ipLen.Length);
                offset += ipLen.Length;
                System.Buffer.BlockCopy(ipBuffer, 0, buffer, offset, ipBuffer.Length);
                offset += ipBuffer.Length;
                */
                System.Buffer.BlockCopy(typeLen, 0, buffer, offset, typeLen.Length);
                offset += typeLen.Length;
                System.Buffer.BlockCopy(typeData, 0, buffer, offset, typeData.Length);
                offset += typeData.Length;

                System.Buffer.BlockCopy(metaLen, 0, buffer, offset, metaLen.Length);
                offset += metaLen.Length;
                System.Buffer.BlockCopy(metaData, 0, buffer, offset, metaData.Length);

                var args = SocketHelpers.AcquireSocketArg();
                if (args != null)
                {
                    //Trace.Write(string.Format("SSocket SEND {0} -> {1} {2}",
                    //socket.LocalEndPoint.ToString(), socket.RemoteEndPoint.ToString(), command.ToString()));

                    args.Completed += SendAsyncComplete;
                    args.SetBuffer(buffer, 0, buffer.Length);
                    args.UserToken = this;
                    socket.SendAsync(args);

                    sentBytes = (ulong)buffer.Length;

                    if (Sent != null)
                        Sent(this, EventArgs.Empty);
                }
            }
            catch (Exception exc)
            {
                Trace.WriteLine("[OETS.Shared] SEND " + exc.Message);
                this.Close();
                if (SockUtils.HandleSocketError(exc))
                {
                    if (Disconnected != null)
                        Disconnected(this, EventArgs.Empty);
                }
                else
                {
                    throw;
                }
            }
        }
        private static void SendAsyncComplete(object sender, SocketAsyncEventArgs args)
        {
            args.Completed -= SendAsyncComplete;
            SocketHelpers.ReleaseSocketArg(args);
        }
        #endregion

        #region Receive
        
        /// <summary>
        /// Size For packets
        /// </summary>
        public const int BufferSize = 3072;

        private byte[] _bufferSegment = null;

        public void Receive()
        {
            if (socket == null)
                return;
            try
            {
                //if (_bufferSegment == null)
                    _bufferSegment = new byte[BufferSize];

                var socketArgs = SocketHelpers.AcquireSocketArg();
                var i = socketArgs.Count;
                socketArgs.SetBuffer(_bufferSegment, 0, _bufferSegment.Length);
                socketArgs.UserToken = this;                
                socketArgs.Completed += ReceiveAsyncComplete;

                var willRaiseEvent = socket.ReceiveAsync(socketArgs);
                if (!willRaiseEvent)
                {
                    ProcessRecieve(socketArgs);
                }
            }
            catch (Exception exc)
            {
                Trace.WriteLine("[OETS.Shared] Receive " + exc.Message);

                this.Close();
                if (SockUtils.HandleSocketError(exc))
                {
                    if (Disconnected != null)
                        Disconnected(this, EventArgs.Empty);
                }
                else
                {
                    throw exc;
                }
            }
        }

        private void ProcessRecieve(SocketAsyncEventArgs args)
        {
            try
            {
                var bytesReceived = args.BytesTransferred;

                if (bytesReceived == 0)
                {
                    Trace.WriteLine("[OETS.Shared] SSocket ProcessRecieve bytesReceived = 0");

                    this.Close();
                    if (Disconnected != null)
                        Disconnected(this, EventArgs.Empty);
                }
                else
                {
                    int offset = 4;
                    //command
                    int bufferVal = BitConverter.ToInt32(_bufferSegment, 0);

                    /*/ IP
                    byte[] b1 = new byte[4];
                    System.Buffer.BlockCopy(_bufferSegment, offset, b1, 0, 4);
                    offset += b1.Length;
                    int size = BitConverter.ToInt32(b1, 0);                    
                    byte[] ipBuffer = new byte[size];
                    System.Buffer.BlockCopy(_bufferSegment, offset, ipBuffer, 0, size);
                    offset += size;
                    */
                    // metaType
                    byte[] b2 = new byte[4];
                    System.Buffer.BlockCopy(_bufferSegment, offset, b2, 0, 4);
                    offset += b2.Length;
                    int size = BitConverter.ToInt32(b2, 0);
                    byte[] typeBuffer = new byte[size];
                    System.Buffer.BlockCopy(_bufferSegment, offset, typeBuffer, 0, size);
                    offset += size;

                    // metadata
                    byte[] b3 = new byte[4];
                    System.Buffer.BlockCopy(_bufferSegment, offset, b3, 0, 4);
                    offset += b3.Length;
                    size = BitConverter.ToInt32(b3, 0);
                    byte[] metaBuffer = new byte[size];
                    System.Buffer.BlockCopy(_bufferSegment, offset, metaBuffer, 0, size);
                    offset += size;

                    System.Array.Clear(_bufferSegment, 0, _bufferSegment.Length);

                    // check if it is a know command 
                    if (Enum.IsDefined(typeof(OpcoDes), bufferVal))
                    {
                        command = (OpcoDes)bufferVal;

                       // if (!IPAddress.TryParse(Encoding.Unicode.GetString(ipBuffer), out target))
                       //     target = IPAddress.Parse("127.0.0.1");

                       // Trace.Write(string.Format("SSocket RECIEVE {0} <- {1} {2}",
                    //socket.LocalEndPoint.ToString(), socket.RemoteEndPoint.ToString(), command.ToString()));

                        metatype = Encoding.Unicode.GetString(typeBuffer);

                        if (String.IsNullOrEmpty(metatype))
                        {
                            Trace.Write("Ошибка!!!!!! Тип не найден! [" + metatype + "]. Команда - " + command + ". " +
                                " Размер metaBuffer:" + metaBuffer.Length + ", typeBuffer:" + typeBuffer.Length + ",bufferVal:" + bufferVal);
                        }

                        Type typeToCreate = Type.GetType(metatype);
                        IBasePacket packet = (IBasePacket)Activator.CreateInstance(typeToCreate);

                        packet.Initialize(metaBuffer);
                        metadata = packet;

                        if (Received != null)
                            Received(this, EventArgs.Empty);
                    }
                    
                    receivedBytes = (uint)bytesReceived;

                    Receive();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[OETS.Shared] ProcessRecieve " + ex.Message);
                Trace.WriteLine("[OETS.Shared] ProcessRecieve " + ex.StackTrace);
                this.Close();
                if (Disconnected != null)
                    Disconnected(this, EventArgs.Empty);
            }
            finally
            {                
                args.Completed -= ReceiveAsyncComplete;
                SocketHelpers.ReleaseSocketArg(args);
            }
        }

        private void ReceiveAsyncComplete(object sender, SocketAsyncEventArgs args)
        {
            ProcessRecieve(args);
        }

        #endregion

        #region Close
        public void Close()
        {
            if (socket != null)
            {
                if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            socket = null;
        }
        #endregion 

        #region Disconnect
        private void Disconnect()
        {
            Trace.WriteLine("[OETS.Shared] SSocket Disconnected");
            this.Close();
            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }
        #endregion
    }
}
