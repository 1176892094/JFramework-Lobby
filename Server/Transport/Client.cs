using System;
using System.Net;
using System.Net.Sockets;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace JFramework.Udp
{
    public sealed class Client : Proxy
    {
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private event Action OnConnect;
        private event Action OnDisconnect;
        private event Action<ArraySegment<byte>, int> OnReceive;

        public Client(Setting setting, Action OnConnect, Action OnDisconnect, Action<ArraySegment<byte>, int> OnReceive) : base(setting, 0)
        {
            this.setting = setting;
            this.OnConnect = OnConnect;
            this.OnReceive = OnReceive;
            this.OnDisconnect = OnDisconnect;
            buffer = new byte[setting.MaxUnit];
        }

        public void Connect(string address, ushort port)
        {
            try
            {
                if (state != State.Disconnect)
                {
                    Log.Warn("客户端已经连接！");
                    return;
                }

                var addresses = Dns.GetHostAddresses(address);
                if (addresses.Length >= 1)
                {
                    Reset(setting);
                    state = State.Connect;
                    endPoint = new IPEndPoint(addresses[0], port);
                    socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    Common.SetBuffer(socket);
                    socket.Connect(endPoint);
                    Log.Info($"客户端连接到：{addresses[0]} 端口：{port}。");
                    SendReliable(ReliableHeader.Connect, default);
                }
            }
            catch (SocketException e)
            {
                Log.Error($"无法解析主机地址：{address}\n{e}");
                OnDisconnect?.Invoke();
            }
        }


        private bool TryReceive(out ArraySegment<byte> segment)
        {
            segment = default;
            if (socket == null) return false;
            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead)) return false;
                int size = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                segment = new ArraySegment<byte>(buffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    return false;
                }

                Log.Info($"客户端接收消息失败！\n{e}");
                Disconnect();
                return false;
            }
        }

        public void Send(ArraySegment<byte> segment, int channel)
        {
            if (state == State.Disconnect)
            {
                Log.Warn("客户端没有连接，发送消息失败！");
                return;
            }

            SendData(segment, channel);
        }


        public void Input(ArraySegment<byte> segment)
        {
            if (segment.Count <= 1 + 4)
            {
                return;
            }

            var channel = segment.Array[segment.Offset];
            Utility.Decode32U(segment.Array, segment.Offset + 1, out var newCookie);
            if (newCookie == 0)
            {
                Log.Error($"网络代理丢弃了无效的签名缓存。旧：{cookie} 新：{newCookie}");
            }

            if (cookie == 0)
            {
                cookie = newCookie;
            }
            else if (cookie != newCookie)
            {
                Log.Info($"从 {endPoint} 删除无效cookie: {newCookie}预期:{cookie}。");
                return;
            }

            Input(channel, new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4));
        }

        protected override void Connected()
        {
            Log.Info("客户端连接成功。");
            OnConnect?.Invoke();
        }

        protected override void Send(ArraySegment<byte> segment)
        {
            if (socket == null) return;
            try
            {
                if (socket.Poll(0, SelectMode.SelectWrite))
                {
                    socket.Send(segment.Array, segment.Offset, segment.Count, SocketFlags.None);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    return;
                }

                Log.Error($"客户端发送消息失败！\n{e}");
            }
        }

        protected override void Receive(ArraySegment<byte> message, int channel)
        {
            OnReceive?.Invoke(message, channel);
        }

        protected override void Disconnected()
        {
            Log.Info($"客户端断开连接。");
            OnDisconnect?.Invoke();
            endPoint = null;
            socket?.Close();
            socket = null;
        }

        public override void EarlyUpdate()
        {
            if (state == State.Disconnect)
            {
                return;
            }

            while (TryReceive(out var segment))
            {
                Input(segment);
            }

            base.EarlyUpdate();
        }

        public override void AfterUpdate()
        {
            if (state == State.Disconnect)
            {
                return;
            }

            base.AfterUpdate();
        }
    }
}