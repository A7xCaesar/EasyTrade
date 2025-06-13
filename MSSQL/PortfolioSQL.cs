using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSSQL
{
    public static class PortfolioSQL
    {
        // Get user's asset balances from balances table
        public static string GetAssetBalances = @"
            SELECT 
                b.assetId,
                ISNULL(a.symbol, 'UNKNOWN') as symbol,
                ISNULL(a.name, 'Unknown Asset') as name,
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
            ORDER BY totalValue DESC";

        // Get latest price for a specific asset
        public static string GetLatestPrice = @"
            SELECT TOP 1 price 
            FROM priceHistory 
            WHERE assetId = @AssetId 
            ORDER BY timestamp DESC";

        // Get user's trade history
        public static string GetTradeHistory = @"
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

        // Get total portfolio value from balances table
        public static string GetTotalPortfolioValue = @"
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
            WHERE b.userId = @UserId AND b.amount > 0";

        // Add or update balance for a user
        public static string UpsertBalance = @"
            IF EXISTS (SELECT 1 FROM balances WHERE userId = @UserId AND assetId = @AssetId)
            BEGIN
                UPDATE balances 
                SET amount = @Amount, lastUpdated = GETDATE()
                WHERE userId = @UserId AND assetId = @AssetId
            END
            ELSE
            BEGIN
                INSERT INTO balances (userId, assetId, amount, lastUpdated)
                VALUES (@UserId, @AssetId, @Amount, GETDATE())
            END";

        // Get specific balance for a user and asset
        public static string GetBalance = @"
            SELECT amount 
            FROM balances 
            WHERE userId = @UserId AND assetId = @AssetId";

        // Delete balance if amount is zero or negative
        public static string CleanupZeroBalances = @"
            DELETE FROM balances 
            WHERE userId = @UserId AND amount <= 0";
    }
}
