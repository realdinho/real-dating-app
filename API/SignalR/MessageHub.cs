using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;

        public MessageHub(
            IMessageRepository messageRepo, 
            IUserRepository userRepo, 
            IMapper mapper, 
            IHubContext<PresenceHub> presenceHub
        ) {
            _mapper = mapper;
            _userRepo = userRepo;
            _messageRepo = messageRepo;
            _presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"];
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await AddToGroup(groupName);

            var messages = await _messageRepo.GetMessageThread(
                Context.User.GetUsername(), otherUser
            );

            await Clients.Group(groupName).SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await RemoveFromMessageGroup();
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO)
        {
            var username = Context.User.GetUsername();

            if (username.ToLower() == createMessageDTO.RecipientUsername.ToLower()) 
                throw new HubException("You cannot send messages to yourself");

            var sender = await _userRepo.GetUserByUsernameAsync(username);
            var recipient = await _userRepo.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null) throw new HubException("User not found!");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content    
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _messageRepo.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else 
            {
                var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);

                if (connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync(
                        "NewMessageReceived", 
                        new { username = sender.UserName, knownAs = sender.KnownAs }
                    );
                }
            }

            _messageRepo.AddMessage(message);

            if (await _messageRepo.SaveAllAsync())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }

        private async Task<bool> AddToGroup(string groupName)
        {
            var group = await _messageRepo.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if (group == null)
            {
                group = new Group(groupName);
                _messageRepo.AddGroup(group);
            }

            group.Connections.Add(connection);

            return await _messageRepo.SaveAllAsync();
        }

        private async Task RemoveFromMessageGroup()
        {
            var connection = await _messageRepo.GetConnection(Context.ConnectionId);
            _messageRepo.RemoveConnection(connection);
            await _messageRepo.SaveAllAsync();
        }
    }
}