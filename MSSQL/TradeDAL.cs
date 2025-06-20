// In folder: MSSQL/TradeDAL.cs

using System;
using System.Data;
using System.Threading.Tasks;
using EasyTrade_Crypto.Interfaces;
using Microsoft.Data.SqlClient;

namespace MSSQL
{
    public class TradeDAL : ITradeDAL
    {
        private readonly IDbConnectionStringProvider _connectionProvider;

        
        private const string EnsureEurAssetExistsSql = @"
            IF NOT EXISTS (SELECT 1 FROM assets WHERE symbol = 'EUR')
            BEGIN
                INSERT INTO assets (assetId, symbol, name) VALUES ('EUR', 'EUR', 'Euro Cash');
            END";

        private const string InitializeEurBalanceSql = @"
            IF NOT EXISTS (SELECT 1 FROM balances WHERE userId = @UserId AND assetId = 'EUR')
            BEGIN
                INSERT INTO balances (balanceId, userId, assetId, amount) VALUES (@BalanceId, @UserId, 'EUR', 10000.00);
            END";

        private const string GetBalanceSql = @"
            SELECT amount 
            FROM balances 
            WHERE userId = @UserId AND assetId = @AssetId";

        private const string UpsertBalanceSql = @"
            MERGE balances AS target
            USING (SELECT @UserId AS userId, @AssetId AS assetId) AS source
            ON (target.userId = source.userId AND target.assetId = source.assetId)
            WHEN MATCHED THEN
                UPDATE SET 
                    amount = target.amount + @Amount
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (balanceId, userId, assetId, amount)
                VALUES (@BalanceId, @UserId, @AssetId, @Amount);";

        private const string CleanupZeroBalancesSql = @"
            DELETE FROM balances 
            WHERE userId = @UserId AND amount <= 0";

        public TradeDAL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<bool> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(symbol) ||
                string.IsNullOrWhiteSpace(tradeType) || quantity <= 0)
            {
                throw new ArgumentException("Invalid trade parameters provided.");
            }

