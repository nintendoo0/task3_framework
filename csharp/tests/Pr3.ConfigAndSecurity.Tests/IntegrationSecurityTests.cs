using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Pr3.ConfigAndSecurity.Tests;

public sealed class IntegrationSecurityTests
{
    [Fact]
    public async Task Доверенный_источник_получает_разрешающий_заголовок()
    {
        await using var host = await StartApp(trustedOrigin: "http://localhost:5173", readLimit: 100, writeLimit: 100);
        var client = host.Client;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Contains("http://localhost:5173", values);
    }

    [Fact]
    public async Task Недоверенный_источник_не_получает_разрешающий_заголовок()
    {
        await using var host = await StartApp(trustedOrigin: "http://localhost:5173", readLimit: 100, writeLimit: 100);
        var client = host.Client;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
        request.Headers.TryAddWithoutValidation("Origin", "http://evil.local");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Ограничитель_частоты_возвращает_429()
    {
        await using var host = await StartApp(trustedOrigin: "http://localhost:5173", readLimit: 2, writeLimit: 1);
        var client = host.Client;

        async Task<HttpStatusCode> Call()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
            request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
            var resp = await client.SendAsync(request);
            return resp.StatusCode;
        }

        var a = await Call();
        var b = await Call();
        var c = await Call();

        Assert.Equal(HttpStatusCode.OK, a);
        Assert.Equal(HttpStatusCode.OK, b);
        Assert.Equal((HttpStatusCode)429, c);
    }

    [Fact]
    public async Task Защитные_заголовки_присутствуют()
    {
        await using var host = await StartApp(trustedOrigin: "http://localhost:5173", readLimit: 100, writeLimit: 100);
        var client = host.Client;

        var response = await client.GetAsync("/api/items");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Cache-Control"));
    }

    private static async Task<TestHost> StartApp(string trustedOrigin, int readLimit, int writeLimit)
    {
        var settings = new Dictionary<string, string?>
        {
            ["App:Mode"] = "Учебный",
            ["App:TrustedOrigins:0"] = trustedOrigin,
            ["App:RateLimits:ReadPerMinute"] = readLimit.ToString(),
            ["App:RateLimits:WritePerMinute"] = writeLimit.ToString()
        };

        var app = Program.BuildApp(
            Array.Empty<string>(),
            cfg => cfg.AddInMemoryCollection(settings),
            webHost => webHost.UseUrls("http://127.0.0.1:0"));

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var address = addresses?.Addresses.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Не удалось получить адрес тестового сервера");

        var client = new HttpClient { BaseAddress = new Uri(address, UriKind.Absolute) };
        return new TestHost(app, client);
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public HttpClient Client { get; }

        public TestHost(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            _app.Dispose();
        }
    }
}
