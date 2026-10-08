using Atrio.Domain;

namespace Atrio.Application;

public sealed class ApplicationAssemblyMarker
{
    public DomainAssemblyMarker? Marker { get; set; }
}
