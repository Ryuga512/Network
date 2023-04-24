/*
TCPライブラリ
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Console;



public class TCP
{
    public delegate void EventHandler(NetEventState state);

    protected Thread thread = null;
    protected bool thread_loop = false;

    private static int MTU = 1024;
    private bool running_server = false;
    private bool connecting = false;
    private Socket listener = null;
    private Socket socket = null;
    private PacketQueue send_queue = new PacketQueue();     //送信バッファ
    private PacketQueue receive_queue = new PacketQueue();  //受信バッファ
    private EventHandler event_handler;

    //イベントハンドラの登録するメソッド
    public void RegisterEventHandler(EventHandler handler)
    {
        event_handler += handler;
    }

    //イベントハンドラの解除するメソッド
    public void UnregisterEventHandler(EventHandler handler)
    {
        event_handler -= handler;
    }



    //サーバーを開始するためのメソッド
    public bool StartServer(int port, int connection_num)
    {
        try
        {
            //サーバー側のソケットを作成し、指定されたポート番号で待機
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Listen(connection_num);
        }
        catch
        {
            return false;
        }
        running_server = true;

        return LaunchThread();
    }

    //サーバーを停止するメソッド
    public void StopServer()
    {
        thread_loop = false;
        if(thread != null)
        {
            thread.Join();
            thread = null;
        }

        Disconnect();

        if (listener != null)
        {
            listener.Close();
            listener = null;
        }
        running_server = false;
    }

    //通信接続するメソッド
    public bool Connect(string address, int port)
    {
        //リスナーが既に存在しているときは何もしない
        if(listener != null)
        {
            return false;
        }

        bool ret = false;

        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.Connect(address, port);
            //socket.SendBufferSize = 0;
            ret = LaunchThread();
        }
        catch(SocketException e)
        {
            socket = null;
        }

        if(ret == true)
        {
            connecting = true;
        }
        else
        {
            connecting = false;
        }

        //イベントハンドラが存在すれば接続イベントを発生させる
        if (event_handler != null)
        {
            NetEventState state = new NetEventState();
            state.type = NetEventType.Connect;
            state.result = (connecting == true) ? NetEventResult.Success : NetEventResult.Failure;
            event_handler(state);
        }

        return connecting;
    }

    //通信切断するメソッド
    public void Disconnect()
    {
        connecting = false;

        //ソケットを閉じる処理
        if (socket != null)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
            catch (SocketException e)
            {
                //
            }
        }

        if (event_handler != null)
        {
            NetEventState state = new NetEventState();
            state.type = NetEventType.Disconnect;
            state.result = NetEventResult.Success;
            event_handler(state);
        }

    }

    //送信するデータを格納するためのメソッド
    public int Send(byte[] data, int size)
    {
        if(send_queue == null)
        {
            return 0;
        }
        return send_queue.Enqueue(data, size);
    }

    //受信したデータを取り出すためのメソッド
    public int Receive(ref byte[] buffer, int size)
    {
        if(receive_queue == null)
        {
            return 0;
        }
        return receive_queue.Dequeue(ref buffer, size);
    }

    //クライアントからの接続を受け付けるメソッド
    void AcceptClient()
    {
        if (listener != null && listener.Poll(0, SelectMode.SelectRead))
        {
            socket = listener.Accept();
            connecting = true;
            if (event_handler != null)
            {
                NetEventState state = new NetEventState();
                state.type = NetEventType.Connect;
                state.result = NetEventResult.Success;
                event_handler(state);
            }
        }
    }


    //送信キューを用いてソケットからデータを送信するメソッド
    void DispatchSend()
    {
        try
        {
            if (socket.Poll(0, SelectMode.SelectWrite))
            {
                byte[] buffer = new byte[MTU];
                int send_size = send_queue.Dequeue(ref buffer, buffer.Length);
                while (send_size > 0)
                {
                    socket.Send(buffer, send_size, SocketFlags.None);
                    send_size = send_queue.Dequeue(ref buffer, buffer.Length);
                }
            }
        } 
        catch 
        {
            return;    
        }
    }

    //ソケットから受信キューにデータを受信するメソッド
    void DispatchReceive()
    {
        while (socket.Poll(0, SelectMode.SelectRead))
        {
            byte[] buffer = new byte[MTU];
            int receive_size = socket.Receive(buffer, buffer.Length, SocketFlags.None);
            if (receive_size == 0)
            {
                Disconnect();
            }
            else if (receive_size > 0)
            {
                receive_queue.Enqueue(buffer, receive_size);
            }
        }
    }

    //ループによって定期的に受信と送信を行うメソッド
    public void Dispatch()
    {
        while (thread_loop)
        {
            AcceptClient();

            if (socket != null && connecting == true)
            {
                DispatchSend();
                DispatchReceive();
            }
            Thread.Sleep(5);
        }
    }

    //別スレッドでDispatch関数を実行するメソッド
    bool LaunchThread()
    {
        try
        {
            thread_loop = true;
            thread = new Thread(new ThreadStart(Dispatch));
            thread.Start();
        }
        catch
        {
            return false;
        }

        return true;
    }

}

