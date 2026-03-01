using System;
using FluentValidation;

namespace SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;

public static class ServiceProviderExtensions
{
    public static object? GetValidator(this IServiceProvider serviceProvider, Type type, bool useBaseTypeValidations)
    {
        var validator = serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(type));
        if (validator is not null) return validator;
        if (!useBaseTypeValidations) return null;

        return GetValidatorFromBaseClasses(serviceProvider, type)
               ?? GetValidatorFromInterfaces(serviceProvider, type);
    }

    private static object? GetValidatorFromBaseClasses(IServiceProvider serviceProvider, Type type)
    {
        var baseType = type.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            var baseValidatorType = typeof(IValidator<>).MakeGenericType(baseType);
            var baseValidator = serviceProvider.GetService(baseValidatorType);

            if (baseValidator is not null) return baseValidator;

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static object? GetValidatorFromInterfaces(IServiceProvider serviceProvider, Type type)
    {
        foreach (var interfaceType in type.GetInterfaces())
        {
            var interfaceValidatorType = typeof(IValidator<>).MakeGenericType(interfaceType);
            var interfaceValidator = serviceProvider.GetService(interfaceValidatorType);

            if (interfaceValidator is not null) return interfaceValidator;
        }

        return null;
    }
}