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

        public TradeDAL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task<bool> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity)
        {
            using var conn = new SqlConnection(_connectionProvider.ConnectionString);
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();
            try
            {
                // Get assetId by symbol and latest price
                string getAssetQuery = "SELECT assetId FROM assets WHERE symbol = @symbol";
                string getPriceQuery = @"SELECT TOP 1 price FROM priceHistory WHERE assetId = @assetId ORDER BY timestamp DESC";

                var assetId = string.Empty;
                using (var cmd = new SqlCommand(getAssetQuery, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@symbol", symbol);
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new Exception("Asset not found");
                    assetId = res.ToString();
                }

                decimal price = 0m;
                using (var cmd = new SqlCommand(getPriceQuery, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new Exception("Price not found");
                    price = Convert.ToDecimal(res);
                }

                // Validate balances
                // Fetch current fiat (EUR) assetId
                string getCashAssetQuery = "SELECT assetId FROM assets WHERE symbol = 'EUR'";
                string cashAssetId = string.Empty;
                using (var cmd = new SqlCommand(getCashAssetQuery, conn, (SqlTransaction)tran))
                {
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        throw new Exception("Cash asset not found");
                    cashAssetId = res.ToString();
                }

                // Get current balances for validation
                decimal currentCryptoBalance = 0m;
                using (var cmd = new SqlCommand(PortfolioSQL.GetBalance, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AssetId", assetId);
                    var res = await cmd.ExecuteScalarAsync();
                    currentCryptoBalance = res != null ? Convert.ToDecimal(res) : 0m;
                }

                decimal currentCashBalance = 0m;
                using (var cmd = new SqlCommand(PortfolioSQL.GetBalance, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AssetId", cashAssetId);
                    var res = await cmd.ExecuteScalarAsync();
                    currentCashBalance = res != null ? Convert.ToDecimal(res) : 0m;
                }

                var cost = quantity * price;
                if (tradeType.ToLower() == "buy" && currentCashBalance < cost)
                {
                    throw new Exception("Insufficient cash balance");
                }
                if (tradeType.ToLower() == "sell" && currentCryptoBalance < quantity)
                {
                    throw new Exception("Insufficient asset balance");
                }

                // Insert into trades
                var insertTrade = @"INSERT INTO trades (tradeId, userId, assetId, tradeType, quantity, price, timestamp)
                                     VALUES (@tradeId, @userId, @assetId, @tradeType, @quantity, @price, GETDATE())";
                using (var cmd = new SqlCommand(insertTrade, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@tradeId", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    cmd.Parameters.AddWithValue("@tradeType", tradeType);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@price", price);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Update balance (buy adds, sell subtracts)
                decimal sign = tradeType.ToLower() == "buy" ? 1 : -1;
                var newAmount = sign * quantity;
                var upsert = PortfolioSQL.UpsertBalance;
                using (var cmd = new SqlCommand(upsert, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AssetId", assetId);
                    cmd.Parameters.AddWithValue("@Amount", newAmount);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Update cash balance (EUR)
                decimal cashDelta = tradeType.ToLower() == "buy" ? -cost : cost;
                using (var cmd = new SqlCommand(upsert, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@AssetId", cashAssetId);
                    cmd.Parameters.AddWithValue("@Amount", cashDelta);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Clean up zero balances rows
                using (var cmd = new SqlCommand(PortfolioSQL.CleanupZeroBalances, conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return true;
            }
            catch
            {
                await tran.RollbackAsync();
                return false;
            }
        }
    }
} 