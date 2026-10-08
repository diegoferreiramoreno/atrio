using Atrio.Application;
using Atrio.Domain;
using Atrio.Infrastructure;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Atrio.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly string[] ForbiddenDomainDependencies =
    [
        "Atrio.Application",
        "Atrio.Infrastructure",
        "Atrio.Api"
    ];

    private static readonly string[] ForbiddenApplicationDependencies =
    [
        "Atrio.Infrastructure",
        "Atrio.Api"
    ];

    private static readonly string[] ForbiddenInfrastructureDependencies =
    [
        "Atrio.Api"
    ];

    [Fact]
    public void Domain_must_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenDomainDependencies)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Domain must remain independent. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_must_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenApplicationDependencies)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Application must not depend on Infrastructure or Api. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_api()
    {
        var result = Types.InAssembly(typeof(InfrastructureAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenInfrastructureDependencies)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Infrastructure must not depend on Api. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
