// ReSharper disable InconsistentNaming

using System;
using FluentValidation;
using NSubstitute;
using SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;
using SharpGrip.FluentValidation.AutoValidation.Tests.Shared.Stubs;
using Xunit;

namespace SharpGrip.FluentValidation.AutoValidation.Tests.FluentValidation.AutoValidation.Shared.Extensions;

public class ServiceProviderExtensionsTest
{
    [Fact]
    public void Test_GetValidator()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        var testModel = new TestModel();
        var testModelValidator = new TestModelValidator();

        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(testModel.GetType())).Returns(testModelValidator);

        var validator = serviceProvider.GetValidator(testModel.GetType(), false);

        Assert.Equal(testModelValidator, validator);
    }

    [Fact]
    public void Test_GetValidator_WithBaseTypeValidator()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        var testModel = new CreatePostRequest();
        var testModelValidator = new CreatePostValidator();

        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(testModel.GetType())).Returns(null);
        serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(typeof(ICreatePost))).Returns(testModelValidator);

        var validator = serviceProvider.GetValidator(testModel.GetType(), false);
        Assert.Null(validator);

        validator = serviceProvider.GetValidator(testModel.GetType(), true);
        Assert.Equal(testModelValidator, validator);
    }

    private class TestModel;

    private class TestModelValidator;
}