using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Exceptions;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;
using Xunit;

namespace UrlShortener.Tests;

public class UrlShortenerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly UrlShortenerService _sut;
    private const string BaseUrl = "http://localhost:8080";

    public UrlShortenerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new UrlShortenerService(_db);
    }

    public void Dispose() => _db.Dispose();

    // --- ShortenAsync ---

    [Fact]
    public async Task ShortenAsync_WithValidUrl_ReturnsShortUrl()
    {
        var request = new ShortenUrlRequest { FullUrl = "https://example.com/very/long/path" };

        var result = await _sut.ShortenAsync(request, BaseUrl);

        result.ShortUrl.Should().StartWith(BaseUrl + "/");
        result.FullUrl.Should().Be(request.FullUrl);
        result.Alias.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ShortenAsync_WithCustomAlias_UsesProvidedAlias()
    {
        var request = new ShortenUrlRequest
        {
            FullUrl = "https://example.com",
            CustomAlias = "my-alias"
        };

        var result = await _sut.ShortenAsync(request, BaseUrl);

        result.Alias.Should().Be("my-alias");
        result.ShortUrl.Should().Be($"{BaseUrl}/my-alias");
    }

    [Fact]
    public async Task ShortenAsync_WithDuplicateAlias_ThrowsInvalidOperationException()
    {
        var request = new ShortenUrlRequest
        {
            FullUrl = "https://example.com",
            CustomAlias = "duplicate"
        };
        await _sut.ShortenAsync(request, BaseUrl);

        var secondRequest = new ShortenUrlRequest
        {
            FullUrl = "https://another.com",
            CustomAlias = "duplicate"
        };

        await _sut.Invoking(s => s.ShortenAsync(secondRequest, BaseUrl))
            .Should().ThrowAsync<DuplicateAliasException>()
            .WithMessage("*duplicate*");
    }

    [Theory]
    [InlineData("a")]          // too short (1 char)
    [InlineData("this-alias-is-way-too-long-to-be-accepted-by-the-service-because-it-exceeds-64-characters")]
    [InlineData("bad alias")]  // contains space
    [InlineData("bad/alias")]  // contains slash
    public async Task ShortenAsync_WithInvalidAlias_ThrowsArgumentException(string alias)
    {
        var request = new ShortenUrlRequest
        {
            FullUrl = "https://example.com",
            CustomAlias = alias
        };

        await _sut.Invoking(s => s.ShortenAsync(request, BaseUrl))
            .Should().ThrowAsync<InvalidAliasException>();
    }

    [Fact]
    public async Task ShortenAsync_GeneratesUniqueAliasEachTime()
    {
        var r1 = await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://one.com" }, BaseUrl);
        var r2 = await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://two.com" }, BaseUrl);

        r1.Alias.Should().NotBe(r2.Alias);
    }

    // --- GetFullUrlAsync ---

    [Fact]
    public async Task GetFullUrlAsync_WithExistingAlias_ReturnsFullUrl()
    {
        await _sut.ShortenAsync(new ShortenUrlRequest
        {
            FullUrl = "https://example.com",
            CustomAlias = "test-alias"
        }, BaseUrl);

        var result =  _sut.GetFullUrlAsync("test-alias").GetAwaiter().GetResult();

        result.Should().Be("https://example.com");
    }

    [Fact]
    public async Task GetFullUrlAsync_WithUnknownAlias_ReturnsNull()
    {
        var result = await _sut.GetFullUrlAsync("nonexistent");

        result.Should().BeNull();
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsAllUrls()
    {
        await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://one.com", CustomAlias = "one" }, BaseUrl);
        await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://two.com", CustomAlias = "two" }, BaseUrl);

        var results = (await _sut.GetAllAsync(BaseUrl)).ToList();

        results.Should().HaveCount(2);
        results.Should().Contain(r => r.Alias == "one");
        results.Should().Contain(r => r.Alias == "two");
    }

    [Fact]
    public async Task GetAllAsync_EachItemHasCorrectShortUrl()
    {
        await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://example.com", CustomAlias = "abc" }, BaseUrl);

        var results = (await _sut.GetAllAsync(BaseUrl)).ToList();

        results.Single().ShortUrl.Should().Be($"{BaseUrl}/abc");
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var results = await _sut.GetAllAsync(BaseUrl);

        results.Should().BeEmpty();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WithExistingAlias_ReturnsTrueAndRemovesEntry()
    {
        await _sut.ShortenAsync(new ShortenUrlRequest { FullUrl = "https://example.com", CustomAlias = "to-delete" }, BaseUrl);

        var deleted = await _sut.DeleteAsync("to-delete");
        var afterDelete = await _sut.GetFullUrlAsync("to-delete");

        deleted.Should().BeTrue();
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownAlias_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync("ghost");

        result.Should().BeFalse();
    }
}
