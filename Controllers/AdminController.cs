using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DiveIntoIVE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public AdminController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string category = "general")
        {
            if (file == null || file.Length == 0)
                return BadRequest("File missing");

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", category);

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var id = Guid.NewGuid();
            var storedName = $"{id}-{file.FileName}";
            var filePath = Path.Combine(uploadFolder, storedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{category}/{storedName}";

            return Ok(new
            {
                id,
                fileName = file.FileName,
                storedName,
                url = fileUrl
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete-file")]
        public IActionResult DeleteFile([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path required");

            var physicalPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));

            if (!System.IO.File.Exists(physicalPath))
                return NotFound("File not found");

            System.IO.File.Delete(physicalPath);

            return Ok("File deleted");
        }

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