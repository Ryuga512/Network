using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;

namespace ntw
{

    namespace ntw
    {
        public class TCP
        {
            private Socket listener;
            private bool running_server;
            private Socket socket;
            private bool connecting;
            private PacketQueue send_queue;
            private PacketQueue receive_queue;

            public bool StartServer(int port, int connection_num)
            {
                return true;
            }

            public void StopServer()
            {
                return;
            }

            public bool Connect(string address, int port)
            {
                return true;
            }

            public bool Disconnect()
            {
                return true;
            }

            public int Send(byte[] data, int size)
            {
                return size;
            }

            public int Receive(ref byte[] buffer, int size)
            {
                return size;
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
                if(offset_list.Count <= 0)
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

                    if(offset_list.Count == 0)
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

}
