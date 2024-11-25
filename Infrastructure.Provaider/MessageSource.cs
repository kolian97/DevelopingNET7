using App.Contract;
using System.Net;
using NetMQ;
using NetMQ.Sockets;
using System.Text.Json;
namespace Infrastructure.Provaider
{



    public interface IMessageSource
    {
        Task Send(Message message, IPEndPoint endpoint, CancellationToken cancellationToken);
        Task<ReceivResult> Receive(CancellationToken cancellationToken);
    }

    public class NetMQMessageSource : IMessageSource
    {
        private readonly string _address;
        private readonly NetMQSocket _socket;

        public NetMQMessageSource(string address)
        {
            _address = address;
            _socket = new ResponseSocket();
            _socket.Bind(_address);
        }

        public async Task Send(Message message, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            string jsonMessage = JsonSerializer.Serialize(message);
            await Task.Run(() => _socket.SendFrame(jsonMessage), cancellationToken);
        }

        public async Task<ReceivResult> Receive(CancellationToken cancellationToken)
        {
            string receivedMessage = await Task.Run(() => _socket.ReceiveFrameString(), cancellationToken);

            var message = JsonSerializer.Deserialize<Message>(receivedMessage);
            return new ReceivResult { Message = message };
        }
    }

    public interface IMessageSourceClient
    {
        Task Send(Message message, CancellationToken cancellationToken);
        Task<ReceivResult> Receive(CancellationToken cancellationToken);
    }

    public class NetMQMessageSourceClient : IMessageSourceClient
    {
        private readonly string _address;
        private readonly RequestSocket _socket;

        public NetMQMessageSourceClient(string address)
        {
            _address = address;
            _socket = new RequestSocket();
            _socket.Connect(_address);
        }

        public async Task Send(Message message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            string jsonMessage = JsonSerializer.Serialize(message);
            await Task.Run(() => _socket.SendFrame(jsonMessage), cancellationToken);
        }

        public async Task<ReceivResult> Receive(CancellationToken cancellationToken)
        {
            string receivedMessage = await Task.Run(() => _socket.ReceiveFrameString(), cancellationToken);

            var message = JsonSerializer.Deserialize<Message>(receivedMessage);
            return new ReceivResult { Message = message };
        }
    }
}

