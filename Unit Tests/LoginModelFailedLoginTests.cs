// ---- Tests/PageModels/LoginTests/LoginModelFailedLoginTests.cs ----

using Xunit;
using Moq;
using DTO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelFailedLoginTests : LoginModelTestBase
    {
        public LoginModelFailedLoginTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WithInvalidPassword_ShouldReturnPageAndLogWarning()
        {
            // Arrange
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel 
            {
                Email = "test@example.com", 
                Password = "incorrectPassword" 
            };
            string expectedErrorMessage = "Invalid login attempt";
            _mockAccountService.Setup(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny))
                .Returns(false)
                .Callback((string e, string p, out UserLoginInfoDTO? ui, out string em) => { ui = null; em = expectedErrorMessage; });

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(expectedErrorMessage, _loginModel.ErrorMessage);
            _mockAuthService.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            VerifyLog(LogLevel.Warning, "Failed login attempt", Times.Once());
        }
    }
}