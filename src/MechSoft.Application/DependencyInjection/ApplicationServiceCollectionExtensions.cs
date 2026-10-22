using Microsoft.Extensions.DependencyInjection;

namespace MechSoft.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application-layer services. Currently the Application layer
    /// exposes pure logic (DTOs, calculation helpers) with no stateful
    /// services of its own to register, but this hook keeps Program.cs
    /// consistent with the Infrastructure registration pattern.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services;
    }
}
