using System;
using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;

namespace SharpGrip.FluentValidation.AutoValidation.Endpoints.Configuration;

public class AutoValidationEndpointsConfiguration
{
    /// <summary>
    /// Gets a value indicating whether the validation process should look for validators
    /// registered for interfaces or base types when a validator for the concrete type is not found.
    /// </summary>
    public bool UseBaseTypeValidations { get; private set; }

    /// <summary>
    /// Holds the overridden result factory. This property is meant for infrastructure and should not be used by application code.
    /// </summary>
    public Type? OverriddenResultFactory { get; private set; }

    /// <summary>
    /// Overrides the default result factory with a custom result factory. Custom result factories are required to implement <see cref="IFluentValidationAutoValidationResultFactory" />.
    /// The default result factory returns the validation errors wrapped in a <see cref="ValidationProblem" /> object.
    /// </summary>
    /// <see cref="FluentValidationAutoValidationDefaultResultFactory" />
    /// <typeparam name="TResultFactory">The custom result factory implementing <see cref="IFluentValidationAutoValidationResultFactory" />.</typeparam>
    public AutoValidationEndpointsConfiguration OverrideDefaultResultFactoryWith<TResultFactory>() where TResultFactory : IFluentValidationAutoValidationResultFactory
    {
        OverriddenResultFactory = typeof(TResultFactory);
        return this;
    }

    /// <summary>
    /// Enables the fallback mechanism to search for validators in the type hierarchy (interfaces and base classes)
    /// if no specific validator is registered for the primary parameter type.
    /// </summary>
    /// <returns>The current <see cref="AutoValidationEndpointsConfiguration" /> instance for fluent chaining.</returns>
    public AutoValidationEndpointsConfiguration WithBaseTypeValidations()
    {
        UseBaseTypeValidations = true;
        return this;
    }
}