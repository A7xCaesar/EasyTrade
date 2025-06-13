using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using EasyTrade_Crypto.DTO;
using EasyTrade_Crypto.Interfaces;
using Microsoft.Data.SqlClient;
using MSSQL;

namespace EasyTrade_Crypto.DAL.MSSQL
{
    public class PortfolioDAL : IPortfolioDAL
    {
        private readonly IDbConnectionStringProvider _connectionProvider;

        public PortfolioDAL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<List<AssetBalanceDTO>> GetAssetBalancesAsync(string userId)
        {
            var assetBalances = new List<AssetBalanceDTO>();
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                
                // Get all balances from the balances table with current prices
                var command = new SqlCommand(@"
                    SELECT 
                        b.assetId,
                        a.symbol,
                        a.name,
                        b.amount,
                        ISNULL(ph.price, 1.00) as currentPrice,
                        (b.amount * ISNULL(ph.price, 1.00)) as totalValue
                    FROM balances b
                    LEFT JOIN assets a ON b.assetId = a.assetId
                    LEFT JOIN (
                        SELECT 
                            assetId, 
                            price,
                            ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                        FROM priceHistory
                    ) ph ON b.assetId = ph.assetId AND ph.rn = 1
                    WHERE b.userId = @UserId AND b.amount > 0
                    ORDER BY totalValue DESC", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var assetBalance = new AssetBalanceDTO
                        {
                            AssetId = reader["assetId"]?.ToString() ?? "UNKNOWN",
                            Symbol = reader["symbol"]?.ToString() ?? "UNKNOWN",
                            Name = reader["name"]?.ToString() ?? "Unknown Asset",
                            Amount = reader.GetDecimal("amount"),
                            CurrentPrice = reader.GetDecimal("currentPrice"),
                            TotalValue = reader.GetDecimal("totalValue")
                        };
                        assetBalances.Add(assetBalance);
                    }
                }
            }
            return assetBalances;
        }

        public async Task<decimal> GetLatestPriceAsync(string assetId)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT TOP 1 price 
                    FROM priceHistory 
                    WHERE assetId = @AssetId 
                    ORDER BY timestamp DESC", connection);
                command.Parameters.AddWithValue("@AssetId", assetId);
                
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                return 0;
            }
        }

        public async Task<List<TradeDTO>> GetTradeHistoryAsync(string userId)
        {
            var trades = new List<TradeDTO>();
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT 
                        t.tradeId,
                        t.userId,
                        t.assetId,
                        ISNULL(a.symbol, 'UNKNOWN') as symbol,
                        t.tradeType,
                        t.quantity,
                        t.price,
                        (t.quantity * t.price) as totalValue,
                        t.timestamp
                    FROM trades t
                    LEFT JOIN assets a ON t.assetId = a.assetId
                    WHERE t.userId = @UserId
                    ORDER BY t.timestamp DESC", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var trade = new TradeDTO
                        {
                            TradeId = reader.GetString("tradeId"),
                            UserId = userId,
                            AssetId = reader.GetString("assetId"),
                            Symbol = reader.GetString("symbol"),
                            Type = reader.GetString("tradeType"),
                            Quantity = reader.GetDecimal("quantity"),
                            Price = reader.GetDecimal("price"),
                            TotalValue = reader.GetDecimal("totalValue"),
                            Timestamp = reader.GetDateTime("timestamp")
                        };
                        trades.Add(trade);
                    }
                }
            }
            return trades;
        }

        public async Task<string> GetSymbolByAssetIdAsync(string assetId)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT symbol 
                    FROM assets 
                    WHERE assetId = @AssetId", connection);
                command.Parameters.AddWithValue("@AssetId", assetId);
                
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
                return string.Empty;
            }
        }

        public async Task<decimal> GetTotalPortfolioValueAsync(string userId)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT 
                        ISNULL(SUM(b.amount * ISNULL(ph.price, 1.00)), 0) as totalPortfolioValue
                    FROM balances b
                    LEFT JOIN (
                        SELECT 
                            assetId, 
                            price,
                            ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                        FROM priceHistory
                    ) ph ON b.assetId = ph.assetId AND ph.rn = 1
                    WHERE b.userId = @UserId AND b.amount > 0", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                return 0m;
            }
        }

        // Additional method to get available cash balance
        public async Task<decimal> GetAvailableCashBalanceAsync(string userId)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT 
                        (10000.00 + ISNULL(SUM(CASE 
                            WHEN t.tradeType = 'sell' THEN (t.quantity * t.price)
                            WHEN t.tradeType = 'buy' THEN -(t.quantity * t.price)
                            ELSE 0
                        END), 0)) as availableBalance
                    FROM trades t
                    WHERE t.userId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
                return 10000.00m; // Default initial balance
            }
        }

        // Method to get portfolio profit/loss
        public async Task<List<ProfitLossDTO>> GetProfitLossAsync(string userId)
        {
            var profitLossList = new List<ProfitLossDTO>();
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    WITH TradeSummary AS (
                        SELECT 
                            t.assetId,
                            SUM(CASE WHEN t.tradeType = 'buy' THEN t.quantity ELSE -t.quantity END) as netQuantity,
                            SUM(CASE WHEN t.tradeType = 'buy' THEN (t.quantity * t.price) ELSE -(t.quantity * t.price) END) as netInvestment
                        FROM trades t
                        WHERE t.userId = @UserId
                        GROUP BY t.assetId
                        HAVING SUM(CASE WHEN t.tradeType = 'buy' THEN t.quantity ELSE -t.quantity END) > 0
                    ),
                    CurrentValues AS (
                        SELECT 
                            ts.assetId,
                            ts.netQuantity,
                            ts.netInvestment,
                            ISNULL(ph.price, 0) as currentPrice,
                            (ts.netQuantity * ISNULL(ph.price, 0)) as currentValue
                        FROM TradeSummary ts
                        LEFT JOIN (
                            SELECT 
                                assetId, 
                                price,
                                ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                            FROM priceHistory
                        ) ph ON ts.assetId = ph.assetId AND ph.rn = 1
                    )
                    SELECT 
                        cv.assetId,
                        a.symbol,
                        a.name,
                        cv.netQuantity,
                        cv.netInvestment,
                        cv.currentValue,
                        (cv.currentValue - cv.netInvestment) as profitLoss,
                        CASE 
                            WHEN cv.netInvestment > 0 THEN ((cv.currentValue - cv.netInvestment) / cv.netInvestment) * 100
                            ELSE 0
                        END as profitLossPercentage
                    FROM CurrentValues cv
                    INNER JOIN assets a ON cv.assetId = a.assetId
                    ORDER BY profitLoss DESC", connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var profitLoss = new ProfitLossDTO
                        {
                            AssetId = reader.GetString("assetId"),
                            Symbol = reader.GetString("symbol"),
                            Name = reader.GetString("name"),
                            NetQuantity = reader.GetDecimal("netQuantity"),
                            NetInvestment = reader.GetDecimal("netInvestment"),
                            CurrentValue = reader.GetDecimal("currentValue"),
                            ProfitLoss = reader.GetDecimal("profitLoss"),
                            ProfitLossPercentage = reader.GetDecimal("profitLossPercentage")
                        };
                        profitLossList.Add(profitLoss);
                    }
                }
            }
            return profitLossList;
        }
    }

    // Additional DTO for profit/loss information
    public class ProfitLossDTO
    {
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal NetQuantity { get; set; }
        public decimal NetInvestment { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }
} 