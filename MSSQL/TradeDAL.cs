using System;
using System.Data;
using System.Threading.Tasks;
using EasyTrade_Crypto.Interfaces;
using Microsoft.Data.SqlClient;

namespace MSSQL
{
    public class TradeDAL : ITradeDAL
    {
        private readonly string _connectionString;
        public TradeDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity)
        {
            using var conn = new SqlConnection(_connectionString);
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

                // Commit
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