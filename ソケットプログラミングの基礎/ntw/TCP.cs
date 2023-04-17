using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;

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

        }

        public void StopServer()
        {

        }

        public bool Connect(string address, int port)
        {

        }

        public bool Disconnect()
        {

        }

        public int Send(byte[] data, int size)
        {

        }

        public int Receive(ref byte[] buffer, int size)
        {

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
        }
    }
}
