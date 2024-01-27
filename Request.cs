using rgueler_mtcg.Database;
using System;
using System.Text.Json;
using rgueler_mtcg.GameObjects;
using System.Linq;
using System.Text.RegularExpressions;

namespace rgueler_mtcg
{
    public class Request
    {
        public string request { get; private set; }
        public string response { get; private set; }

        public DBRepository dBRepository { get; private set; }
        public TradingDBRepository tradingDBRepository { get; private set; }

        private const string adminToken = "admin-mtcgToken";

        public Request(string rawRequest)
        {
            this.response = "";
            this.request = rawRequest;
            this.dBRepository = new DBRepository();
            this.tradingDBRepository = new TradingDBRepository();
            string authToken = ParseRequest();

            HandleRequest(authToken);
        }

        private string ParseRequest()
        {
            const string authorizationHeader = "Authorization: Bearer ";
            int startIndex = request.IndexOf(authorizationHeader);

            if (startIndex >= 0)
            {
                startIndex += authorizationHeader.Length;
                int endIndex = request.IndexOf("\r\n", startIndex);

                if (endIndex >= 0)
                {
                    return request.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }
        private void HandleRequest(string token)
        {
            string parsedAuthenticationToken = token;
            int indexBody = this.request.IndexOf("{");

            if ((request.Contains("POST /packages")) || request.Contains("PUT /deck")) indexBody = request.IndexOf("[");

            string endpoint = request;
            string[] parts = endpoint.Split(' ');

            // Check if there are at least two parts
            if (parts.Length >= 2)
            {
                // Concatenate the first two parts to get the result
                endpoint = parts[0] + " " + parts[1];
            }

            if (indexBody >= 0)
            {
                // Find the end of the HTTP headers
                int headerEndIndex = request.IndexOf("\r\n\r\n") + 4;

                // Extract the Content-Length header value
                string contentLengthHeader = request.Substring(request.IndexOf("Content-Length:") + 15);
                int contentLength = int.Parse(contentLengthHeader.Substring(0, contentLengthHeader.IndexOf("\r\n")));

                // Extract the JSON payload based on Content-Length
                var jsonPayload = request.Substring(indexBody, contentLength);

                try
                {

                    switch (endpoint)
                    {
                        case "POST /users":
                        case "POST /sessions":
                            HandleUserAndSessionsRequests(jsonPayload);
                            break;

                        case "POST /packages":
                            HandlePackagesRequest(jsonPayload, token);
                            break;

                        case "PUT /deck":
                            HandleDeckRequest(jsonPayload, parsedAuthenticationToken);
                            break;

                        case "POST /tradings":
                            HandlePostTrading(jsonPayload, parsedAuthenticationToken);
                            break;

                        default:

                            break;
                    }

                    if (endpoint.Contains("PUT /users")) HandlePutUserRequest(jsonPayload, parsedAuthenticationToken);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    switch (endpoint)
                    {
                        case "POST /transactions/packages":
                            HandleTransactionsPackagesRequest(parsedAuthenticationToken);
                            break;

                        case "GET /cards":
                            HandleGetCardsRequest(parsedAuthenticationToken);
                            break;

                        case "GET /deck":
                        case "GET /deck?format=plain":
                            HandleGetDeckRequest(parsedAuthenticationToken);
                            break;

                        case "GET /stats":
                            HandleGetStatsRequest(parsedAuthenticationToken);
                            break;

                        case "GET /scoreboard":
                            HandleGetScoreboardRequest(parsedAuthenticationToken);
                            break;

                        case "GET /tradings":
                            HandleGetTradings(parsedAuthenticationToken);
                            break;

                        case "GET /coins":
                            HandleGetCoins(parsedAuthenticationToken);
                            break;

                        case "PUT /coins":
                            HandlePutCoins(parsedAuthenticationToken);
                            break;

                        default:
                            break;
                    }
                    if (endpoint.Contains("GET /users")) HandleGetUserRequest(parsedAuthenticationToken);

                    if (endpoint.Contains("DELETE /tradings/"))
                    {
                        string id = "";
                        int startindex = request.IndexOf("/tradings/");

                        if (startindex != -1)
                        {
                            int space = startindex + ("/tradings/").Length;
                            int endindex = request.IndexOf(' ', space);

                            if (endindex != -1)
                            {
                                id = request.Substring(space, endindex - space);
                            }
                        }
                        HandleDeleteTradings(parsedAuthenticationToken, id);
                    }
                    if (endpoint.Contains("POST /tradings/"))
                    {
                        int startIndex = this.request.IndexOf("\"") + 1;
                        int endIndex = this.request.IndexOf("\"", startIndex);
                        string cardId = "";
                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            cardId = this.request.Substring(startIndex, endIndex - startIndex);
                        }

                        string tradeId = "";
                        int start = request.IndexOf("/tradings/");

                        if (start != -1)
                        {
                            int space = start + ("/tradings/").Length;
                            int end = request.IndexOf(' ', space);

                            if (end != -1)
                            {
                                tradeId = request.Substring(space, end - space);
                            }
                        }

                        HandlePostTradingsWithID(parsedAuthenticationToken, cardId, tradeId);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }
        // Helper methods to handle specific endpoints
        void HandleUserAndSessionsRequests(string jsonPayload)
        {
            // Parse JSON payload
            var userObject = JsonSerializer.Deserialize<User>(jsonPayload);
            Console.WriteLine(userObject);
            dBRepository = new DBRepository(userObject);
            // Check the endpoint and perform specific logic
            if (request.Contains("POST /users"))
            {
                Response responsePostUsers = new Response("users");
                string username = "";
                username = userObject.Username;
                if (!dBRepository.DoesUserExist(username))
                {
                    dBRepository.AddUser();
                    response = responsePostUsers.GetResponseMessage(201);
                }
                else response = responsePostUsers.GetResponseMessage(409);

            }
            else if (request.Contains("POST /sessions"))
            {
                Response responsePostSessions = new Response("sessions");
                if (dBRepository.IsUserLoggedIn(userObject.Username, userObject.Password))
                {
                    response = responsePostSessions.GetResponseMessage(200) + "Token: " + userObject.Username + "-mtcgToken\r\n";
                }
                else
                {
                    response = responsePostSessions.GetResponseMessage(401);
                }
            }
        }

        void HandlePackagesRequest(string jsonPayload, string token)
        {
            Response responsePackage = new Response("packages");
            if (token.Length <= 0)
            {
                response = responsePackage.GetResponseMessage(401);
            }
            else
            {
                if (token == adminToken)
                {
                    // Deserialize the JSON payload into a list of cards
                    List<Card> cards = JsonSerializer.Deserialize<List<Card>>(jsonPayload);

                    if (dBRepository.CheckDuplicateCards(cards))
                    {
                        response = responsePackage.GetResponseMessage(409);
                    }
                    else
                    {
                        int packageID = dBRepository.CreatePackageID();
                        // Create a package
                        var package = new Package { PackageId = packageID, Bought = false };

                        // Add the cards to the package
                        package.Cards.AddRange(cards);

                        dBRepository.AddPackage(package);

                        response = responsePackage.GetResponseMessage(201);
                    }
                }
                else response = responsePackage.GetResponseMessage(403);
            }
        }

        void HandleDeckRequest(string jsonPayload, string parsedAuthenticationToken)
        {
            Response responsePutDeck = new Response("PUTdeck");

            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                List<string> cardIds = JsonSerializer.Deserialize<List<string>>(jsonPayload);
                Console.WriteLine("cardIds: " + cardIds.Count);
                Console.WriteLine("cardIds: " + cardIds[0]);
                bool cardnotmine = false;
                if (cardIds.Count == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (dBRepository.DoesCardBelongToUser(parsedAuthenticationToken, cardIds[i]) == false) cardnotmine = true;
                    }

                    if (!cardnotmine)
                    {
                        dBRepository.DeleteUserDeck(dBRepository.GetUserName(parsedAuthenticationToken));
                        for (int i = 0; i < 4; i++)
                        {
                            dBRepository.AddCardToUserDeck(dBRepository.GetUserName(parsedAuthenticationToken), cardIds[i]);
                        }

                        response = responsePutDeck.GetResponseMessage(200);
                    }
                    else response = responsePutDeck.GetResponseMessage(403);
                }
                else response = responsePutDeck.GetResponseMessage(400);
            }
            else response = responsePutDeck.GetResponseMessage(401);
        }

        void HandlePutUserRequest(string jsonPayload, string parsedAuthenticationToken)
        {
            Response responsePutUser = new Response("PUTuser");

            // Find the position of the username in the curl script
            int usernameStart = request.IndexOf("/users/") + "/users/".Length;
            int usernameEnd = request.IndexOf(' ', usernameStart);

            // Extract the username
            string username = request.Substring(usernameStart, usernameEnd - usernameStart);

            if (dBRepository.DoesUserExist(username))
            {
                if (dBRepository.ValidUserToken(username, parsedAuthenticationToken) || parsedAuthenticationToken == adminToken)
                {
                    dBRepository.UpdateUserData(username, jsonPayload);

                    response = responsePutUser.GetResponseMessage(200);
                }
                else response = responsePutUser.GetResponseMessage(401);
            }
            else response = responsePutUser.GetResponseMessage(404);
        }

        void HandleTransactionsPackagesRequest(string parsedAuthenticationToken)
        {
            Response responseTransactionsPackages = new Response("transactions/packages");
            List<int> packagelist = new List<int>();
            int coins;
            string username = dBRepository.GetUserName(parsedAuthenticationToken);
            //Console.WriteLine("username:" + username);
            coins = dBRepository.GetCoins(username);
            //Console.WriteLine("coins:" + coins);
            packagelist = dBRepository.IsAnyPackageAvailable();
            //Console.WriteLine("packagelist:" + packagelist);

            if (packagelist.Count > 0)
            {
                if (coins >= 5)
                {
                    dBRepository.UpdateCoinsByPackage(username);
                    dBRepository.AcquirePackage(packagelist[0], username);
                    //Console.WriteLine("packagelist[0]:" + packagelist[0]);
                    response = responseTransactionsPackages.GetResponseMessage(200);

                    coins = dBRepository.GetCoins(username);
                    //Console.WriteLine("coins:" + coins);
                }
                else
                {
                    response = responseTransactionsPackages.GetResponseMessage(403);
                }
            }
            else
            {
                response = responseTransactionsPackages.GetResponseMessage(404);
            }
        }

        void HandleGetCardsRequest(string parsedAuthenticationToken)
        {
            Response responseCards = new Response("cards");

            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string userCards = dBRepository.GetUserCardsJSON(parsedAuthenticationToken);

                if (userCards.Length > 2)
                {
                    response = responseCards.GetResponseMessage(200) + userCards + "\r\n";
                }
                else
                {
                    response = responseCards.GetResponseMessage(204);
                }
            }
            else response = responseCards.GetResponseMessage(401);
        }

        void HandleGetDeckRequest(string parsedAuthenticationToken)
        {
            Response responseGetDeck = new Response("GETdeck");
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string deck = "";

                if (request.Contains("format=plain")) deck = dBRepository.GetCardsFromDeck(parsedAuthenticationToken, true);
                else deck = dBRepository.GetCardsFromDeck(parsedAuthenticationToken, false);

                if (deck.Length > 2)
                {
                    response = responseGetDeck.GetResponseMessage(200) + deck + "\r\n";
                }
                else
                {
                    response = responseGetDeck.GetResponseMessage(204);
                }
            }
            else response = responseGetDeck.GetResponseMessage(401);
        }

