using System.Text.RegularExpressions;
using DiveIntoIVE.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiveIntoIVE.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Super-Admin")]
[Route("api/admin/media")]
public class AdminMediaController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly AppSettings _appSettings;

    public AdminMediaController(IWebHostEnvironment environment, IOptions<AppSettings> appSettings)
    {
        _environment = environment;
        _appSettings = appSettings.Value;
    }

    private string ResolveWebRootPath()
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");

        return webRootPath;
    }

    private string ResolveUploadsRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_appSettings.UploadsRootPath))
        {
            if (Path.IsPathRooted(_appSettings.UploadsRootPath))
                return _appSettings.UploadsRootPath;

            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _appSettings.UploadsRootPath));
        }

        return Path.Combine(ResolveWebRootPath(), "uploads");
    }

    [HttpGet("library")]
    public IActionResult GetLibrary([FromQuery] string? search = null)
    {
        var uploadRoot = ResolveUploadsRootPath();

        if (!Directory.Exists(uploadRoot))
        {
            return Ok(new
            {
                totalFiles = 0,
                totalBytes = 0L,
                files = Array.Empty<object>()
            });
        }

        var fileEntries = Directory.GetFiles(uploadRoot, "*", SearchOption.AllDirectories)
            .Select(path =>
            {
                var info = new FileInfo(path);
                var relativePath = "/uploads/" + Path.GetRelativePath(uploadRoot, path).Replace("\\", "/");
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var folder = Path.GetDirectoryName(Path.GetRelativePath(uploadRoot, path))?.Replace("\\", "/") ?? string.Empty;

                return new
                {
                    fileName = info.Name,
                    folder,
                    relativePath,
                    url = $"{baseUrl}{relativePath}",
                    sizeBytes = info.Length,
                    updatedAtUtc = info.LastWriteTimeUtc
                };
            })
            .Where(file => string.IsNullOrWhiteSpace(search)
                           || file.fileName.Contains(search, StringComparison.OrdinalIgnoreCase)
                           || file.folder.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.updatedAtUtc)
            .ToList();

        return Ok(new
        {
            totalFiles = fileEntries.Count,
            totalBytes = fileEntries.Sum(file => file.sizeBytes),
            files = fileEntries
        });
    }

    public record UploadMediaDto(IFormFile File, string? Folder);

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadMediaDto dto)
    {
        if (dto.File is null || dto.File.Length == 0)
            return BadRequest("Please upload an image file.");

        var extension = Path.GetExtension(dto.File.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Unsupported file type. Allowed: .jpg, .jpeg, .png, .webp, .gif");

        var normalizedFolder = Regex.Replace((dto.Folder ?? "quiz").Trim().ToLowerInvariant(), "[^a-z0-9-_]", string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedFolder))
            normalizedFolder = "quiz";

        var uploadRoot = Path.Combine(ResolveUploadsRootPath(), normalizedFolder);
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await dto.File.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/{normalizedFolder}/{fileName}";
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            url = $"{baseUrl}{relativePath}",
            path = relativePath
        });
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Media URL is required.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return BadRequest("Invalid media URL.");

        var localPath = uri.LocalPath;
        if (!localPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only uploaded media can be deleted.");

        var filePath = Path.Combine(ResolveUploadsRootPath(), localPath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(filePath))
            return NotFound("File not found.");

        System.IO.File.Delete(filePath);
        return Ok(new { message = "Media deleted." });
    }

    public record RenameMediaDto(string Url, string NewFileNameWithoutExtension);

    [HttpPatch("rename")]
    public IActionResult Rename([FromBody] RenameMediaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
            return BadRequest("Media URL is required.");

        if (string.IsNullOrWhiteSpace(dto.NewFileNameWithoutExtension))
            return BadRequest("New file name is required.");

        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri))
            return BadRequest("Invalid media URL.");

        var localPath = uri.LocalPath;
        if (!localPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only uploaded media can be renamed.");

        var currentPath = Path.Combine(ResolveUploadsRootPath(), localPath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(currentPath))
            return NotFound("File not found.");

        var extension = Path.GetExtension(currentPath);
        var safeFileName = Regex.Replace(dto.NewFileNameWithoutExtension.Trim(), "[^a-zA-Z0-9-_]", string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return BadRequest("New file name contains invalid characters.");

        var directory = Path.GetDirectoryName(currentPath)!;
        var newPath = Path.Combine(directory, $"{safeFileName}{extension.ToLowerInvariant()}");

        if (System.IO.File.Exists(newPath))
            return Conflict("A file with that name already exists in this folder.");

        var oldRelativePath = localPath;
        var oldUrl = dto.Url;

        System.IO.File.Move(currentPath, newPath);

        // Backward compatibility: keep the old URL valid for existing frontend references.
        // Keep a copy at the old path so existing saved URLs still render.
        System.IO.File.Copy(newPath, currentPath, overwrite: false);

        var relativePath = "/uploads/" + Path.GetRelativePath(ResolveUploadsRootPath(), newPath).Replace("\\", "/");
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            message = "Media renamed.",
            url = $"{baseUrl}{relativePath}",
            relativePath,
            fileName = Path.GetFileName(newPath),
            previousUrl = oldUrl,
            previousRelativePath = oldRelativePath
        });
    }
}
