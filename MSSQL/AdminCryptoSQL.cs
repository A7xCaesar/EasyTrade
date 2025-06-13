using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using DTO;
using EasyTrade_Crypto.Interfaces;
using System.Data;

namespace MSSQL
{
    public class AdminCryptoSQL : IAdminCryptoDAL
    {
        private readonly IDbConnectionStringProvider _connectionProvider;

        public AdminCryptoSQL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        private static string GenerateShortUniqueId()
        {
            return Guid.NewGuid().ToString().Substring(0, 8);
        }

        public async Task<List<CryptoDTO>> GetAllCryptocurrenciesAsync()
        {
            var cryptocurrencies = new List<CryptoDTO>();
            
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT 
                        a.assetId,
                        a.symbol,
                        a.name,
                        ISNULL(ph.price, 0) as currentPrice,
                        ISNULL(ph.timestamp, GETDATE()) as lastUpdated
                    FROM assets a
                    LEFT JOIN (
                        SELECT 
                            assetId, 
                            price,
                            timestamp,
                            ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                        FROM priceHistory
                    ) ph ON a.assetId = ph.assetId AND ph.rn = 1
                    ORDER BY a.symbol", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var crypto = new CryptoDTO
                        {
                            AssetId = reader["assetId"].ToString(),
                            Symbol = reader["symbol"].ToString(),
                            Name = reader["name"].ToString(),
                            CurrentPrice = Convert.ToDecimal(reader["currentPrice"]),
                            LastUpdated = Convert.ToDateTime(reader["lastUpdated"]),
                            IsActive = true // Default to true since we're not using this column
                        };
                        cryptocurrencies.Add(crypto);
                    }
                }
            }
            return cryptocurrencies;
        }

        public async Task<AdminCryptoResult> AddCryptocurrencyAsync(AddCryptoDTO crypto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Check if symbol already exists
                    if (await CryptocurrencyExistsAsync(crypto.Symbol))
                    {
                        return AdminCryptoResult.CreateError("A cryptocurrency with this symbol already exists.");
                    }

                    var assetId = GenerateShortUniqueId();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert into assets table (basic schema)
                            var insertAssetCommand = new SqlCommand(@"
                                INSERT INTO assets (assetId, symbol, name)
                                VALUES (@assetId, @symbol, @name)", connection, transaction);
                            
                            insertAssetCommand.Parameters.AddWithValue("@assetId", assetId);
                            insertAssetCommand.Parameters.AddWithValue("@symbol", crypto.Symbol.ToUpper());
                            insertAssetCommand.Parameters.AddWithValue("@name", crypto.Name);

                            await insertAssetCommand.ExecuteNonQueryAsync();

                            // Insert initial price into priceHistory
                            var insertPriceCommand = new SqlCommand(@"
                                INSERT INTO priceHistory (priceId, assetId, price, timestamp)
                                VALUES (@priceId, @assetId, @price, @timestamp)", connection, transaction);
                            
                            insertPriceCommand.Parameters.AddWithValue("@priceId", GenerateShortUniqueId());
                            insertPriceCommand.Parameters.AddWithValue("@assetId", assetId);
                            insertPriceCommand.Parameters.AddWithValue("@price", crypto.InitialPrice);
                            insertPriceCommand.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);

                            await insertPriceCommand.ExecuteNonQueryAsync();

                            transaction.Commit();
                            return AdminCryptoResult.CreateSuccess();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return AdminCryptoResult.CreateError($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return AdminCryptoResult.CreateError($"An error occurred: {ex.Message}");
            }
        }

        public async Task<AdminCryptoResult> RemoveCryptocurrencyAsync(string assetId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check if cryptocurrency has any user balances
                            var checkBalancesCommand = new SqlCommand(@"
                                SELECT COUNT(*) FROM balances 
                                WHERE assetId = @assetId AND amount > 0", connection, transaction);
                            checkBalancesCommand.Parameters.AddWithValue("@assetId", assetId);
                            
                            var balanceCount = (int)await checkBalancesCommand.ExecuteScalarAsync();
                            if (balanceCount > 0)
                            {
                                return AdminCryptoResult.CreateError("Cannot remove cryptocurrency that users currently hold in their portfolios.");
                            }

                            // Delete from priceHistory first (foreign key constraint)
                            var deletePriceHistoryCommand = new SqlCommand(@"
                                DELETE FROM priceHistory WHERE assetId = @assetId", connection, transaction);
                            deletePriceHistoryCommand.Parameters.AddWithValue("@assetId", assetId);
                            await deletePriceHistoryCommand.ExecuteNonQueryAsync();

                            // Delete any zero balances
                            var deleteZeroBalancesCommand = new SqlCommand(@"
                                DELETE FROM balances WHERE assetId = @assetId", connection, transaction);
                            deleteZeroBalancesCommand.Parameters.AddWithValue("@assetId", assetId);
                            await deleteZeroBalancesCommand.ExecuteNonQueryAsync();

                            // Delete from assets table
                            var deleteAssetCommand = new SqlCommand(@"
                                DELETE FROM assets WHERE assetId = @assetId", connection, transaction);
                            deleteAssetCommand.Parameters.AddWithValue("@assetId", assetId);
                            
                            var rowsAffected = await deleteAssetCommand.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                return AdminCryptoResult.CreateError("Cryptocurrency not found.");
                            }

                            transaction.Commit();
                            return AdminCryptoResult.CreateSuccess();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return AdminCryptoResult.CreateError($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return AdminCryptoResult.CreateError($"An error occurred: {ex.Message}");
            }
        }

        public async Task<AdminCryptoResult> UpdateCryptocurrencyAsync(CryptoDTO crypto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update assets table (symbol and name)
                            var updateAssetCommand = new SqlCommand(@"
                                UPDATE assets 
                                SET symbol = @symbol, name = @name
                                WHERE assetId = @assetId", connection, transaction);
                            
                            updateAssetCommand.Parameters.AddWithValue("@assetId", crypto.AssetId);
                            updateAssetCommand.Parameters.AddWithValue("@symbol", crypto.Symbol.ToUpper());
                            updateAssetCommand.Parameters.AddWithValue("@name", crypto.Name);

                            var rowsAffected = await updateAssetCommand.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                return AdminCryptoResult.CreateError("Cryptocurrency not found.");
                            }

                            // Check if price needs to be updated
                            if (crypto.CurrentPrice > 0)
                            {
                                // Get the current price to check if it's different
                                var getCurrentPriceCommand = new SqlCommand(@"
                                    SELECT TOP 1 price 
                                    FROM priceHistory 
                                    WHERE assetId = @assetId 
                                    ORDER BY timestamp DESC", connection, transaction);
                                getCurrentPriceCommand.Parameters.AddWithValue("@assetId", crypto.AssetId);
                                
                                var currentPriceResult = await getCurrentPriceCommand.ExecuteScalarAsync();
                                var currentPrice = currentPriceResult != null ? Convert.ToDecimal(currentPriceResult) : 0m;

                                // Only add new price entry if the price has changed
                                if (Math.Abs(crypto.CurrentPrice - currentPrice) > 0.001m) // Using small tolerance for decimal comparison
                                {
                                    var insertPriceCommand = new SqlCommand(@"
                                        INSERT INTO priceHistory (priceId, assetId, price, timestamp)
                                        VALUES (@priceId, @assetId, @price, @timestamp)", connection, transaction);
                                    
                                    insertPriceCommand.Parameters.AddWithValue("@priceId", GenerateShortUniqueId());
                                    insertPriceCommand.Parameters.AddWithValue("@assetId", crypto.AssetId);
                                    insertPriceCommand.Parameters.AddWithValue("@price", crypto.CurrentPrice);
                                    insertPriceCommand.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);

                                    await insertPriceCommand.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return AdminCryptoResult.CreateSuccess();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return AdminCryptoResult.CreateError($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return AdminCryptoResult.CreateError($"An error occurred: {ex.Message}");
            }
        }

        public async Task<CryptoDTO> GetCryptocurrencyByIdAsync(string assetId)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT 
                        a.assetId,
                        a.symbol,
                        a.name,
                        ISNULL(ph.price, 0) as currentPrice,
                        ISNULL(ph.timestamp, GETDATE()) as lastUpdated
                    FROM assets a
                    LEFT JOIN (
                        SELECT 
                            assetId, 
                            price,
                            timestamp,
                            ROW_NUMBER() OVER (PARTITION BY assetId ORDER BY timestamp DESC) as rn
                        FROM priceHistory
                    ) ph ON a.assetId = ph.assetId AND ph.rn = 1
                    WHERE a.assetId = @assetId", connection);
                command.Parameters.AddWithValue("@assetId", assetId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new CryptoDTO
                        {
                            AssetId = reader["assetId"].ToString(),
                            Symbol = reader["symbol"].ToString(),
                            Name = reader["name"].ToString(),
                            CurrentPrice = Convert.ToDecimal(reader["currentPrice"]),
                            LastUpdated = Convert.ToDateTime(reader["lastUpdated"]),
                            IsActive = true // Default to true
                        };
                    }
                }
            }
            return null;
        }

        public async Task<AdminCryptoResult> SetCryptocurrencyStatusAsync(string assetId, bool isActive)
        {
            // Since we're not using isActive column, just return success
            return AdminCryptoResult.CreateSuccess();
        }

        public async Task<bool> CryptocurrencyExistsAsync(string symbol)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    SELECT COUNT(*) FROM assets WHERE symbol = @symbol", connection);
                command.Parameters.AddWithValue("@symbol", symbol.ToUpper());

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }

        // Helper method to add sample balances for testing
        public async Task AddSampleBalanceAsync(string userId, string assetId, decimal amount)
        {
            using (var connection = new SqlConnection(_connectionProvider.ConnectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"
                    IF EXISTS (SELECT 1 FROM balances WHERE userId = @userId AND assetId = @assetId)
                    BEGIN
                        UPDATE balances 
                        SET amount = @amount, lastUpdated = GETDATE()
                        WHERE userId = @userId AND assetId = @assetId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO balances (userId, assetId, amount, lastUpdated)
                        VALUES (@userId, @assetId, @amount, GETDATE())
                    END", connection);
                
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@assetId", assetId);
                command.Parameters.AddWithValue("@amount", amount);
                
                await command.ExecuteNonQueryAsync();
            }
        }
    }
} 