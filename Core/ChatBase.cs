using App.Contract;
using Infrastructure.Persistence.Context;
using Infrastructure.Provaider;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
namespace Core
{
    public class NetMQMessageSourceAdapter : IMessageSource
    {
        private readonly NetMQMessageSourceClient _client;

        public NetMQMessageSourceAdapter(NetMQMessageSourceClient client)
        {
            _client = client;
        }

        public async Task Send(Message message, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            await _client.Send(message, cancellationToken);
        }

        public async Task<ReceivResult> Receive(CancellationToken cancellationToken)
        {
            return await _client.Receive(cancellationToken);
        }
    }
    public abstract class ChatBase
    {
        protected CancellationTokenSource CancellationTokenSourse { get; set; } = new CancellationTokenSource();
        protected CancellationToken CancellationToken => CancellationTokenSourse.Token;
        protected abstract Task Listener();
        public abstract Task Start();
    }
    public  class ChatClient : ChatBase
    {
        private readonly User _user;
        private readonly IPEndPoint _serverEndpoint;
        private readonly IMessageSource _source;
        private IEnumerable<User> _users = [];
        private IPEndPoint? serverEndpoint;

        public  ChatClient(string username, string serverAddress, IMessageSource source)
        {
            _user = new User { Name =  username };
            _serverEndpoint = serverEndpoint;
            _source = source;
        }
        public override async Task Start()
        {
            var join = new Message {Text = _user.Name, Command = Command.Join};
            await _source.Send(join, _serverEndpoint, CancellationToken);
            Task.Run(Listener);

            while (!CancellationToken.IsCancellationRequested)
            {
                string input = (await Console.In.ReadLineAsync()) ?? string.Empty;
                Message message;
                if(input.Trim().Equals("/exit", StringComparison.CurrentCultureIgnoreCase))
                {
                     message = new() { SenderID = _user.Id, Command = Command.Exit };
                }
                else
                {
                     message = new() { Text = input, SenderID = _user.Id, Command = Command.None };
                }
               
                await _source.Send(message, _serverEndpoint, CancellationToken);
            }
        }
        protected override async Task Listener()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReceivResult result = await _source.Receive(CancellationToken);
                    if (result.Message is null)
                        throw new Exception("Message is null");

                    if(result.Message.Command == Command.Join)
                    {
                        JoinHandler(result.Message);
                    }
                    else if (result.Message.Command == Command.Users)
                    {
                        UsersHandler(result.Message);
                    }
                    else if (result.Message.Command == Command.None)
                    {
                        MessageHandler(result.Message);
                    }

                }
                catch(Exception ext)
                {
                    await Console.Out.WriteLineAsync(ext.Message);
                }
            }
        }

        private void MessageHandler(Message message)
        {
            Console.WriteLine($"{_users.First(u =>u.Id == message.SenderID)}:{message.Text}");
        }

        private void UsersHandler(Message message)
        {
            _users = message.Users;
        }

        private void JoinHandler(Message message)
        {
            _user.Id = message.RecepentId!.Value;
            Console.WriteLine("Join success");
        }
    }
    public class ChatServer : ChatBase
    {
        private readonly IMessageSource _source;
        private readonly ChatContext _context;
        private HashSet<User> _users = [];
        public ChatServer(string address, ChatContext context)
        {
            _source = new NetMQMessageSource(address);
            _context = context;
        }

        public override async Task Start()
        {
            await Task.CompletedTask;
            Task.Run(Listener);
        }

        protected override async Task Listener()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReceivResult result = (await _source.Receive(CancellationToken) ?? throw new Exception("Message is null"));
                    switch (result.Message!.Command)
                    { 
                        case Command.None:
                            await MessageHandler(result);
                            break;
                        case Command.Join:
                            await JoinHandler(result);
                            break;
                        case Command.Exit:
                            await ExitHandler(result);
                            break;
                        case Command.Users:
                            break;
                        case Command.Confirm:
                            break;
                    }


                }
                catch (Exception ext)
                {
                    await Console.Out.WriteLineAsync(ext.Message);
                }
            }
        }

        private async Task ExitHandler(ReceivResult result)
        {
            var user = User.FromDomain(await _context.Users.FirstAsync(x => x.Id == result.Message!.SenderID));
            user.LastOnline = DateTime.Now;
            await _context.SaveChangesAsync();
            _users.Remove(_users.First(u =>u.Id == result.Message.SenderID));
        }

        private async Task MessageHandler(ReceivResult result)
        {
            if(result.Message!.RecepentId < 0)
            {
                await SendAllASunc(result.Message);
            }
            else
            {
                await _source.Send(
                    result.Message,
                    _users.First(u => u.Id == result.Message.SenderID).EndPoint!,
                    CancellationToken);
                var recipientEndpoint = _users.FirstOrDefault(u => u.Id == result.Message.SenderID) ?. EndPoint;
                if (recipientEndpoint != null)
                {
                await _source.Send(
                   result.Message,
                   recipientEndpoint,
                   CancellationToken);
                }
            }
        }

        private async Task JoinHandler(ReceivResult result)
        {
            User? user = _users.FirstOrDefault(u => u.Name == result.Message!.Text);
            if (user == null)
            {
                user = new User() { Name = result.Message!.Text};
                _users.Add(user);
            }
            user.EndPoint = result.Endpoint;
            await _source.Send( 
                new Message() { Command = Command.Join, RecepentId = user.Id },
                user.EndPoint!,
                CancellationToken);
            await SendAllASunc(new Message() { Command = Command.Confirm, Text = $"{user.Name} joined to chat" });
            await SendAllASunc(new Message() { Command = Command.Users, RecepentId = user.Id, Users = _users });

            var unreded =await _context.Messages.Where(x => x.RecepientId == user.Id).ToListAsync();
            foreach (var message in unreded)
            {
                await _source.Send(
                Message.FromDomain(message),
                user.EndPoint!,
                CancellationToken);
            }
        }

        private async Task SendAllASunc(Message message)
        {
            foreach (var user in _users)
            {
                await _source.Send(
                message,
                user.EndPoint!,
                CancellationToken);
            }
        }
    }
}