            using var conn = new SqlConnection(_connectionProvider.ConnectionString);
            await conn.OpenAsync();
            using var tran = (SqlTransaction)await conn.BeginTransactionAsync();
            try
            {
                // Logic to ensure EUR asset exists, now using the local SQL string
                using (var ensureCmd = new SqlCommand(EnsureEurAssetExistsSql, conn, tran))
                {
                    await ensureCmd.ExecuteNonQueryAsync();
                }

                // Get EUR (cash) assetId - make sure we get the actual assetId from the database
                string getCashAssetQuery = "SELECT assetId FROM assets WHERE symbol = 'EUR'";
                string cashAssetId = string.Empty;
                using (var cmd = new SqlCommand(getCashAssetQuery, conn, tran))
                {
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new InvalidOperationException("EUR asset not found in system.");
                    cashAssetId = res.ToString();
                }

                // Logic to initialize user's EUR balance, now using the actual cashAssetId
                string initBalanceQuery = @"
                    IF NOT EXISTS (SELECT 1 FROM balances WHERE userId = @UserId AND assetId = @AssetId)
                    BEGIN
                        INSERT INTO balances (balanceId, userId, assetId, amount) 
                        VALUES (@BalanceId, @UserId, @AssetId, 10000.00);
                    END";

                using (var initCmd = new SqlCommand(initBalanceQuery, conn, tran))
                {
                    initCmd.Parameters.AddWithValue("@UserId", userId);
                    initCmd.Parameters.AddWithValue("@AssetId", cashAssetId);
                    initCmd.Parameters.AddWithValue("@BalanceId", Guid.NewGuid().ToString().Substring(0, 8));
                    await initCmd.ExecuteNonQueryAsync();
                }

                // Get assetId by symbol and validate asset exists
                string getAssetQuery = "SELECT assetId FROM assets WHERE symbol = @symbol";
                var assetId = string.Empty;
                using (var cmd = new SqlCommand(getAssetQuery, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@symbol", symbol.ToUpper());
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new InvalidOperationException($"Asset '{symbol}' not found or not available for trading.");
                    assetId = res.ToString();
                }

                // Get latest price from price history
                string getPriceQuery = @"SELECT TOP 1 price FROM priceHistory WHERE assetId = @assetId ORDER BY timestamp DESC";
                decimal price = 0m;
                using (var cmd = new SqlCommand(getPriceQuery, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new InvalidOperationException($"No price data available for {symbol}. Please try again later.");
                    price = Convert.ToDecimal(res);
                }

                // Get current balances for validation
                decimal currentCryptoBalance = await GetCurrentBalance(conn, tran, userId, assetId);
                decimal currentCashBalance = await GetCurrentBalance(conn, tran, userId, cashAssetId);

                var totalCost = quantity * price;
                var tradeTypeLower = tradeType.ToLower();

                // Validate sufficient balances with detailed error messages
                if (tradeTypeLower == "buy")
                {
                    if (currentCashBalance < totalCost)
                    {
                        throw new InvalidOperationException($"Insufficient EUR balance. You need €{totalCost:F2} but only have €{currentCashBalance:F2} available.");
                    }
                }
                else if (tradeTypeLower == "sell")
                {
                    if (currentCryptoBalance < quantity)
                    {
                        throw new InvalidOperationException($"Insufficient {symbol} balance. You need {quantity:F8} {symbol} but only have {currentCryptoBalance:F8} {symbol} available.");
                    }
                    if (currentCryptoBalance <= 0)
                    {
                        throw new InvalidOperationException($"You don't own any {symbol} to sell.");
                    }
                }

                // Generate unique trade ID
                var tradeId = Guid.NewGuid().ToString();

                // Insert trade record
                var insertTradeQuery = @"
                    INSERT INTO trades (tradeId, userId, assetId, tradeType, quantity, price, timestamp)
                    VALUES (@tradeId, @userId, @assetId, @tradeType, @quantity, @price, GETUTCDATE())";

                using (var cmd = new SqlCommand(insertTradeQuery, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@tradeId", tradeId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    cmd.Parameters.AddWithValue("@tradeType", tradeTypeLower);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@price", price);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Update asset balance (only if it's not a sell that would result in zero)
                decimal assetDelta = tradeTypeLower == "buy" ? quantity : -quantity;
                await UpsertBalance(conn, tran, userId, assetId, assetDelta);

                // Update cash balance
                decimal cashDelta = tradeTypeLower == "buy" ? -totalCost : totalCost;
                await UpsertBalance(conn, tran, userId, cashAssetId, cashDelta);

                // Clean up zero balances
                await CleanupZeroBalances(conn, tran, userId);

                await tran.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Trade execution failed: {ex.Message}");
                throw;
            }
        }

        private async Task<decimal> GetCurrentBalance(SqlConnection conn, SqlTransaction tran, string userId, string assetId)
        {
            
            using var cmd = new SqlCommand(GetBalanceSql, conn, tran);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@AssetId", assetId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
        }

        private async Task UpsertBalance(SqlConnection conn, SqlTransaction tran, string userId, string assetId, decimal deltaAmount)
        {
            // First, validate that the asset exists
            string validateAssetQuery = "SELECT COUNT(1) FROM assets WHERE assetId = @AssetId";
            using (var validateCmd = new SqlCommand(validateAssetQuery, conn, tran))
            {
                validateCmd.Parameters.AddWithValue("@AssetId", assetId);
                var assetExists = Convert.ToInt32(await validateCmd.ExecuteScalarAsync()) > 0;
                if (!assetExists)
                {
                    throw new InvalidOperationException($"Cannot create balance for asset '{assetId}' - asset does not exist in system.");
                }
            }

            // Now safely upsert the balance
            const string upsertSql = @"
                MERGE balances AS target
                USING (SELECT @UserId AS userId, @AssetId AS assetId) AS source
                ON (target.userId = source.userId AND target.assetId = source.assetId)
                WHEN MATCHED THEN
                    UPDATE SET 
                        amount = target.amount + @Amount
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (balanceId, userId, assetId, amount)
                    VALUES (@BalanceId, @UserId, @AssetId, @Amount);";

            using var cmd = new SqlCommand(upsertSql, conn, tran);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@AssetId", assetId);
            cmd.Parameters.AddWithValue("@Amount", deltaAmount);
            cmd.Parameters.AddWithValue("@BalanceId", Guid.NewGuid().ToString().Substring(0, 8));
            await cmd.ExecuteNonQueryAsync();
        }

       

        private async Task CleanupZeroBalances(SqlConnection conn, SqlTransaction tran, string userId)
        {
           
            using var cmd = new SqlCommand(CleanupZeroBalancesSql, conn, tran);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}