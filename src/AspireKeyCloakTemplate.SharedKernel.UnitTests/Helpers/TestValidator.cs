using FluentValidation;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value cannot be empty");

        RuleFor(x => x.Value)
            .MinimumLength(3)
            .WithMessage("Value must be at least 3 characters");
    }
}

public class AlwaysValidValidator : AbstractValidator<TestRequest>
{
}
