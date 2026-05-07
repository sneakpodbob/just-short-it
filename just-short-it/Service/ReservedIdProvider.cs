using Microsoft.AspNetCore.Routing.Patterns;

namespace JustShortIt.Service;

public interface IReservedIdProvider
{
    IReadOnlySet<string> ReservedIds { get; }
}

public sealed class ReservedIdProvider : IReservedIdProvider
{
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;
    private readonly Lazy<IReadOnlySet<string>> _reservedIds;

    public ReservedIdProvider(IEnumerable<EndpointDataSource> endpointDataSources)
    {
        _endpointDataSources = endpointDataSources;
        _reservedIds = new Lazy<IReadOnlySet<string>>(BuildReservedIds, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlySet<string> ReservedIds => _reservedIds.Value;

    private IReadOnlySet<string> BuildReservedIds()
    {
        var reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in _endpointDataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>())
        {
            var firstSegment = endpoint.RoutePattern.PathSegments.FirstOrDefault();
            var firstPart = firstSegment?.Parts.FirstOrDefault();

            if (firstPart is RoutePatternLiteralPart literalPart && !string.IsNullOrWhiteSpace(literalPart.Content))
            {
                reservedIds.Add(literalPart.Content);
            }
        }

        return reservedIds;
    }
}