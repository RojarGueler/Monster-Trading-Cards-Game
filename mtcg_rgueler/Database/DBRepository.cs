﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Npgsql;
using rgueler_mtcg.GameObjects;
using static System.Net.Mime.MediaTypeNames;

namespace rgueler_mtcg.Database
{
    public class DBRepository
    {
        private string connectionString = "Host=localhost;Username=postgres;Password=Pass2020!;Database=MTCG_DB";
        private User User { get; set; }

        //Counter for the uniqueID of a package
        private static int counter = 0;

        //Init Database
        public DBRepository() { }
        
        public DBRepository(User u1)
        {
            User = u1;
        }
        public void InitDB()
        {
            DropDataBaseTable("tradings");
            DropDataBaseTable("deck");
            DropDataBaseTable("user_packages");
            DropDataBaseTable("cards");
            DropDataBaseTable("packages");
            DropDataBaseTable("users");

            CreateDataBaseTable("users");
            CreateDataBaseTable("packages");
            CreateDataBaseTable("cards");
            CreateDataBaseTable("user_packages");
            CreateDataBaseTable("deck");
            CreateDataBaseTable("tradings");

            AlterTableUsersAddLastSpinDate();
        }
        private void DropDataBaseTable(string tableName)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = $"DROP TABLE IF EXISTS {tableName};";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error dropping table '{tableName}': {ex.Message}");
                    }
                }
                connection.Close();
            }
        }

        private void CreateDataBaseTable(string tableName)
        {
            string createTableSql = GetCreateTableSql(tableName);

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(createTableSql, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating table '{tableName}': {ex.Message}");
                    }
                }
                connection.Close();
            }
        }
        private string GetCreateTableSql(string tableName)
        {
            switch (tableName)
            {
                case "users":
                    return "CREATE TABLE IF NOT EXISTS users (token varchar(255) ,username VARCHAR(255) NOT NULL PRIMARY KEY ,password VARCHAR(255) NOT NULL,coins int NOT NULL ,name VARCHAR(255) ,bio VARCHAR(255) ,image VARCHAR(255) ,elo int NOT NULL ,wins int NOT NULL ,losses int NOT NULL);";
                case "packages":
                    return "CREATE TABLE IF NOT EXISTS packages (package_id SERIAL PRIMARY KEY, bought BOOLEAN NOT NULL);";
                case "cards":
                    return "CREATE TABLE IF NOT EXISTS cards (id VARCHAR(255) PRIMARY KEY, name VARCHAR(255) NOT NULL, damage DOUBLE PRECISION NOT NULL, package_id INTEGER REFERENCES packages(package_id));";
                case "user_packages":
                    return "CREATE TABLE IF NOT EXISTS user_packages (username VARCHAR(255) REFERENCES users(username), package_id INT REFERENCES packages(package_id), PRIMARY KEY (username, package_id));";
                case "deck":
                    return "CREATE TABLE IF NOT EXISTS deck (username VARCHAR(255) REFERENCES users(username),card_id VARCHAR(255) REFERENCES cards(id),PRIMARY KEY (username, card_id));";
                case "tradings":
                    return "CREATE TABLE IF NOT EXISTS tradings (id VARCHAR(255) PRIMARY KEY, cardtotrade VARCHAR(255) NOT NULL, card_type VARCHAR(255), MinimumDamage double PRECISION, username VARCHAR(255) REFERENCES users(username));";
                default:
                    Console.WriteLine($"Table '{tableName}' not supported.");
                    return null;
            }
        }
        public void AddUser()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO users (token, username, password, coins, elo, wins, losses) VALUES (@token, @username, @password, @coins, 0, 0, 0)";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    string token = User.Username + "-mtcgToken";
                    
                    command.Parameters.AddWithValue("@token", token);
                    command.Parameters.AddWithValue("@username", User.Username);
                    command.Parameters.AddWithValue("@password", HashPassword(User.Password));
                    command.Parameters.AddWithValue("@coins", 20);

                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }
        // Check if the username exists in the database
        public bool IsUserLoggedIn(string username, string password)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", HashPassword(password));

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    connection.Close();
                    return count > 0;
                }
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
        //Check if there are duplicate cards in the database
        public bool CheckDuplicateCards(List<Card> cards)
        {

            // Extract the card IDs from the list
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                foreach (Card card in cards)
                {
                    string cardi = card.Id;
                    string sql = $"SELECT COUNT(*) FROM cards WHERE id = @card";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@card", card.Id);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        // If count is greater than 1, the card ID is a duplicate
                        if (count >= 1)
                        {
                            Console.WriteLine($"Duplicate card ID found: {card.Id}");
                            return true;
                        }
                    }
                }
            }
            // No duplicates found
            return false;
        }

        //Create unique ID for package
        public int CreatePackageID()
        {
            counter++;
            return counter;
        }

        public void AddPackage(Package package)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Persist the package and associated cards
                        SavePackageAndCards(connection, transaction, package);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving package and cards: {ex.Message}");
                        transaction.Rollback();
                    }
                }

                connection.Close();
            }
        }

        public void SavePackageAndCards(NpgsqlConnection connection, NpgsqlTransaction transaction, Package package)
        {
            SavePackage(connection, transaction, package);

            foreach (var card in package.Cards)
            {
                SaveCard(connection, transaction, card, package.PackageId);
            }
        }

        private void SavePackage(NpgsqlConnection connection, NpgsqlTransaction transaction, Package package)
        {
            string sql = "INSERT INTO packages (package_id, bought) VALUES (@package_id, @bought)";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@package_id", package.PackageId);
                command.Parameters.AddWithValue("@bought", package.Bought);

                command.ExecuteNonQuery();
            }
        }

        private void SaveCard(NpgsqlConnection connection, NpgsqlTransaction transaction, Card card, int packageId)
        {
            string sql = "INSERT INTO cards (id, name, damage, package_id) VALUES (@Id, @name, @damage, @packageId)";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", card.Id);
                command.Parameters.AddWithValue("@name", card.Name);
                command.Parameters.AddWithValue("@damage", card.Damage);
                command.Parameters.AddWithValue("@packageId", packageId);

                command.ExecuteNonQuery();
            }
        }
        public int GetCoins(string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT coins FROM users WHERE username = @username;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int storedCoins = reader.GetInt32(0);
                            connection.Close();
                            return storedCoins;
                        }
                    }
                    connection.Close();
                    return -1;
                }
            }
        }
        public string GetUserName(string token)
        {
            string username = "";
            int indexOfHyphen = token.IndexOf('-');

            if (indexOfHyphen != -1) username = token.Substring(0, indexOfHyphen);

            return username;
        }
        public List<int> IsAnyPackageAvailable()
        {
            List<int> packageList = new List<int>();

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT package_id FROM packages WHERE bought = false;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int packageCount = Convert.ToInt32(reader[0]);
                            packageList.Add(packageCount);
                            Console.WriteLine("Package Nummerierung: " + packageCount);
                        }
                    }
                }

                connection.Close();
            }
            
            return packageList;
        }
        public void AcquirePackage(int packageId, string username)
        {
            Console.WriteLine("PackageID: " + packageId);
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO user_packages (username, package_id) VALUES (@username, @packageId)";
                string sqlUpdate = "UPDATE packages SET bought = true WHERE package_id = @packageId";
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Log relevant information
                        Console.WriteLine($"Acquiring package {packageId} for user {username}");

                        // Insert into the user_packages table
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@username", username);
                            command.Parameters.AddWithValue("@packageId", packageId);

                            command.ExecuteNonQuery();
                        }

                        // Update the bought status in the packages table
                        using (NpgsqlCommand updateCommand = new NpgsqlCommand(sqlUpdate, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@packageId", packageId);
                            updateCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Console.WriteLine($"Package {packageId} acquired successfully by {username}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error acquiring package: {ex.Message}");
                        transaction.Rollback();
                    }
                }
            }
        }

        public void UpdateCoinsByPackage(string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT coins FROM users WHERE username = @username";
                string sqlCoins = "UPDATE users SET coins = coins - 5 WHERE username = @username";
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int userCoins = 0;
                        using (NpgsqlCommand getUserCoinsCommand = new NpgsqlCommand(sql, connection, transaction))
                        {
                            getUserCoinsCommand.Parameters.AddWithValue("@username", username);
                            var result = getUserCoinsCommand.ExecuteScalar();
                            if (result != null)
                            {
                                userCoins = Convert.ToInt32(result);
                            }
                        }

                        if (userCoins >= 5)
                        {
                            using (NpgsqlCommand updateCoinsCommand = new NpgsqlCommand(sqlCoins, connection, transaction))
                            {
                                updateCoinsCommand.Parameters.AddWithValue("@username", username);
                                updateCoinsCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            Console.WriteLine($"Coins updated successfully for {username}.");
                        }
                        else
                        {
                            Console.WriteLine($"Not enough coins for {username}.");
                            transaction.Rollback();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating coins: {ex.Message}");
                        transaction.Rollback();
                    }
                }
            }
        }
        //Checks if there is a token
        public bool DoesTokenExist(string token)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT COUNT(*) FROM users WHERE token = @token";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    int count = 0;
                    command.Parameters.AddWithValue("@token", token);
                    if (token != null) count = Int32.Parse(command.ExecuteScalar().ToString());
                    
                    connection.Close();
                    return count > 0;
                }
            }
        }

        public string GetUserCardsJSON(string token)
        {
            using (
                NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT cards.id, cards.name, cards.damage " +
                             "FROM users " +
                             "JOIN user_packages ON users.username = user_packages.username " +
                             "JOIN cards ON user_packages.package_id = cards.package_id " +
                             "WHERE users.username = @username;";
                string username = GetUserName(token);

                if (!string.IsNullOrEmpty(username))
                {
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);

                        List<string> cardsJson = new List<string>();

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string cardId = reader.GetString(0);
                                string cardName = reader.GetString(1);
                                double cardDamage = reader.GetDouble(2);

                                string cardJson = $"{{\"id\":\"{cardId}\",\"name\":\"{cardName}\",\"damage\":{cardDamage}}}";
                                cardsJson.Add(cardJson);
                            }
                        }

                        connection.Close();

                        // Debug output
                        Console.WriteLine($"User: {username}");
                        Console.WriteLine($"Number of Cards: {cardsJson.Count}");
                        Console.WriteLine($"Card JSON: {string.Join(",", cardsJson)}");

                        // Join the individual card JSON strings into a single JSON array
                        return $"[{string.Join(",", cardsJson)}]";
                    }
                }
                connection.Close();
                return null;
            }
        }
        public string GetCardsFromDeck(string token, bool plainFormat)
        {
            List<Card> userDeck = new List<Card>();
            StringBuilder plainTextBuilder = new StringBuilder();
            string ergebnis_plainText = "";
            string ergebnis_Text = "";

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("token: " + token);

                if (!string.IsNullOrEmpty(token))
                {
                    string sql = $"SELECT cards.id, cards.name, cards.damage " +
                                   "FROM users " +
                                   "JOIN user_packages ON users.username = user_packages.username " +
                                   "JOIN cards ON user_packages.package_id = cards.package_id " +
                                   "JOIN deck ON users.username = deck.username AND cards.id = deck.card_id " +
                                   "WHERE users.token = @token;";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@token", token);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Card card = new Card
                                {
                                    Id = reader.GetString(0),
                                    Name = reader.GetString(1),
                                    Damage = reader.GetDouble(2)
                                };
                                userDeck.Add(card);
                                if (plainFormat)
                                {
                                    plainTextBuilder.AppendLine($"Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}");
                                    ergebnis_plainText += plainTextBuilder.ToString();
                                }
                            }
                        }
                        connection.Close();

                        // Debug output
                        Console.WriteLine($"Token: {token}");

                        if (plainFormat) {
                            return ergebnis_plainText;
                        }
                        else
                        {
                            ergebnis_Text = JsonSerializer.Serialize(userDeck, new JsonSerializerOptions { WriteIndented = true });
                            return ergebnis_Text;
                        }
                    }
                }
                connection.Close();
                return "";
            }
        }


        public bool DoesCardBelongToUser(string token, string cardId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string username = GetUserName(token);

                if (!string.IsNullOrEmpty(username))
                {
                    string sql = "SELECT COUNT(*) " +
                    "FROM users " +
                    "JOIN user_packages ON users.username = user_packages.username " +
                    "JOIN cards ON user_packages.package_id = cards.package_id " +
                    "WHERE users.token = @token AND cards.id = @cardId;";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@token", token);
                        command.Parameters.AddWithValue("@cardId", cardId);

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        connection.Close();
                        return count > 0;
                    }
                }

                connection.Close();
                return false;
            }
        }

        public void DeleteUserDeck(string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM deck WHERE username = @username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public void AddCardToUserDeck(string username, string cardId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO deck (username, card_id) VALUES (@username, @cardId)";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@cardId", cardId);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
        public bool DoesUserExist(string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT COUNT(*) FROM users WHERE username = @username;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    connection.Close();
                    return count > 0;
                }
            }
        }
        public bool ValidUserToken(string username, string token)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT COUNT(*) FROM users WHERE username = @username AND token = @token;";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@token", token);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    connection.Close();

                    return count > 0;
                }
            }
        }
        public string GetUserData(string username)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                var content = new
                {
                    Bio = (string)null,
                    Image = (string)null,
                    Name = (string)null
                };

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT image, bio, name FROM users WHERE username = @username;";
                    command.Parameters.AddWithValue("@username", username);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            User user = new User
                            {
                                Bio = reader.IsDBNull(1) ? null : (string)reader["bio"],
                                Image = reader.IsDBNull(0) ? null : (string)reader["image"],
                                Name = reader.IsDBNull(2) ? null : (string)reader["name"]
                            };
                            content = new
                            {
                                Bio = user.Bio,
                                Image = user.Image,
                                Name = user.Name,
                            };
                        }
                    }

                connection.Close();
                return JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true });
            }
            }
        }
        public void UpdateUserData(string username, string jsonUserData)
        {
            try
            {
                User userData = JsonSerializer.Deserialize<User>(jsonUserData);

                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(
                        "UPDATE users SET name = @name, bio = @bio, image = @image WHERE username = @username;", connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@name", userData.Name);
                        command.Parameters.AddWithValue("@bio", userData.Bio);
                        command.Parameters.AddWithValue("@image", userData.Image);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            }
        }
        public string GetUserStats(string token)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    var content = new
                    {
                        Name = (string)null,
                        Wins = (int)0,
                        Losses = (int)0,
                        Elo = (int)0
                    };
                    string sql = "SELECT name, elo, wins, losses FROM users WHERE token = @token;";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@token", token);

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                content = new
                                {
                                    Name = reader.IsDBNull(0) ? null : reader.GetString(0),
                                    Wins = reader.GetInt32(2),
                                    Losses = reader.GetInt32(3),
                                    Elo = reader.GetInt32(1)
                                };
                                connection.Close();
                                return JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true });
                            }
                        }
                    }
                }

                return "{}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading user stats: {ex.Message}");
                return "{}";
            }
        }
        public string GetScoreboard()
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    var userStats = new
                    {
                        Name = (string)null,
                        Wins = (int)0,
                        Losses = (int)0,
                        Elo = (int)0
                    };
                    string sql = "SELECT name, elo, wins, losses FROM users ORDER BY elo DESC;";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        List<object> scoreboard = new List<object>();

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userStats = new
                                {
                                    Name = reader["name"] is DBNull ? null : reader.GetString("name"),
                                    Wins = reader.GetInt32("wins"),
                                    Losses = reader.GetInt32("losses"),
                                    Elo = reader.GetInt32("elo")
                                };

                                scoreboard.Add(userStats);
                            }
                        }

                        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        connection.Close();
                        return JsonSerializer.Serialize(scoreboard, jsonOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading user stats: {ex.Message}");
                return "[]";
            }
        }
        private void AlterTableUsersAddLastSpinDate()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string sql = "ALTER TABLE users ADD COLUMN IF NOT EXISTS last_spin_date DATE;";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding column 'last_spin_date' to 'users' table: {ex.Message}");
                    }
                }
                connection.Close();
            }
        }
        public int GetUserCoins(string username)
        {
            int coins = 0;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT coins FROM users WHERE username = @username";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            coins = reader.GetInt32(0);
                        }
                    }
                }

                connection.Close();
            }

            return coins;
        }

        public void UpdateLastSpinDate(string username, DateTime currentDate)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "UPDATE users SET last_spin_date = @currentDate WHERE username = @username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@currentDate", currentDate);
                    command.Parameters.AddWithValue("@username", username);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public bool CanUserSpinWheel(string username, DateTime currentDate)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT last_spin_date FROM users WHERE username = @username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    object lastSpinDateObj = command.ExecuteScalar();

                    if (lastSpinDateObj != DBNull.Value)
                    {
                        DateTime lastSpinDate = (DateTime)lastSpinDateObj;

                        bool oneDayPassed = (currentDate - lastSpinDate).TotalDays >= 1;

                        connection.Close();
                        return oneDayPassed;
                    }
                }

                connection.Close();

                return true;
            }
        }

        public int SpinWheel()
        {
            // Returns a random value from 1 to 10
            Random random = new Random();
            return random.Next(1, 11);
        }

        public void AwardCoins(string username, int coins)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "UPDATE users SET coins = coins + @coins WHERE username = @username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@coins", coins);
                    command.Parameters.AddWithValue("@username", username);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
    }
}