using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DTO;
using EasyTrade_Crypto.Interfaces;
using Microsoft.Data.SqlClient;

namespace MSSQL
{
    public class PortfolioDAL : IPortfolioDAL
    {
        private readonly IDbConnectionStringProvider _connectionProvider;

        public PortfolioDAL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        public async Task<List<AssetBalanceDTO>> GetAssetBalancesAsync(string userId)
        {
            var assetBalances = new List<AssetBalanceDTO>();
            const string sql = @"
                WITH LatestPrices AS (
                    SELECT 
                        assetId, 
                        price,
                        ROW_NUMBER() OVER(PARTITION BY assetId ORDER BY timestamp DESC) as rn
                    FROM priceHistory
                )
                SELECT 
                    'CASH-' + @UserId AS balanceId,
                    'CASH' AS assetId,
                    'EUR' AS symbol,
                    'Euro Cash' AS name,
                    (10000.00 + ISNULL(SUM(CASE WHEN t.tradeType = 'sell' THEN (t.quantity * t.price) WHEN t.tradeType = 'buy' THEN -(t.quantity * t.price) ELSE 0 END), 0)) as amount,
                    1.00 AS currentPrice,
                    (10000.00 + ISNULL(SUM(CASE WHEN t.tradeType = 'sell' THEN (t.quantity * t.price) WHEN t.tradeType = 'buy' THEN -(t.quantity * t.price) ELSE 0 END), 0)) as totalValue
                FROM trades t
                WHERE t.userId = @UserId
                UNION ALL
                SELECT 
                    b.balanceId,
                    b.assetId,
                    ISNULL(a.symbol, 'UNKNOWN') AS symbol,
                    ISNULL(a.name, 'Unknown Asset') AS name,
                    b.amount,
                    ISNULL(lp.price, 1.00) AS currentPrice,
                    (b.amount * ISNULL(lp.price, 1.00)) AS totalValue
                FROM 
                    balances b
                LEFT JOIN 
                    assets a ON b.assetId = a.assetId
                LEFT JOIN 
                    LatestPrices lp ON b.assetId = lp.assetId AND lp.rn = 1
                WHERE 
                    b.userId = @UserId AND b.amount > 0
                ORDER BY totalValue DESC;";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        assetBalances.Add(new AssetBalanceDTO
                        {
                            BalanceId = reader.GetString(reader.GetOrdinal("balanceId")),
                            AssetId = reader.GetString(reader.GetOrdinal("assetId")),
                            Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                            CurrentPrice = reader.GetDecimal(reader.GetOrdinal("currentPrice")),
                            TotalValue = reader.GetDecimal(reader.GetOrdinal("totalValue"))
                        });
                    }
                }
            }
            return assetBalances;
        }

        public async Task<decimal> GetTotalPortfolioValueAsync(string userId)
        {
            const string sql = @"
                WITH AssetValues AS (
                    SELECT 
                        SUM(b.amount * ISNULL(ph.price, 0)) as totalAssetValue
                    FROM balances b
                    LEFT JOIN (
                        SELECT 
                            assetId, 
                            price,
                            ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                        FROM priceHistory
                    ) ph ON b.assetId = ph.assetId AND ph.rn = 1
                    WHERE b.userId = @UserId AND b.amount > 0
                ),
                CashBalance AS (
                    SELECT 
                        (10000.00 + ISNULL(SUM(CASE 
                            WHEN t.tradeType = 'sell' THEN (t.quantity * t.price)
                            WHEN t.tradeType = 'buy' THEN -(t.quantity * t.price)
                            ELSE 0
                        END), 0)) as availableBalance
                    FROM trades t
                    WHERE t.userId = @UserId
                )
                SELECT 
                    ISNULL(av.totalAssetValue, 0) + ISNULL(cb.availableBalance, 10000.00) as totalPortfolioValue
                FROM (SELECT 1 AS join_key) dummy
                LEFT JOIN AssetValues av ON 1=1
                LEFT JOIN CashBalance cb ON 1=1";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : 10000.00m;
            }
        }

        public async Task<decimal> GetAvailableCashBalanceAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    (10000.00 + ISNULL(SUM(CASE 
                        WHEN t.tradeType = 'sell' THEN (t.quantity * t.price)
                        WHEN t.tradeType = 'buy' THEN -(t.quantity * t.price)
                        ELSE 0
                    END), 0)) as availableBalance
                FROM trades t
                WHERE t.userId = @UserId";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : 10000.00m;
            }
        }

        public async Task<List<TradeDTO>> GetTradeHistoryAsync(string userId)
        {
            var trades = new List<TradeDTO>();
            const string sql = @"
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
                ORDER BY t.timestamp DESC";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        trades.Add(new TradeDTO
                        {
                            TradeId = reader.GetString(reader.GetOrdinal("tradeId")),
                            UserId = userId,
                            AssetId = reader.GetString(reader.GetOrdinal("assetId")),
                            Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                            Type = reader.GetString(reader.GetOrdinal("tradeType")),
                            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            TotalValue = reader.GetDecimal(reader.GetOrdinal("totalValue")),
                            Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp"))
                        });
                    }
                }
            }
            return trades;
        }

        public async Task<List<ProfitLossDTO>> GetProfitLossAsync(string userId)
        {
            var profitLossList = new List<ProfitLossDTO>();
            const string sql = @"
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
                ORDER BY profitLoss DESC";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        profitLossList.Add(new ProfitLossDTO
                        {
                            AssetId = reader.GetString(reader.GetOrdinal("assetId")),
                            Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            NetQuantity = reader.GetDecimal(reader.GetOrdinal("netQuantity")),
                            NetInvestment = reader.GetDecimal(reader.GetOrdinal("netInvestment")),
                            CurrentValue = reader.GetDecimal(reader.GetOrdinal("currentValue")),
                            ProfitLoss = reader.GetDecimal(reader.GetOrdinal("profitLoss")),
                            ProfitLossPercentage = reader.GetDecimal(reader.GetOrdinal("profitLossPercentage"))
                        });
                    }
                }
            }
            return profitLossList;
        }

        public async Task<decimal> GetLatestPriceAsync(string assetId)
        {
            const string sql = @"
                SELECT TOP 1 price 
                FROM priceHistory 
                WHERE assetId = @AssetId 
                ORDER BY timestamp DESC";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AssetId", assetId);

                var result = await command.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : 0m;
            }
        }

        public async Task<decimal?> GetBalanceAsync(string userId, string assetId)
        {
            const string sql = @"
                SELECT amount 
                FROM balances 
                WHERE userId = @UserId AND assetId = @AssetId";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@AssetId", assetId);

                var result = await command.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : null;
            }
        }

        public async Task UpsertBalanceAsync(string userId, string assetId, decimal amount)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // First, validate that the asset exists
                        string validateAssetQuery = "SELECT COUNT(1) FROM assets WHERE assetId = @AssetId";
                        using (var validateCmd = new SqlCommand(validateAssetQuery, connection, transaction))
                        {
                            validateCmd.Parameters.AddWithValue("@AssetId", assetId);
                            var assetExists = Convert.ToInt32(await validateCmd.ExecuteScalarAsync()) > 0;
                            if (!assetExists)
                            {
                                throw new InvalidOperationException($"Cannot create balance for asset '{assetId}' - asset does not exist in system.");
                            }
                        }

                        // Now safely upsert the balance
                        const string sql = @"
                            MERGE balances AS target
                            USING (SELECT @UserId AS userId, @AssetId AS assetId) AS source
                            ON (target.userId = source.userId AND target.assetId = source.assetId)
                            WHEN MATCHED THEN
                                UPDATE SET 
                                    amount = target.amount + @Amount
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (balanceId, userId, assetId, amount)
                                VALUES (@BalanceId, @UserId, @AssetId, @Amount);";

                        var command = new SqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@AssetId", assetId);
                        command.Parameters.AddWithValue("@Amount", amount);
                        command.Parameters.AddWithValue("@BalanceId", Guid.NewGuid().ToString().Substring(0, 8));

                        await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<string> GetSymbolByAssetIdAsync(string assetId)
        {
            const string sql = @"
                SELECT symbol 
                FROM assets 
                WHERE assetId = @AssetId";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@AssetId", assetId);

                var result = await command.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? result.ToString() : string.Empty;
            }
        }

        public async Task CleanupZeroBalancesAsync(string userId)
        {
            const string sql = @"
                DELETE FROM balances 
                WHERE userId = @UserId AND amount <= 0";

            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}