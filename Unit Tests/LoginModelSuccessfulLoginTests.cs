
using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using DTO;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelSuccessfulLoginTests : LoginModelTestBase
    {
        public LoginModelSuccessfulLoginTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WithValidCredentials_ShouldSignInAndRedirect()
        {
            // Arrange
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel { Email = "test@example.com", Password = "correctPassword123" };
            var userInfo = new UserLoginInfoDTO 
            {
                UserId = "test-user-id", 
                Username = "testuser", 
                Email = "test@example.com", 
                Role = "Normal" 
            };
            _mockAccountService.Setup(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny))
                .Returns(true)
                .Callback((string e, string p, out UserLoginInfoDTO? ui, out string em) => { ui = userInfo; em = string.Empty; });

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/TradingView", redirectResult.Url);
            _mockAuthService.Verify(s => s.SignInAsync(_httpContext, "Cookies", It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier).Value == userInfo.UserId), It.IsAny<AuthenticationProperties>()), Times.Once);
            VerifyLog(LogLevel.Information, $"User {userInfo.Username}", Times.Once());
        }
    }
}