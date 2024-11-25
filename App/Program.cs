
using Infrastructure.Provaider;
using System.Net;
using System.Net.Sockets;
using NetMQ;
using System.Threading.Tasks;
using Core;
using Infrastructure.Persistence.Context;

class Program
{
    static async Task Main(string[] args)
    {
        const string serverAddress = "tcp://127.0.0.1:12000";
        IMessageSource source;
        if (args.Length == 0)
        {
            source = new NetMQMessageSource(serverAddress);

            var chat = new ChatServer(serverAddress, new ChatContext());
            await chat.Start();
        }
        else
        {
            var clientSource = new NetMQMessageSourceAdapter(new NetMQMessageSourceClient(serverAddress));
            var chat = new ChatClient(args[0], serverAddress, clientSource);
            await chat.Start();
        }
    }
}
