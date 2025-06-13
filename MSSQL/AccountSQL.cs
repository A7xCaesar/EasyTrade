using EasyTrade_Crypto.Interfaces; 
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace EasyTrade_Crypto.DAL.MSSQL
{
   
        public class AccountSQL : IAccountDAL
        {
            private readonly IDbConnectionStringProvider _connectionProvider;

            public AccountSQL(IDbConnectionStringProvider connectionProvider)
            {
                _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            }

            public bool GetUserCredentialsByEmail(string email, out string? userId, out string? username, out string? storedPasswordHash, out string? roleName, out string errorMessage) // <-- Add roleName
            {
                userId = null;
                username = null;
                storedPasswordHash = null;
                roleName = null;
                errorMessage = string.Empty;

               
                string sql = "SELECT userId, username, passwordHash, roleName FROM users WHERE email = @email";

                try
                {
                    using (var conn = new SqlConnection(_connectionProvider.ConnectionString))
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userId = reader.GetString(0);
                                username = reader.GetString(1);
                                storedPasswordHash = reader.GetString(2);
                                roleName = reader.GetString(3); 
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    errorMessage = $"Database Error (GetUserCredentialsByEmail): {ex.Message} (Number: {ex.Number})";
                    return false;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Unexpected Error (GetUserCredentialsByEmail): {ex.Message}";
                    return false;
                }
            }


            public bool InsertUser(string userId, string roleName, string username, string email, string passwordHash, out string errorMessage)
            {
                errorMessage = string.Empty;


                string sql = @"
                INSERT INTO users (userId, roleName, username, email, passwordHash, registrationDate)
                VALUES (@userId, @roleName, @username, @email, @passwordHash, @registrationDate)";

                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionProvider.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {

                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@roleName", roleName);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@registrationDate", DateTime.UtcNow);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                        else
                        {

                            errorMessage = "Database Error: User insertion affected 0 rows.";
                            return false;
                        }
                    }
                }

                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    errorMessage = "Database Error: Email or Username likely already exists.";

                    return false;
                }
                catch (SqlException ex)
                {
                    errorMessage = $"Database Error (InsertUser): {ex.Message} (Number: {ex.Number})";

                    return false;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Unexpected Error (InsertUser): {ex.Message}";

                    return false;
                }
            }
        }
    }