using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository _userRepo;
        private readonly ILikesRepository _likesRepo;
        public LikesController(IUserRepository userRepo, ILikesRepository likesRepo)
        {
            _likesRepo = likesRepo;
            _userRepo = userRepo;            
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await _userRepo.GetUserByUsernameAsync(username);
            var sourceUser = await _likesRepo.GetUserWithLikes(sourceUserId);

            if (likedUser == null) return NotFound();
            if (sourceUser.UserName == username) return BadRequest("you cannot like yourself!");

            var userLike = await _likesRepo.GetUserLike(sourceUserId, likedUser.Id);

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);

            if (await _userRepo.SaveAllAsync()) return Ok();

            return BadRequest("Failed to like user!");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDTO>>> GetUserLikes(string predicate)
        {
            var users = await _likesRepo.GetUserLikes(predicate, User.GetUserId());
            
            return Ok(users);
        }
    }
}