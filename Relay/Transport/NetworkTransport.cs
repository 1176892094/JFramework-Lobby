using System;
using System.Net;
using JFramework.Udp;

namespace JFramework.Net
{
    public class NetworkTransport : Transport
    {
        private bool noDelay = true;
        private bool congestion = true;
        private int resend = 2;
        private int timeout = 10000;
        private int maxTransmitUnit = 1200;
        private int sendBufferSize = 1024 * 1027 * 7;
        private int receiveBufferSize = 1024 * 1027 * 7;
        private uint sendPacketSize = 1024 * 4;
        private uint receivePacketSize = 1024 * 4;
        private uint interval = 10;
        private Setting setting;
        private Client client;
        private Server server;

        private void Awake()
        {
            Console.WriteLine("Awake");
            Log.Info = Console.WriteLine;
            Log.Warn = Console.WriteLine;
            Log.Error = Console.Error.WriteLine;
            setting = new Setting(sendBufferSize, receiveBufferSize, maxTransmitUnit, timeout, receivePacketSize, sendPacketSize, interval, resend, noDelay, congestion);
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
            return channel == Channel.Reliable ? Helper.ReliableSize(setting.maxTransferUnit, receivePacketSize) : Helper.UnreliableSize(setting.maxTransferUnit);
        }

        public override int UnreliableSize() => Helper.UnreliableSize(maxTransmitUnit);

        public override void StopServer() => server.StopServer();

        public override void ClientEarlyUpdate() => client.EarlyUpdate();

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate() => server.EarlyUpdate();

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}