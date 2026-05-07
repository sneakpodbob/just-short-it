using JustShortIt.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace JustShortIt.Tests;

public class ReservedIdProviderTests
{
    /// <summary>
    /// Verifies ReservedIdProvider captures literal first path segments and ignores parameter-first routes.
    /// </summary>
    [Test]
    public async Task ReservedIds_WhenBuiltFromEndpoints_ContainsLiteralFirstSegmentsCaseInsensitive()
    {
        var endpoints = new Endpoint[]
        {
            CreateRouteEndpoint("Login"),
            CreateRouteEndpoint("Logout"),
            CreateRouteEndpoint("Urls/{id}"),
            CreateRouteEndpoint("Inspect/{id?}"),
            CreateRouteEndpoint("{id?}")
        };

        var provider = new ReservedIdProvider([new TestEndpointDataSource(endpoints)]);

        await Assert.That(provider.ReservedIds.Contains("Login")).IsTrue();
        await Assert.That(provider.ReservedIds.Contains("login")).IsTrue();
        await Assert.That(provider.ReservedIds.Contains("Urls")).IsTrue();
        await Assert.That(provider.ReservedIds.Contains("Inspect")).IsTrue();
        await Assert.That(provider.ReservedIds.Contains("id")).IsFalse();
    }

    private static RouteEndpoint CreateRouteEndpoint(string pattern)
    {
        return new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(pattern),
            order: 0,
            EndpointMetadataCollection.Empty,
            displayName: pattern);
    }

    private sealed class TestEndpointDataSource(IReadOnlyList<Endpoint> endpoints) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints => endpoints;

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
    }
}
