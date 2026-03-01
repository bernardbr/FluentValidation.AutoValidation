using FluentValidation;

namespace SharpGrip.FluentValidation.AutoValidation.Tests.Shared.Stubs;

public interface ICreatePost
{
    string Body { get; }
    string Title { get; }
}

public class CreatePostRequest : ICreatePost
{
    public string Body { get; set; } = null!;
    public string Title { get; set; } = null!;
}

public class CreatePostValidator : AbstractValidator<ICreatePost>
{
    public CreatePostValidator()
    {
        RuleFor(req => req.Title).NotEmpty().WithMessage("Title cannot be null or empty.");
        RuleFor(req => req.Body).NotEmpty().WithMessage("Body cannot be null or empty.");
    }
}