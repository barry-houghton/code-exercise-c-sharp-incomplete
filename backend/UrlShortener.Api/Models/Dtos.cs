using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models;

public class ShortenUrlRequest
{
    [Required(ErrorMessage = "fullUrl is required.")]
    [Url(ErrorMessage = "fullUrl must be a valid URL.")]
    public required string FullUrl { get; set; }

    public string? CustomAlias { get; set; }
}

public class ShortenUrlResponse
{
    public required string ShortUrl { get; set; }
    public required string Alias { get; set; }
    public required string FullUrl { get; set; }
}

public class UrlListRequest
{
    [Range(1, 10)]
    public required int PageIndex { get; set; } = 1;
    [Range(1, 100)]
    public required int PageSize { get; set; } = 10;
}

public class UrlListItem
{
    public required string Alias { get; set; }
    public required string FullUrl { get; set; }
    public required string ShortUrl { get; set; }
}
