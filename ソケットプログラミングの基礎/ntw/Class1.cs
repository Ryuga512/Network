using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Console;

namespace ntw
{
    public enum NetEventType
    {
        Connect = 0,
        Disconnect,
        SendError,
        ReceiveError,
    }

    public enum NetEventResult
    {
        Failure = -1,
        Success = 0,
    }

    public class NetEventState
    {
        public NetEventType type;
        public NetEventResult result;
    }

    public delegate void EventHandler(NetEventState state);
    public class TCP
    {
        Thread thread;
        private Socket listener;
        private Socket socket;
        private bool thread_loop;
        private bool running_server;
        private bool connecting;
        private bool starting;
        private PacketQueue send_queue = new PacketQueue();     //送信バッファ
        private PacketQueue receive_queue = new PacketQueue();  //受信バッファ
        private EventHandler event_handler;

        public void RegisterEventHandler(EventHandler handler)
        {
            event_handler += handler;
        }

        public void UnregisterEventHandler(EventHandler handler)
        {
            event_handler -= handler;
        }

        void AcceptClient()
        {
            if(listener != null && listener.Poll(0, SelectMode.SelectRead))
            {
                socket = listener.Accept();
                connecting = true;
                if(event_handler != null)
                {
                    NetEventState state = new NetEventState();
                    state.type = NetEventType.Connect;
                    state.result = NetEventResult.Success;
                    event_handler(state);
                }
            }
        }
        public bool StartServer(int port, int connection_num)
        {
            listener = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Listen(connection_num);
            running_server = true;

            return true;
        }

        public void StopServer()
        {
            listener.Close();
            listener = null;
            running_server = false;
        }

        public bool Connect(string address, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.Connect(address, port);
            socket.SendBufferSize = 0;
            connecting = true;

            return true;
        }

        public bool Disconnect()
        {
            connecting = false;

            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }

            if(event_handler != null)
            {
                NetEventState state = new NetEventState();
                state.type = NetEventType.Disconnect;
                state.result = NetEventResult.Success;
                event_handler(state);
            }

            return true;
        }

        public int Send(byte[] data, int size)
        {
            return send_queue.Enqueue(data,size);
        }

        public int Receive(ref byte[] buffer, int size)
        {
            return receive_queue.Dequeue(ref buffer, size);
        }

        public void Dispatch()
        {
            while(thread_loop)
            {
                AcceptClient();

                if(socket != null && connecting == true)
                {
                    DispatchSend();
                    DispatchReceive();
                }
                Thread.Sleep(5);
            }
        }

        void DispatchSend()
        {
            if(socket.Poll(0, SelectMode.SelectWrite))
            {
                byte[] buffer = new byte[4096];
                int send_size = send_queue.Dequeue(ref buffer, buffer.Length);
                while(send_size > 0)
                {
                    socket.Send(buffer, send_size, SocketFlags.None);
                    send_size = send_queue.Dequeue(ref buffer, buffer.Length);
                }
            }
        }

        void DispatchReceive()
        {
            while(socket.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[4096];
                int receive_size = socket.Receive(buffer, buffer.Length,SocketFlags.None);
                if(receive_size > 0)
                {
                    Disconnect();
                }
                else if(receive_size > 0)
                {
                    receive_queue.Enqueue(buffer, receive_size);
                }
            }
        }

        bool LaunchThread()
        {
            try
            {
                thread = new Thread(new ThreadStart(Dispatch));
                thread.Start();
            }
            catch
            {
                return false;
            }

            starting = true;

            return true;
        }
    }

    public class PacketQueue
    {
        struct PacketInfo
        {
            public int offset;
            public int size;
        };

        private MemoryStream stream_buffer;
        private List<PacketInfo> offset_list;
        private int offset = 0;
        private Object lock_object = new Object();

        public PacketQueue()
        {
            stream_buffer = new MemoryStream();
            offset_list = new List<PacketInfo>();
        }

        public int Enqueue(byte[] data, int size)
        {
            PacketInfo info = new PacketInfo();

            info.offset = offset;
            info.size = size;

            lock (lock_object)
            {
                offset_list.Add(info);

                stream_buffer.Position = offset;
                stream_buffer.Write(data, 0, size);
                stream_buffer.Flush();
                offset += size;
            }

            return size;
        }

        public int Dequeue(ref byte[] buffer, int size)
        {
            if (offset_list.Count <= 0)
            {
                return -1;
            }

            int receive_size = 0;
            lock (lock_object)
            {
                PacketInfo info = offset_list[0];

                int data_size = Math.Min(size, info.size);
                stream_buffer.Position = info.offset;
                receive_size = stream_buffer.Read(buffer, 0, data_size);

                if (receive_size > 0)
                {
                    offset_list.RemoveAt(0);
                }

                if (offset_list.Count == 0)
                {
                    Clear();
                    offset = 0;
                }

                return receive_size;
            }
        }

        public void Clear()
        {
            byte[] buffer = stream_buffer.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);

            stream_buffer.Position = 0;
            stream_buffer.SetLength(0);
        }
    }


}

