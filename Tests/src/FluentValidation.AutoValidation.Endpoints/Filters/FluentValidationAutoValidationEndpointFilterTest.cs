// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Configuration;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Filters;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Interceptors;
using SharpGrip.FluentValidation.AutoValidation.Tests.Shared.Stubs;
using Xunit;

namespace SharpGrip.FluentValidation.AutoValidation.Tests.FluentValidation.AutoValidation.Endpoints.Filters;

public class FluentValidationAutoValidationEndpointFilterTest
{
    private static readonly Dictionary<string, string[]> ValidationFailures = new()
    {
        {nameof(TestModel.Parameter1), [$"'{nameof(TestModel.Parameter1)}' must be empty."]},
        {nameof(TestModel.Parameter2), [$"'{nameof(TestModel.Parameter2)}' must be empty."]},
        {nameof(TestModel.Parameter3), [$"'{nameof(TestModel.Parameter3)}' must be empty."]}
    };

    [Fact]
    public async Task TestInvokeAsync_ValidatorFound()
    {
        var logger = Substitute.For<ILogger<FluentValidationAutoValidationEndpointFilter>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var endpointFilterInvocationContext = Substitute.For<EndpointFilterInvocationContext>();
        var configuration = Substitute.For<IOptions<AutoValidationEndpointsConfiguration>>(); 
        configuration.Value.Returns(new AutoValidationEndpointsConfiguration());

        endpointFilterInvocationContext.HttpContext.Returns(new DefaultHttpContext {RequestServices = serviceProvider});
        endpointFilterInvocationContext.Arguments.Returns(new List<object?> {new TestModel {Parameter1 = "Value 1", Parameter2 = "Value 2", Parameter3 = "Value 3"}});
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(TestModel))).Returns(new TestValidator());
        serviceProvider.GetService(typeof(IGlobalValidationInterceptor)).Returns(new GlobalValidationInterceptor());

        var validationFailuresValues = ValidationFailures.Values.ToList();

        var endpointFilter = new FluentValidationAutoValidationEndpointFilter(logger, configuration);

        var result = (ValidationProblem) (await endpointFilter.InvokeAsync(endpointFilterInvocationContext, _ => ValueTask.FromResult(new object())!))!;
        var problemDetailsErrorValues = result.ProblemDetails.Errors.ToList();

        Assert.Contains(validationFailuresValues[0].First(), problemDetailsErrorValues[0].Value);
        Assert.Contains(validationFailuresValues[1].First(), problemDetailsErrorValues[1].Value);
        Assert.Contains(validationFailuresValues[2].First(), problemDetailsErrorValues[2].Value);
    }

    [Fact]
    public async Task TestInvokeAsync_ValidatorNotFound()
    {
        var logger = Substitute.For<ILogger<FluentValidationAutoValidationEndpointFilter>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var endpointFilterInvocationContext = Substitute.For<EndpointFilterInvocationContext>();
        var configuration = Substitute.For<IOptions<AutoValidationEndpointsConfiguration>>(); 
        configuration.Value.Returns(new AutoValidationEndpointsConfiguration());

        endpointFilterInvocationContext.HttpContext.Returns(new DefaultHttpContext {RequestServices = serviceProvider});
        endpointFilterInvocationContext.Arguments.Returns(new List<object?> {new TestModel {Parameter1 = "Value 1", Parameter2 = "Value 2", Parameter3 = "Value 3"}});
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(TestModel))).Returns(null);
        serviceProvider.GetService(typeof(IGlobalValidationInterceptor)).Returns(null);

        var endpointFilter = new FluentValidationAutoValidationEndpointFilter(logger, configuration);

        var result = await endpointFilter.InvokeAsync(endpointFilterInvocationContext, _ => ValueTask.FromResult(new object())!);

        Assert.IsType<object>(result);
    }
    
    [Fact]
    public async Task TestInvokeAsync_BaseTypeValidatorNotFound()
    {
        var logger = Substitute.For<ILogger<FluentValidationAutoValidationEndpointFilter>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var endpointFilterInvocationContext = Substitute.For<EndpointFilterInvocationContext>();
        var configuration = Substitute.For<IOptions<AutoValidationEndpointsConfiguration>>(); 
        configuration.Value.Returns(new AutoValidationEndpointsConfiguration());

        endpointFilterInvocationContext.HttpContext.Returns(new DefaultHttpContext {RequestServices = serviceProvider});
        endpointFilterInvocationContext.Arguments.Returns(new List<object?> {new CreatePostRequest()});
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(CreatePostRequest))).Returns(null);
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(ICreatePost))).Returns(new CreatePostValidator());
        serviceProvider.GetService(typeof(IGlobalValidationInterceptor)).Returns(null);

        var endpointFilter = new FluentValidationAutoValidationEndpointFilter(logger, configuration);

        var result = await endpointFilter.InvokeAsync(endpointFilterInvocationContext, _ => ValueTask.FromResult(new object())!);

        Assert.IsType<object>(result);
    } 
    
    [Fact]
    public async Task TestInvokeAsync_BaseTypeValidatorFound()
    {
        var logger = Substitute.For<ILogger<FluentValidationAutoValidationEndpointFilter>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var endpointFilterInvocationContext = Substitute.For<EndpointFilterInvocationContext>();
        var configuration = Substitute.For<IOptions<AutoValidationEndpointsConfiguration>>(); 
        configuration.Value.Returns(new AutoValidationEndpointsConfiguration().WithBaseTypeValidations());

        endpointFilterInvocationContext.HttpContext.Returns(new DefaultHttpContext {RequestServices = serviceProvider});
        endpointFilterInvocationContext.Arguments.Returns(new List<object?> {new CreatePostRequest()});
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(CreatePostRequest))).Returns(null);
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(ICreatePost))).Returns(new CreatePostValidator());
        serviceProvider.GetService(typeof(IGlobalValidationInterceptor)).Returns(new GlobalValidationInterceptor());

        var endpointFilter = new FluentValidationAutoValidationEndpointFilter(logger, configuration);

        var result = await endpointFilter.InvokeAsync(endpointFilterInvocationContext, _ => ValueTask.FromResult<object?>(new object()));
        Assert.IsType<ValidationProblem>(result, false);
        var problem = result as ValidationProblem;
        var problemDetailsErrorValues = problem.ProblemDetails.Errors.ToList();
        
        Assert.Contains(problemDetailsErrorValues, x => x.Value.Contains("Title cannot be null or empty."));
        Assert.Contains(problemDetailsErrorValues, x => x.Value.Contains("Body cannot be null or empty."));
    }     

    private class TestModel
    {
        public string? Parameter1 { get; set; }
        public string? Parameter2 { get; set; }
        public string? Parameter3 { get; set; }
    }

    private class TestValidator : AbstractValidator<TestModel>, IValidatorInterceptor
    {
        public TestValidator()
        {
            RuleFor(x => x.Parameter1).Empty();
            RuleFor(x => x.Parameter2).Empty();
            RuleFor(x => x.Parameter3).Empty();
        }

        public Task<IValidationContext?> BeforeValidation(EndpointFilterInvocationContext endpointFilterInvocationContext, IValidationContext validationContext, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IValidationContext?>(null);
        }

        public Task<ValidationResult?> AfterValidation(EndpointFilterInvocationContext endpointFilterInvocationContext, IValidationContext validationContext, ValidationResult validationResult, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ValidationResult?>(null);
        }
    }

    private class GlobalValidationInterceptor : IGlobalValidationInterceptor
    {
        public Task<IValidationContext?> BeforeValidation(EndpointFilterInvocationContext endpointFilterInvocationContext, IValidationContext validationContext, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IValidationContext?>(null);
        }

        public Task<ValidationResult?> AfterValidation(EndpointFilterInvocationContext endpointFilterInvocationContext, IValidationContext validationContext, ValidationResult validationResult, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ValidationResult?>(null);
        }
    }
}