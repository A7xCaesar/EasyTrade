using Xunit;
using Moq;
using System.Threading.Tasks;
using DTO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelNullUserInfoTests : LoginModelTestBase
    {
        public LoginModelNullUserInfoTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WhenValidationSucceedsButUserInfoIsNull_ReturnsPageAndLogsError()
        {
            // Arrange
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel 
            { 
                Email = "test@example.com", 
                Password = "password" 
            };
            _mockAccountService.Setup(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny))
                .Returns(true) // The service claims success...
                .Callback((string e, string p, out UserLoginInfoDTO? ui, out string em) =>
                {
                    ui = null; // ...but breaks the contract by providing a null user object.
                    em = string.Empty;
                });

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("An unexpected error occurred. Please try again.", _loginModel.ErrorMessage);
            VerifyLog(LogLevel.Error, "An unexpected error occurred", Times.Once());
            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        }
    }
}