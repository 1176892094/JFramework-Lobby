using System;
using JFramework.Udp;

namespace JFramework.Net
{
    internal class KcpTransport : Transport
    {
        public int maxUnit = 1200;
        public uint timeout = 10000;
        public uint interval = 10;
        public uint deadLink = 40;
        public uint fastResend = 2;
        public uint sendWindow = 1024 * 4;
        public uint receiveWindow = 1024 * 4;
        private Client client;
        private Server server;

        public override void Awake()
        {
            var setting = new Udp.Setting(maxUnit, timeout, interval, deadLink, fastResend, sendWindow, receiveWindow);
            client = new Client(setting, ClientConnect, ClientDisconnect, ClientError, ClientReceive);
            server = new Server(setting, ServerConnect, ServerDisconnect, ServerError, ServerReceive);

            void ClientConnect() => OnClientConnect.Invoke();

            void ClientDisconnect() => OnClientDisconnect.Invoke();

            void ClientError(int error, string message) => OnClientError?.Invoke(error, message);

            void ClientReceive(ArraySegment<byte> message, int channel) => OnClientReceive.Invoke(message, channel);

            void ServerConnect(int clientId) => OnServerConnect.Invoke(clientId);

            void ServerDisconnect(int clientId) => OnServerDisconnect.Invoke(clientId);

            void ServerError(int clientId, int error, string message) => OnServerError?.Invoke(clientId, error, message);

            void ServerReceive(int clientId, ArraySegment<byte> message, int channel) => OnServerReceive.Invoke(clientId, message, channel);
        }

        public override void Update()
        {
            server.EarlyUpdate();
            server.AfterUpdate();
        }

        public override int MessageSize(int channel) => channel == Channel.Reliable ? Common.ReliableSize(maxUnit, receiveWindow) : Common.UnreliableSize(maxUnit);

        public override void StartServer() => server.Connect(port);

        public override void StopServer() => server.StopServer();

        public override void StopClient(int clientId) => server.Disconnect(clientId);

        public override void SendToClient(int clientId, ArraySegment<byte> segment, int channel = Channel.Reliable) => server.Send(clientId, segment, channel);

        public override void StartClient() => client.Connect(address, port);

        public override void StartClient(Uri uri) => client.Connect(uri.Host, (ushort)(uri.IsDefaultPort ? port : uri.Port));

        public override void StopClient() => client.Disconnect();

        public override void SendToServer(ArraySegment<byte> segment, int channel = Channel.Reliable) => client.Send(segment, channel);

        public override void ClientEarlyUpdate() => client.EarlyUpdate();

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate() => server.EarlyUpdate();

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}