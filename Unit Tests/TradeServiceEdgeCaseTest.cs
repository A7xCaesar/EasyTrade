using System.Threading.Tasks;
using Moq;
using Xunit;
using EasyTrade_Crypto.Services;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Tests.Services
{
    public class TradeServiceEdgeCaseTest
    {
        [Fact]
        public async Task ExecuteTradeAsync_NegativeQuantity_ReturnsValidationErrorAndDoesNotCallDal()
        {
            // Arrange – single mock instance
            var mockDal = new Mock<ITradeDAL>();
            var service = new TradeService(mockDal.Object);

            // Act – attempt to trade with a negative quantity (edge case)
            var (success, error) = await service.ExecuteTradeAsync("user1", "BTC", "buy", -5m);

            // Assert – service should reject the trade, DAL should never be invoked
            Assert.False(success);
            Assert.Equal("Quantity must be greater than zero.", error);
            mockDal.Verify(d => d.ExecuteTradeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        }
    }
} 