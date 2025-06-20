using Xunit;
using Moq;
using EasyTrade_Crypto.Services;
using DTO;
using Interfaces;
using Microsoft.Extensions.Logging; 
using System;                    

namespace EasyTrade_Crypto.Tests.Services
{
    public class RegistrationServiceTests
    {
        private readonly Mock<IAccountManager> _mockAccountManager;
        private readonly Mock<ILogger<RegistrationService>> _mockLogger; 
        private readonly RegistrationService _service;

        public RegistrationServiceTests()
        {
            _mockAccountManager = new Mock<IAccountManager>();
            _mockLogger = new Mock<ILogger<RegistrationService>>(); // Initialize the logger mock

            // Pass both mocked objects to the service constructor
            _service = new RegistrationService(_mockAccountManager.Object, _mockLogger.Object);
        }

        [Fact]
        public void RegisterUser_ValidInput_ReturnsTrueAndNoError()
        {
            // Arrange
            var input = new RegisterInputModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "password123",
                ConfirmPassword = "password123"
            };

            _mockAccountManager
                .Setup(am => am.RegisterUser(It.IsAny<RegisterDTO>(), out It.Ref<string>.IsAny))
                .Returns(true)
                .Callback((RegisterDTO dto, out string errorMessage) => { errorMessage = string.Empty; });

            // Act
            var result = _service.RegisterUser(input, out var actualErrorMessage);

            // Assert
            Assert.True(result);
            Assert.Equal(string.Empty, actualErrorMessage);
            _mockAccountManager.Verify(am => am.RegisterUser(It.IsAny<RegisterDTO>(), out It.Ref<string>.IsAny), Times.Once);

    
            VerifyLog(LogLevel.Information, "Successfully registered user", Times.Once());
        }

        [Fact]
        public void RegisterUser_AccountManagerFails_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var input = new RegisterInputModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "password123",
                ConfirmPassword = "password123"
            };
            string expectedErrorFromManager = "Manager failed to register user";

            _mockAccountManager
                .Setup(am => am.RegisterUser(It.IsAny<RegisterDTO>(), out It.Ref<string>.IsAny))
                .Returns(false)
                .Callback((RegisterDTO dto, out string errorMessage) => { errorMessage = expectedErrorFromManager; });

            // Act
            var result = _service.RegisterUser(input, out var actualErrorMessage);

            // Assert
            Assert.False(result);
            Assert.Equal(expectedErrorFromManager, actualErrorMessage);
            _mockAccountManager.Verify(am => am.RegisterUser(It.IsAny<RegisterDTO>(), out It.Ref<string>.IsAny), Times.Once);

            // Verify log
            VerifyLog(LogLevel.Warning, "Account manager failed to register", Times.Once());
        }

        
 
       
        private void VerifyLog(LogLevel expectedLevel, string expectedMessage, Times times)
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