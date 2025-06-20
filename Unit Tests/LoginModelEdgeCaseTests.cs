using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelEdgeCaseTests : LoginModelTestBase
    {
        public LoginModelEdgeCaseTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageResultWithoutCallingService()
        {
            // Arrange
            _loginModel.ModelState.AddModelError("Input.Email", "Email is required");

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            _mockAccountService.Verify(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny), Times.Never);
            VerifyLog(LogLevel.Information, "Login attempt", Times.Never());
        }

        [Fact]
        public async Task OnPostAsync_WhenAccountServiceThrowsException_ReturnsPageAndLogsError()
        {
            // Arrange
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel { Email = "test@example.com", Password = "password" };
            var dbException = new InvalidOperationException("Database connection failed");

            _mockAccountService.Setup(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny))
                .Throws(dbException);

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("An unexpected error occurred. Please try again.", _loginModel.ErrorMessage);
            VerifyLog(LogLevel.Error, "An unexpected error occurred", Times.Once());
        }

        [Fact]
        public async Task OnPostAsync_WithValidLoginAndInvalidReturnUrl_RedirectsToDefaultPage()
        {
            // Arrange
            var invalidReturnUrl = "http://malicious-site.com";
            _loginModel.Input = new EasyTrade_Crypto.Pages.LoginModel.InputModel 
            { 
                Email = "test@example.com", 
                Password = "correctPassword123" 
            };
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
            var result = await _loginModel.OnPostAsync(invalidReturnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/TradingView", redirectResult.PageName);
            VerifyLog(LogLevel.Warning, "Invalid non-local returnUrl", Times.Once());
        }
    }
}