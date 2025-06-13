using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public class LoginModelNullInputTests : LoginModelTestBase
    {
        public LoginModelNullInputTests() : base() { }

        [Fact]
        public async Task OnPostAsync_WhenInputModelIsNull_ReturnsPageAndLogsError()
        {
            // Arrange
            _loginModel.Input = null; // Simulate a null input model

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("An unexpected error occurred. Please try again.", _loginModel.ErrorMessage);
            VerifyLog(LogLevel.Error, "An unexpected error occurred", Times.Once());
            _mockAccountService.Verify(s => s.ValidateUserLogin(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<DTO.UserLoginInfoDTO?>.IsAny, out It.Ref<string>.IsAny), Times.Never);
        }
    }
}