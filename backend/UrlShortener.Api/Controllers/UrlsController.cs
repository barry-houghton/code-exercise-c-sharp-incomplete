using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class UrlsController(IUrlShortenerService service, ILogger<UrlsController> logger) : ControllerBase
{

    // POST /shorten
    [HttpPost("shorten")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Shorten([FromBody] ShortenUrlRequest request)
    {
        var baseUrl = GetBaseUrl();
        var url = await service.ShortenAsync(request, baseUrl);
        return new ObjectResult(url) { StatusCode = StatusCodes.Status201Created };
    }

    // GET /urls
    [HttpGet("urls")]
    [ProducesResponseType(typeof(IEnumerable<UrlListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var baseUrl = GetBaseUrl();
        var urls = await service.GetAllAsync(baseUrl);
        return Ok(urls);
    }

    // GET /{alias}  — redirect
    [HttpGet("{alias}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToUrl(string alias)
    {
        var fullUrl = await service.GetFullUrlAsync(alias);
        if (fullUrl is null)
            return NotFound(new { error = $"No URL found for alias '{alias}'." });

        return Redirect(fullUrl);
    }

    // DELETE /{alias}
    [HttpDelete("{alias}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string alias)
    {
        var deleted = await service.DeleteAsync(alias);
        if (!deleted)
            return NotFound(new { error = $"No URL found for alias '{alias}'." });

        return NoContent();
    }

    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}
