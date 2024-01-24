﻿using rgueler_mtcg.Database;
using System;
using System.Text.Json;
using rgueler_mtcg.GameObjects;
using System.Linq;

namespace rgueler_mtcg
{
    public class Request
    {
        public string request { get; private set; }
        public string response { get; private set; }

        public DBRepository dBRepository { get; private set; }

        private const string adminToken = "admin-mtcgToken";

        public Request(string rawRequest)
        {
            this.response = "";
            this.request = rawRequest;
            this.dBRepository = new DBRepository();

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

            Console.WriteLine("endpoint: " + endpoint);

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

                        default:
                            
                            break;
                    }

                    if(endpoint.Contains("PUT /users")) HandlePutUserRequest(jsonPayload, parsedAuthenticationToken);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }else
            {
                try
                {
                    switch(endpoint)
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

                        default:
                            break;
                    }
                    if(endpoint.Contains("GET /users")) HandleGetUserRequest(parsedAuthenticationToken);
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
                Response responseMsg = new Response("users");
                string username = "";
                username = userObject.Username;
                if (!dBRepository.DoesUserExist(username))
                {
                    dBRepository.AddUser();
                    response = responseMsg.GetResponseMessage(201);
                }
                else response = responseMsg.GetResponseMessage(409);

            }
            else if (request.Contains("POST /sessions"))
            {
                Response responseMsg = new Response("sessions");
                if (dBRepository.IsUserLoggedIn(userObject.Username, userObject.Password))
                {
                    response = responseMsg.GetResponseMessage(200) + "Token: " + userObject.Username + "-mtcgToken\r\n";
                }
                else
                {
                    response = responseMsg.GetResponseMessage(401);
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
            Response responseMsg = new Response("deckPUT");

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

                        response = responseMsg.GetResponseMessage(200);
                    }
                    else response = responseMsg.GetResponseMessage(403);
                }
                else response = responseMsg.GetResponseMessage(400);
            }
            else response = responseMsg.GetResponseMessage(401);
        }

        void HandlePutUserRequest(string jsonPayload, string parsedAuthenticationToken)
        {
            Response responseMsg = new Response("PUTuser");

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

                    response = responseMsg.GetResponseMessage(200);
                }
                else response = responseMsg.GetResponseMessage(401);
            }
            else response = responseMsg.GetResponseMessage(404);
        }

        void HandleTransactionsPackagesRequest(string parsedAuthenticationToken)
        {
            Response responseMsg = new Response("transactions/packages");
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
                    response = responseMsg.GetResponseMessage(200);

                    coins = dBRepository.GetCoins(username);
                    //Console.WriteLine("coins:" + coins);
                }
                else
                {
                    response = responseMsg.GetResponseMessage(403);
                }
            }
            else
            {
                response = responseMsg.GetResponseMessage(404);
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
            Response responseMsg = new Response("deckGET");
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string deck = "";

                if (request.Contains("format=plain")) deck = dBRepository.GetCardsFromDeck(parsedAuthenticationToken, true);
                else deck = dBRepository.GetCardsFromDeck(parsedAuthenticationToken, false);

                if (deck.Length > 2)
                {
                    response = responseMsg.GetResponseMessage(200) + deck + "\r\n";
                }
                else
                {
                    response = responseMsg.GetResponseMessage(204);
                }
            }
            else response = responseMsg.GetResponseMessage(401);
        }

        void HandleGetUserRequest(string parsedAuthenticationToken)
        {
            Response responseMsg = new Response("GETuser");

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

                    response = responseMsg.GetResponseMessage(200) + userData + "\r\n";
                }
                else response = responseMsg.GetResponseMessage(401);
            }
            else response = responseMsg.GetResponseMessage(404);
        }

        void HandleGetStatsRequest(string parsedAuthenticationToken)
        {
            Response responseMsg = new Response("GETstats");
            if (dBRepository.DoesTokenExist(parsedAuthenticationToken))
            {
                string output = dBRepository.GetUserStats(parsedAuthenticationToken);
                response = responseMsg.GetResponseMessage(200) + output + "\r\n";
            }
            else
            {
                responseMsg.GetResponseMessage(401);
            }
        }
    }
}
