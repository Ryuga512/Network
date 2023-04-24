using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PacketQueue
{
    //パケットのデータ定義
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

    //排他制御でdataをstream_bufferに書き込む
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

    //排他制御で引数のref byte[] bufferに内容を読みだす
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

            int data_size = Math.Min(size, info.size); //キューのサイズ以上のものは読みださないように制限
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

    //バッファのクリア
    public void Clear()
    {
        byte[] buffer = stream_buffer.GetBuffer();
        Array.Clear(buffer, 0, buffer.Length);

        stream_buffer.Position = 0;
        stream_buffer.SetLength(0);
    }
}