using Moq;
using EasyTrade_Crypto.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using EasyTrade_Crypto.Interfaces;
using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public abstract class LoginModelTestBase
    {
        protected readonly Mock<IAccountService> _mockAccountService;
        protected readonly Mock<IAuthenticationService> _mockAuthService;
        protected readonly Mock<ILogger<LoginModel>> _mockLogger;
        protected readonly LoginModel _loginModel;
        protected readonly HttpContext _httpContext;



        protected LoginModelTestBase()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockLogger = new Mock<ILogger<LoginModel>>();

            _httpContext = new DefaultHttpContext();
            _httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(_mockAuthService.Object)
                .BuildServiceProvider();

            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            var actionContext = new ActionContext(_httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            var viewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), modelState);
            var pageContext = new PageContext(actionContext) { ViewData = viewData };

            _loginModel = new LoginModel(_mockAccountService.Object, _mockLogger.Object)
            {
                PageContext = pageContext,
                Url = new UrlHelper(actionContext),
                Input = new LoginModel.InputModel() 
                {
                    Email = "", 
                    Password = "" 
                }
            };
        }

        protected void VerifyLog(LogLevel expectedLevel, string expectedMessage, Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
    }
}