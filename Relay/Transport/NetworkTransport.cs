using System;
using System.Net;
using JFramework.Udp;

namespace JFramework.Net
{
    public class NetworkTransport : Transport
    {
        public int maxUnit = 1200;
        public int timeout = 10000;
        public int sendBuffer = 1024 * 1024;
        public int receiveBuffer = 1024 * 1024;
        public uint sendSize = 1024;
        public uint receiveSize = 1024;
        public uint resend = 2;
        public uint interval = 10;
        private Setting setting;
        private Client client;
        private Server server;

        private void Awake()
        {
            Log.Info = Console.WriteLine;
            Log.Warn = Console.WriteLine;
            Log.Error = Console.Error.WriteLine;
            setting = new Setting(maxUnit, timeout, sendBuffer, receiveBuffer, sendSize, receiveSize, resend, interval);
            client = new Client(setting, ClientConnected, ClientDisconnected, ClientDataReceived);
            server = new Server(setting, ServerConnected, ServerDisconnected, ServerDataReceived);
            return;

            void ClientConnected() => OnClientConnected.Invoke();

            void ClientDataReceived(ArraySegment<byte> message, Udp.Channel channel) => OnClientReceive.Invoke(message, (Channel)channel);

            void ClientDisconnected() => OnClientDisconnected.Invoke();

            void ServerConnected(int clientId) => OnServerConnected.Invoke(clientId);

            void ServerDataReceived(int clientId, ArraySegment<byte> message, Udp.Channel channel) => OnServerReceive.Invoke(clientId, message, (Channel)channel);

            void ServerDisconnected(int clientId) => OnServerDisconnected.Invoke(clientId);
        }

        public void Start()
        {
            server.Connect(port);
        }

        public void Update()
        {
            server.EarlyUpdate();
            server.AfterUpdate();
        }

        public override void ClientConnect(Uri uri = null)
        {
            if (uri != null)
            {
                int newPort = uri.IsDefaultPort ? port : uri.Port;
                client.Connect(uri.Host, (ushort)newPort);
            }
            else
            {
                client.Connect(address, port);
            }
        }

        public override void ClientSend(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            client.Send(segment, (Udp.Channel)channel);
        }

        public override void ClientDisconnect() => client.Disconnect();

        public override void StartServer() => server.Connect(port);

        public override void ServerSend(int clientId, ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            server.Send(clientId, segment, (Udp.Channel)channel);
        }

        public override void ServerDisconnect(int clientId) => server.Disconnect(clientId);

        public override Uri GetServerUri()
        {
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = Dns.GetHostName(),
                Port = port
            };
            return builder.Uri;
        }

        public override int GetMaxPacketSize(Channel channel = Channel.Reliable)
        {
            return channel == Channel.Reliable ? Utility.ReliableSize(setting.maxUnit, receiveSize) : Utility.UnreliableSize(setting.maxUnit);
        }

        public override int UnreliableSize() => Utility.UnreliableSize(maxUnit);

        public override void StopServer() => server.StopServer();

        public override void ClientEarlyUpdate() => client.EarlyUpdate();

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate() => server.EarlyUpdate();

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}