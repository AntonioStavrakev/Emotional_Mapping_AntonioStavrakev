using Emotional_Mapping.Web.Controllers;
using Emotional_Mapping.Web.Models;
using Emotional_Mapping.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Emotional_Mapping.Tests.Web;

public class ContactControllerTests
{
    [Fact]
    public void Index_Get_ShouldReturnViewWithContactViewModel()
    {
        var controller = CreateController(new FakeContactEmailService());

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ContactViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_Post_WhenModelStateIsInvalid_ShouldReturnSameViewWithoutSending()
    {
        var service = new FakeContactEmailService();
        var controller = CreateController(service);
        controller.ModelState.AddModelError(nameof(ContactViewModel.Email), "invalid");

        var model = new ContactViewModel
        {
            Name = "Antonio",
            Email = "bad-email",
            Message = "Test"
        };

        var result = await controller.Index(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Equal(0, service.SendCallCount);
    }

    [Fact]
    public async Task Index_Post_WhenSendSucceeds_ShouldRedirectAndSetSuccessMessage()
    {
        var service = new FakeContactEmailService();
        var controller = CreateController(service);
        var model = new ContactViewModel
        {
            Name = "Antonio",
            Email = "antonio@example.com",
            Subject = "Тест",
            Message = "Тестово съобщение"
        };

        var result = await controller.Index(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ContactController.Index), redirectResult.ActionName);
        Assert.Equal(1, service.SendCallCount);
        Assert.Equal("Съобщението беше изпратено успешно.", controller.TempData["ContactSuccess"]);
    }

    [Fact]
    public async Task Index_Post_WhenSendFails_ShouldReturnViewAndSetError()
    {
        var service = new FakeContactEmailService { ThrowOnSend = true };
        var controller = CreateController(service);
        var model = new ContactViewModel
        {
            Name = "Antonio",
            Email = "antonio@example.com",
            Message = "Тестово съобщение"
        };

        var result = await controller.Index(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Equal("Възникна проблем при изпращането. Провери email настройките.", controller.ViewBag.Error);
    }

    private static ContactController CreateController(FakeContactEmailService service)
    {
        var controller = new ContactController(service, NullLogger<ContactController>.Instance)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        return controller;
    }

    private sealed class FakeContactEmailService : IContactEmailService
    {
        public int SendCallCount { get; private set; }

        public bool ThrowOnSend { get; set; }

        public Task SendAsync(string fromName, string fromEmail, string subject, string message)
        {
            SendCallCount++;
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("SMTP failed");
            }

            return Task.CompletedTask;
        }

        public Task SendSystemEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return _data;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var pair in values)
            {
                _data[pair.Key] = pair.Value;
            }
        }
    }
}
