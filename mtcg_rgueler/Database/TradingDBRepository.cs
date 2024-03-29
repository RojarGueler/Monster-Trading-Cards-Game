﻿using Npgsql;
using rgueler_mtcg.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace rgueler_mtcg.Database
{
    public class TradingDBRepository
    {
        private string connectionString = "Host=localhost;Username=postgres;Password=Pass2020!;Database=MTCG_DB";

        public TradingDBRepository() { }

        public string GetTrade()
        {
            List<Trade> trades = new List<Trade>();

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT id, cardtotrade, card_type, minimumdamage, username FROM tradings;";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Trade serializedTrade = MapTradeFromReader(reader);
                                trades.Add(serializedTrade);
                            }
                        }
                    }

                    connection.Close();
                }

                string jsonResult = JsonSerializer.Serialize(trades, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(jsonResult);

                return jsonResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting trades: {ex.Message}");
                return "[]"; // Return an empty array in case of an error
            }
        }
        private Trade MapTradeFromReader(NpgsqlDataReader reader)
        {
            return new Trade
            {
                Id = reader.GetString(0),
                CardToTrade = reader.GetString(1),
                Type = reader.GetString(2),
                MinimumDamage = reader.GetDouble(3)
            };
        }

        public bool DoesUserHaveCard(string username, string cardId)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string sql = "SELECT COUNT(*) FROM user_packages " +
                                   "INNER JOIN cards ON user_packages.package_id = cards.package_id " +
                                   "WHERE user_packages.username = @username AND cards.id = @cardId;";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@cardId", cardId);

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if user has card: {ex.Message}");
                return false;
            }
        }
        
        public bool DoesCardExistInTrading(string cardId)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT id FROM tradings WHERE cardtotrade = @cardId;";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@cardId", cardId);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if card exists in trading: {ex.Message}");
                throw;
            }
        }

        public void AddTrade(string cardToTrade, string Id, string cardType, double minimumDamage, string username)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string sql = "INSERT INTO tradings (id, cardtotrade, card_type, minimumdamage, username) " +
                                   "VALUES (@Id, @cardToTrade, @cardType, @minimumDamage, @username);";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        command.Parameters.AddWithValue("@cardToTrade", cardToTrade);
                        command.Parameters.AddWithValue("@cardType", cardType);
                        command.Parameters.AddWithValue("@minimumDamage", minimumDamage);
                        command.Parameters.AddWithValue("@username", username);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding trade: {ex.Message}");
                throw;
            }
        }
        public bool DoesIdExist(string id)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT id FROM tradings WHERE id = @Id;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    bool exists = false;

                    try
                    {
                        exists = command.ExecuteScalar() != null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking if ID exists: {ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                    }

                    return exists;
                }
            }
        }
        public bool UserID(string id, string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT id FROM tradings WHERE id = @id AND username = @username;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@username", username);

                    bool belongsToUser = false;

                    try
                    {
                        belongsToUser = command.ExecuteScalar() != null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking if ID belongs to user: {ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                    }

                    return belongsToUser;
                }
            }
        }
        public void DeleteTradeById(string tradeId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM tradings WHERE id = @tradeId;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@tradeId", tradeId);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting trade by ID: {ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }
        public string GetCardToTradeById(string tradingId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT cardtotrade FROM tradings WHERE id = @tradingId;";
                string cardToTrade = null;

                try
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@tradingId", tradingId);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                cardToTrade = reader.GetString(0);
                            }
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }

                return cardToTrade;
            }
        }
        public bool CheckIfDamageIsEnough(string cardId, string tradingId)
        {
            double cardDamage = GetCardDamage(cardId);
            double minimumDamageInOffer = GetMinimumDamageInOffer(tradingId);

            return cardDamage >= minimumDamageInOffer;
        }

        private double GetCardDamage(string cardId)
        {
            return GetDoubleValueFromDatabase("SELECT damage FROM cards WHERE id = @cardId;", "@cardId", cardId);
        }

        private double GetMinimumDamageInOffer(string tradingId)
        {
            return GetDoubleValueFromDatabase("SELECT minimumdamage FROM tradings WHERE id = @tradingId;", "@tradingId", tradingId);
        }

        private double GetDoubleValueFromDatabase(string sql, string parameterName, string parameterValue)
        {
            double result = 0.0;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue(parameterName, parameterValue);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = reader.GetDouble(0);
                            }
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }

        public int GetPackageIdFromCardId(string cardId)
        {
            return GetIntValueFromDatabase("SELECT package_id FROM cards WHERE id = @cardId;", "@cardId", cardId);
        }

        public bool UpdatePackageIdForCard(string cardIdToUpdate, int newPackageId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "UPDATE cards SET package_id = @newPackageId WHERE id = @cardIdToUpdate;";
                try
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@cardIdToUpdate", cardIdToUpdate);
                        command.Parameters.AddWithValue("@newPackageId", newPackageId);

                        int rowsAffected = command.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private int GetIntValueFromDatabase(string sql, string parameterName, string parameterValue)
        {
            int result = 0;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue(parameterName, parameterValue);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = reader.GetInt32(0);
                            }
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }

    }
}
