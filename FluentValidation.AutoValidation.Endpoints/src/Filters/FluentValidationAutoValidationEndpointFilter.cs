using System;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Configuration;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Interceptors;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;

namespace SharpGrip.FluentValidation.AutoValidation.Endpoints.Filters
{
    public class FluentValidationAutoValidationEndpointFilter(ILogger<FluentValidationAutoValidationEndpointFilter> logger, IOptions<AutoValidationEndpointsConfiguration> options) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext endpointFilterInvocationContext, EndpointFilterDelegate next)
        {
            var serviceProvider = endpointFilterInvocationContext.HttpContext.RequestServices;

            foreach (var argument in endpointFilterInvocationContext.Arguments)
            {
                if (argument == null) continue;
                
                if (!argument.GetType().IsCustomType() || serviceProvider.GetValidator(argument.GetType(), options.Value.UseBaseTypeValidations) is not IValidator validator)
                {
                    logger.LogDebug("Skipping argument of type '{Type}'. It's not a custom type, or no validator was found for this type.", argument.GetType().Name);
                    continue;
                }
                
                logger.LogDebug("Starting validation for argument of type '{Type}'.", argument.GetType().Name);

                var validationResult = await ExecuteValidation(endpointFilterInvocationContext, validator, serviceProvider, argument);

                if (!validationResult.IsValid) return CreateAInvalidResult(endpointFilterInvocationContext, argument, validationResult, serviceProvider);

                logger.LogDebug("Validation result valid for argument '{Argument}'.", argument.GetType().Name);
            }

            return await next(endpointFilterInvocationContext);
        }

        private object CreateAInvalidResult(EndpointFilterInvocationContext endpointFilterInvocationContext, object argument, ValidationResult validationResult, IServiceProvider serviceProvider)
        {
            logger.LogDebug("Validation result not valid for argument '{Argument}': {ErrorCount} validation error(s) found.", argument.GetType().Name, validationResult.Errors.Count);

            var fluentValidationAutoValidationResultFactory = serviceProvider.GetService<IFluentValidationAutoValidationResultFactory>();

            logger.LogDebug("Creating result for path '{Path}'.", endpointFilterInvocationContext.HttpContext.Request.Path);

            if (fluentValidationAutoValidationResultFactory != null)
            {
                logger.LogTrace("Creating result for path '{Path}' using a custom result factory.", endpointFilterInvocationContext.HttpContext.Request.Path);

                return fluentValidationAutoValidationResultFactory.CreateResult(endpointFilterInvocationContext, validationResult);
            }

            logger.LogTrace("Creating result for path '{Path}' using the default result factory.", endpointFilterInvocationContext.HttpContext.Request.Path);

            return new FluentValidationAutoValidationDefaultResultFactory().CreateResult(endpointFilterInvocationContext, validationResult);
        }

        private async ValueTask<ValidationResult> ExecuteValidation(EndpointFilterInvocationContext endpointFilterInvocationContext, IValidator validator, IServiceProvider serviceProvider, object argument)
        {
            var validatorInterceptor = validator as IValidatorInterceptor;
            var globalValidationInterceptor = serviceProvider.GetService<IGlobalValidationInterceptor>();

            IValidationContext validationContext = new ValidationContext<object>(argument);

            if (validatorInterceptor != null)
            {
                logger.LogDebug("Invoking validator interceptor BeforeValidation for argument '{Argument}'.", argument.GetType().Name);
                validationContext = await validatorInterceptor.BeforeValidation(endpointFilterInvocationContext, validationContext, endpointFilterInvocationContext.HttpContext.RequestAborted) ?? validationContext;
            }

            if (globalValidationInterceptor != null)
            {
                logger.LogDebug("Invoking global validation interceptor BeforeValidation for argument '{Argument}'.", argument.GetType().Name);
                validationContext = await globalValidationInterceptor.BeforeValidation(endpointFilterInvocationContext, validationContext, endpointFilterInvocationContext.HttpContext.RequestAborted) ?? validationContext;
            }

            var validationResult = await validator.ValidateAsync(validationContext, endpointFilterInvocationContext.HttpContext.RequestAborted);

            if (validatorInterceptor != null)
            {
                logger.LogDebug("Invoking validator interceptor AfterValidation for argument '{Argument}'.", argument.GetType().Name);
                validationResult = await validatorInterceptor.AfterValidation(endpointFilterInvocationContext, validationContext, validationResult, endpointFilterInvocationContext.HttpContext.RequestAborted) ?? validationResult;
            }

            if (globalValidationInterceptor != null)
            {
                logger.LogDebug("Invoking global validation interceptor AfterValidation for argument '{Argument}'.", argument.GetType().Name);
                validationResult = await globalValidationInterceptor.AfterValidation(endpointFilterInvocationContext, validationContext, validationResult, endpointFilterInvocationContext.HttpContext.RequestAborted) ?? validationResult;
            }

            return validationResult;
        }
    }
}