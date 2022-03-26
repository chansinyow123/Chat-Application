using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public VideoController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("")]
        [Authorize]
        public async Task<IActionResult> CreateMeeting()
        {
            var videoCall = new VideoCall
            {
                Member = 0,
                OnCreate = DateTime.Now,
            };

            await _dbContext.VideoCall.AddAsync(videoCall);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Id = videoCall.Id });
        }
    }
}