        void HandleGetUserRequest(string parsedAuthenticationToken)
        {
            Response responseGetUser = new Response("GETuser");

            // Find the position of the username in the curl script
            int usernameStart = request.IndexOf("/users/") + "/users/".Length;
            int usernameEnd = request.IndexOf(' ', usernameStart);

            // Extract the username
            string username = request.Substring(usernameStart, usernameEnd - usernameStart);
            if (dBRepository.DoesUserExist(username))
            {
                if (dBRepository.ValidUserToken(username, parsedAuthenticationToken) || parsedAuthenticationToken == adminToken)
                {
                    string userData = dBRepository.GetUserData(username);

                    response = responseGetUser.GetResponseMessage(200) + userData + "\r\n";
                }
                else response = responseGetUser.GetResponseMessage(401);
            }
            else response = responseGetUser.GetResponseMessage(404);
        }

        void HandleGetStatsRequest(string parsedAuthenticationToken)
        {
            Response responseStats = new Response("GETstats");
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string output = dBRepository.GetUserStats(parsedAuthenticationToken);
                response = responseStats.GetResponseMessage(200) + output + "\r\n";
            }
            else
            {
                responseStats.GetResponseMessage(401);
            }
        }

        void HandleGetScoreboardRequest(string parsedAuthenticationToken)
        {
            Response responseScoreboard = new Response("scoreboard");

            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string output = dBRepository.GetScoreboard();
                response = responseScoreboard.GetResponseMessage(200) + output + "\r\n";
            }
            else response = responseScoreboard.GetResponseMessage(401);
        }

        void HandleGetTradings(string parsedAuthenticationToken)
        {
            Response responseTradings = new Response("GETtrade");

            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string output = tradingDBRepository.GetTrade();
                if (!output.Equals("[]"))
                {
                    response = responseTradings.GetResponseMessage(200) + output + "\r\n";
                }
                else response = responseTradings.GetResponseMessage(205);
            }
            else response = responseTradings.GetResponseMessage(401);
        }
        void HandlePostTrading(string jsonPayload, string parsedAuthenticationToken)
        {
            try
            {
                Response responseTradings = new Response("POSTtrade");

                if (!dBRepository.DoesTokenExist(parsedAuthenticationToken))
                {
                    response = responseTradings.GetResponseMessage(401);
                    return;
                }

                var trade = JsonSerializer.Deserialize<Trade>(jsonPayload);
                string username = dBRepository.GetUserName(parsedAuthenticationToken);

                if (!tradingDBRepository.DoesUserHaveCard(username, trade.CardToTrade))
                {
                    response = responseTradings.GetResponseMessage(403);
                    return;
                }

                if (tradingDBRepository.DoesCardExistInTrading(trade.CardToTrade))
                {
                    response = responseTradings.GetResponseMessage(409);
                    return;
                }

                tradingDBRepository.AddTrade(trade.CardToTrade, trade.Id, trade.Type, trade.MinimumDamage, username);

                response = responseTradings.GetResponseMessage(201);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                response = new Response("POSTtrade").GetResponseMessage(400);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling POST trading: {ex.Message}");
                response = new Response("POSTtrade").GetResponseMessage(500);
            }
        }

        void HandleDeleteTradings(string parsedAuthenticationToken, string id)
        {
            Response responseTradings = new Response("DELETEtrade");
            Console.WriteLine("Token: " + parsedAuthenticationToken);
            Console.WriteLine("ID:----------------------:" + id);
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                if (string.IsNullOrEmpty(id))
                {
                    response = responseTradings.GetResponseMessage(404); // ID not found
                }
                else
                {
                    if (tradingDBRepository.DoesIdExist(id))
                    {
                        string username = dBRepository.GetUserName(parsedAuthenticationToken);
                        //Checks if the ID belongs to the given User
                        if (tradingDBRepository.UserID(id, username))
                        {
                            tradingDBRepository.DeleteTradeById(id);
                            response = responseTradings.GetResponseMessage(200); // OKAY
                        }
                        else
                        {
                            response = responseTradings.GetResponseMessage(403); // Forbidden
                        }
                    }
                    else
                    {
                        response = responseTradings.GetResponseMessage(404); // ID Not Found
                    }
                }
            }
            else
            {
                response = responseTradings.GetResponseMessage(401); // Unauthorized
            }
        }

        void HandlePostTradingsWithID(string parsedAuthenticationToken, string cardId, string tradeId)
        {
            Response responseTradings = new Response("SUCCESStrade");
            Console.WriteLine("Token: " + parsedAuthenticationToken);
            Console.WriteLine("TradeID: " + tradeId);
            Console.WriteLine("cardID: " + cardId);
            if (!dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                response = responseTradings.GetResponseMessage(401);
            }
            else
            {
                if (!tradingDBRepository.DoesIdExist(tradeId))
                {
                    response = responseTradings.GetResponseMessage(404);
                }
                else
                {
                    string username = dBRepository.GetUserName(parsedAuthenticationToken);
                    Console.WriteLine("Username: " + username);
                    if (tradingDBRepository.UserID(tradeId, username))
                    {
                        response = responseTradings.GetResponseMessage(410);
                    }
                    else
                    {
                        if (!tradingDBRepository.DoesUserHaveCard(username, cardId))
                        {
                            response = responseTradings.GetResponseMessage(403);
                        }
                        else
                        {
                            string cardToTrade = tradingDBRepository.GetCardToTradeById(tradeId);

                            if (!tradingDBRepository.CheckIfDamageIsEnough(cardId, cardToTrade))
                            {
                                response = responseTradings.GetResponseMessage(411);
                            }
                            else
                            {
                                tradingDBRepository.DeleteTradeById(tradeId);

                                int tradingone = tradingDBRepository.GetPackageIdFromCardId(cardToTrade);
                                int tradingtwo = tradingDBRepository.GetPackageIdFromCardId(cardId);

                                tradingDBRepository.UpdatePackageIdForCard(cardToTrade, tradingone);
                                tradingDBRepository.UpdatePackageIdForCard(cardId, tradingtwo);

                                response = responseTradings.GetResponseMessage(200);
                            }
                        }
                    }
                }
            }
        }

        void HandleGetCoins(string parsedAuthenticationToken)
        {
            Response responseCoins = new Response("GETcoins");

            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string username = dBRepository.GetUserName(parsedAuthenticationToken);
                int output = dBRepository.GetUserCoins(username);
                response = responseCoins.GetResponseMessage(200) + "user: " + username + " coins: " + output + "\r\n";
            }
            else response = responseCoins.GetResponseMessage(401);
        }

        void HandlePutCoins(string parsedAuthenticationToken)
        {
            Response responseCoins = new Response("PUTcoins");
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                DateTime currentDate = DateTime.Now;
                string username = dBRepository.GetUserName(parsedAuthenticationToken);
                if (dBRepository.CanUserSpinWheel(username, currentDate))
                {
                    // Drehe das Glücksrad und erhalte eine zufällige Anzahl von Münzen
                    int coinsWon = dBRepository.SpinWheel();

                    // Füge die Münzen dem Benutzer hinzu
                    dBRepository.AwardCoins(username, coinsWon);

                    // Aktualisiere das Datum der letzten Drehung
                    dBRepository.UpdateLastSpinDate(username, currentDate);

                    response = responseCoins.GetResponseMessage(200) + "User: " + username + " won: " + coinsWon + "\r\n";
                }
                else
                {
                    response = responseCoins.GetResponseMessage(410) + "\r\n";
                }

            }
            else response = responseCoins.GetResponseMessage(401);
        }
    }
}