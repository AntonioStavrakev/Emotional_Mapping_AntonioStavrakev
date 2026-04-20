using System.ComponentModel.DataAnnotations;
using Emotional_Mapping.Web.Models;

namespace Emotional_Mapping.Tests.Web;

public class ContactViewModelValidationTests
{
    [Fact]
    public void Validate_WhenModelIsValid_ShouldPass()
    {
        var model = CreateValidModel();

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenNameIsMissing_ShouldHaveNameError()
    {
        var model = CreateValidModel();
        model.Name = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ContactViewModel.Name)));
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveEmailError()
    {
        var model = CreateValidModel();
        model.Email = "invalid-email";

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ContactViewModel.Email)));
    }

    [Fact]
    public void Validate_WhenMessageIsMissing_ShouldHaveMessageError()
    {
        var model = CreateValidModel();
        model.Message = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ContactViewModel.Message)));
    }

    [Fact]
    public void Validate_WhenSubjectIsTooLong_ShouldHaveSubjectError()
    {
        var model = CreateValidModel();
        model.Subject = new string('a', 151);

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ContactViewModel.Subject)));
    }

    private static ContactViewModel CreateValidModel()
    {
        return new ContactViewModel
        {
            Name = "Antonio",
            Email = "antonio@example.com",
            Subject = "Тестова тема",
            Message = "Тестово съобщение"
        };
    }

    private static List<ValidationResult> Validate(ContactViewModel model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        return validationResults;
    }
}
