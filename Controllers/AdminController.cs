using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DiveIntoIVE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost("upload-member")]
        public IActionResult UploadMember()
        {
            return Ok("Member uploaded by Admin");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("edit-member")]
        public IActionResult EditMember()
        {
            return Ok("Member edited by Admin");
        }
    }
}