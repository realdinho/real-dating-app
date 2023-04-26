using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository _userRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IMapper _mapper;

        public MessagesController(
            IUserRepository userRepo, 
            IMessageRepository messageRepo, 
            IMapper mapper
        ) {
            _mapper = mapper;
            _messageRepo = messageRepo;
            _userRepo = userRepo;            
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        {
            var username = User.GetUsername();

            if (username.ToLower() == createMessageDTO.RecipientUsername.ToLower()) 
                return BadRequest("You cannot send messages to yourself!");

            var sender = await _userRepo.GetUserByUsernameAsync(username);
            var recipient = await _userRepo.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null) return NotFound("Recipient not found!");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content    
            };

            _messageRepo.AddMessage(message);

            if (await _messageRepo.SaveAllAsync()) return Ok(_mapper.Map<MessageDTO>(message));

            return BadRequest("Failed to send message!");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDTO>>> GetMessagesForUser(
            [FromQuery] MessageParams messageParams
        ) {
            messageParams.Username = User.GetUsername();
            var messages = await _messageRepo.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(
                new PaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages) 
            );

            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username)
        {
            var currentUsername = User.GetUsername();

            return Ok(await _messageRepo.GetMessageThread(currentUsername, username));
        }
    }
}