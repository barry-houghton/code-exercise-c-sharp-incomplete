using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using Xunit;

namespace UrlShortener.Tests;

public class UrlsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UrlsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();

                services.AddSingleton(connection);
                services.AddDbContext<AppDbContext>((sp, options) =>
                {
                    var sqliteConnection = sp.GetRequiredService<SqliteConnection>();
                    options.UseSqlite(sqliteConnection);
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // --- POST /shorten ---

    [Fact]
    public async Task Post_Shorten_ValidRequest_Returns201WithShortUrl()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/shorten", new
        {
            fullUrl = "https://example.com/some/path"
        });

        if(!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status code {response.StatusCode}: {errorContent}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>();
        body!.ShortUrl.Should().Contain("/");
        body.FullUrl.Should().Be("https://example.com/some/path");
    }

    [Fact]
    public async Task Post_Shorten_WithCustomAlias_ReturnsAliasInResponse()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/shorten", new
        {
            fullUrl = "https://example.com",
            customAlias = "my-alias"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>();
        body!.Alias.Should().Be("my-alias");
    }

    [Fact]
    public async Task Post_Shorten_DuplicateAlias_Returns400()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://a.com", customAlias = "clash" });
        var response = await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://b.com", customAlias = "clash" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Shorten_MissingFullUrl_Returns400()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/shorten", new { customAlias = "abc" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Shorten_InvalidUrl_Returns400()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/shorten", new { fullUrl = "not-a-url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- GET /{alias} (redirect) ---

    [Fact]
    public async Task Get_Alias_ExistingAlias_Returns302WithLocation()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://target.com", customAlias = "redir" });

        var response = await client.GetAsync("/redir");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be("https://target.com/");
    }

    [Fact]
    public async Task Get_Alias_UnknownAlias_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/unknown-alias-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- GET /urls ---

    [Fact]
    public async Task Get_Urls_ReturnsAllShortenedUrls()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://one.com", customAlias = "list-one" });
        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://two.com", customAlias = "list-two" });

        var response = await client.GetAsync("/urls");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UrlListItem>>();
        body!.Items.Should().Contain(u => u.Alias == "list-one");
        body.Items.Should().Contain(u => u.Alias == "list-two");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(99, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 999)]
    public async Task Get_Urls_InvalidPaging_Returns400(int pageIndex, int pageSize)
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/urls?pageIndex={pageIndex}&pageSize={pageSize}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(1, 2, 10)]
    [InlineData(3, 3, 7)]
    [InlineData(10, 5, 7)]
    public async Task Get_Urls_ValidPaging_ReturnsPagedShortenedUrls(int pageIndex, int pageSize, int totalCount)
    {
        var expectedRows = Math.Clamp(totalCount - (pageIndex - 1) * pageSize, 0, pageSize);
        var client = CreateClient();
        foreach (var i in Enumerable.Range(1, totalCount))
        {
            await client.PostAsJsonAsync("/shorten", new { fullUrl = $"https://{i}.com", customAlias = $"{i:000}" });
        }

        var response = await client.GetAsync($"/urls?pageIndex={pageIndex}&pageSize={pageSize}");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<UrlListItem>>();
        body!.Items.Should().HaveCount(expectedRows);
        body.PageIndex.Should().Be(pageIndex);
        body.PageSize.Should().Be(pageSize);
        body.TotalCount.Should().Be(totalCount);
    }

    // --- DELETE /{alias} ---

    [Fact]
    public async Task Delete_Alias_ExistingAlias_Returns204()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://example.com", customAlias = "del-me" });

        var response = await client.DeleteAsync("/del-me");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Alias_ThenGet_Returns404()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/shorten", new { fullUrl = "https://example.com", customAlias = "gone" });

        await client.DeleteAsync("/gone");
        var getResponse = await client.GetAsync("/gone");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Alias_UnknownAlias_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync("/nobody");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
