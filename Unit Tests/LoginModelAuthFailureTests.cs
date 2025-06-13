using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using DTO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using EasyTrade_Crypto.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelAuthFailureTests : LoginModelTestBase
    {
        public LoginModelAuthFailureTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WhenSignInAsyncThrowsException_ReturnsPageAndLogsError()
        {
            // Arrange
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel { Email = "test@example.com", Password = "password" };
            var userInfo = new UserLoginInfoDTO 
            { UserId = "test-user-id", 
                Username = "testuser",
                Email = "test@example.com", 
                Role = "Normal" 
            };

            _mockAccountService.Setup(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny))
                .Returns(true)
                .Callback((string e, string p, out UserLoginInfoDTO? ui, out string em) => { ui = userInfo; em = string.Empty; });

            _mockAuthService.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .ThrowsAsync(new InvalidOperationException("Authentication provider failed."));

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("An unexpected error occurred. Please try again.", _loginModel.ErrorMessage);
            VerifyLog(LogLevel.Error, "An unexpected error occurred", Times.Once());
        }
    }
}