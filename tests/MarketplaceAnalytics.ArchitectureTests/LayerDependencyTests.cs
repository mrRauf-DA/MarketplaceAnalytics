using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace MarketplaceAnalytics.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "MarketplaceAnalytics.Domain";
    private const string ApplicationNamespace = "MarketplaceAnalytics.Application";
    private const string InfrastructureNamespace = "MarketplaceAnalytics.Infrastructure";
    private const string ApiNamespace = "MarketplaceAnalytics.API";

    private static readonly Assembly DomainAssembly = LoadProductionAssembly(DomainNamespace);
    private static readonly Assembly ApplicationAssembly = LoadProductionAssembly(ApplicationNamespace);
    private static readonly Assembly InfrastructureAssembly = LoadProductionAssembly(InfrastructureNamespace);
    private static readonly Assembly ApiAssembly = LoadProductionAssembly(ApiNamespace);

    [Fact]
    public void Domain_Must_Not_Depend_On_Application()
    {
        AssertHasNoDependency(DomainAssembly, ApplicationNamespace);
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Infrastructure()
    {
        AssertHasNoDependency(DomainAssembly, InfrastructureNamespace);
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_API()
    {
        AssertHasNoDependency(DomainAssembly, ApiNamespace);
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Infrastructure()
    {
        AssertHasNoDependency(ApplicationAssembly, InfrastructureNamespace);
    }

    [Fact]
    public void Application_Must_Not_Depend_On_API()
    {
        AssertHasNoDependency(ApplicationAssembly, ApiNamespace);
    }

    [Fact]
    public void Infrastructure_Must_Not_Depend_On_API()
    {
        AssertHasNoDependency(InfrastructureAssembly, ApiNamespace);
    }

    [Fact]
    public void API_Must_Not_Depend_Directly_On_Domain()
    {
        AssertHasNoDependency(ApiAssembly, DomainNamespace);
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Microsoft_Extensions_Configuration()
    {
        AssertHasNoDependency(DomainAssembly, "Microsoft.Extensions.Configuration");
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Microsoft_Extensions_Options()
    {
        AssertHasNoDependency(DomainAssembly, "Microsoft.Extensions.Options");
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Microsoft_Extensions_Configuration()
    {
        AssertHasNoDependency(ApplicationAssembly, "Microsoft.Extensions.Configuration");
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Microsoft_Extensions_Options()
    {
        AssertHasNoDependency(ApplicationAssembly, "Microsoft.Extensions.Options");
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_EntityFrameworkCore()
    {
        AssertHasNoDependency(DomainAssembly, "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Npgsql()
    {
        AssertHasNoDependency(DomainAssembly, "Npgsql");
    }

    [Fact]
    public void Application_Must_Not_Depend_On_EntityFrameworkCore()
    {
        AssertHasNoDependency(ApplicationAssembly, "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Application_Must_Not_Depend_On_Npgsql()
    {
        AssertHasNoDependency(ApplicationAssembly, "Npgsql");
    }

    [Fact]
    public void Domain_Must_Not_Depend_On_Ebay_Authentication()
    {
        AssertHasNoDependency(
            DomainAssembly,
            "MarketplaceAnalytics.Application.Integrations.Ebay.Authentication");
    }

    private static void AssertHasNoDependency(Assembly assembly, string forbiddenNamespace)
    {
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on {forbiddenNamespace}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    private static Assembly LoadProductionAssembly(string assemblyName)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");

        Assert.True(
            File.Exists(assemblyPath),
            $"The compiled production assembly was not found: {assemblyPath}");

        return Assembly.LoadFrom(assemblyPath);
    }
}
