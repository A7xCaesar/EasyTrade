using System.Threading.Tasks;
using Xunit;
using Moq;
using EasyTrade_Crypto.Services;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Tests.Services
{
    public class TradeServiceTests
    {
        [Fact]
        public async Task ExecuteTradeAsync_ValidQuantity_DalSucceeds_ReturnsSuccess()
        {
            // Arrange
            var mockDal = new Mock<ITradeDAL>();
            mockDal
                .Setup(d => d.ExecuteTradeAsync("user1", "BTC", "buy", 1m))
                .ReturnsAsync(true);

            var service = new TradeService(mockDal.Object);

            // Act
            var (success, error) = await service.ExecuteTradeAsync("user1", "BTC", "buy", 1m);

            // Assert
            Assert.True(success);
            Assert.Equal(string.Empty, error);
            mockDal.Verify(d => d.ExecuteTradeAsync("user1", "BTC", "buy", 1m), Times.Once);
        }

        [Fact]
        public async Task ExecuteTradeAsync_ValidQuantity_DalFails_ReturnsError()
        {
            // Arrange
            var mockDal = new Mock<ITradeDAL>();
            mockDal
                .Setup(d => d.ExecuteTradeAsync("user1", "ETH", "sell", 2m))
                .ReturnsAsync(false);

            var service = new TradeService(mockDal.Object);

            // Act
            var (success, error) = await service.ExecuteTradeAsync("user1", "ETH", "sell", 2m);

            // Assert
            Assert.False(success);
            Assert.Equal("Failed to execute trade.", error);
            mockDal.Verify(d => d.ExecuteTradeAsync("user1", "ETH", "sell", 2m), Times.Once);
        }

        [Fact]
        public async Task ExecuteTradeAsync_InvalidQuantity_ReturnsValidationError()
        {
            // Arrange
            var mockDal = new Mock<ITradeDAL>();
            var service = new TradeService(mockDal.Object);

            // Act
            var (success, error) = await service.ExecuteTradeAsync("user1", "ADA", "buy", 0m);

            // Assert
            Assert.False(success);
            Assert.Equal("Quantity must be greater than zero.", error);
            mockDal.Verify(d => d.ExecuteTradeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }
    }
} 