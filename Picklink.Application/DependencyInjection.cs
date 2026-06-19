using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Picklink.Application.Validators;

namespace Picklink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}
