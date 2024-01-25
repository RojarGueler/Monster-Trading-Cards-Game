using System;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace rgueler_mtcg
{
    public class Response
    {
        private readonly Dictionary<int, string> messages;
        public Response(string path)
        {
            messages = InitializeMessages(path);
        }
        private Dictionary<int, string> InitializeMessages(string path)
        {
            switch (path)
            {
                case "users":
                    return new Dictionary<int, string>
                    {
                        { 201, "HTTP/1.1 201 CREATED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User successfully created\"}\r\n" },
                        { 409, "HTTP/1.1 409 CONFLICT\r\nContent-Type: application/json\r\n\r\n{\"message\": \"This User already exists!\"}\r\n" },
                    };

                case "sessions":
                    return new Dictionary<int, string>
                    {
                        { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User login was successful\"}\r\n" },
                        { 401, "HTTP/1.1 401 UNAUTHORIZED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Invalid username/password\"}\r\n"}
                    };

                case "packages":
                    return new Dictionary<int, string>
                    {
                        { 201, "HTTP/1.1 201 CREATED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Package and cards successfully created\"}\r\n" },
                        { 401, "HTTP/1.1 401 UNAUTHORIZED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                        { 403, "HTTP/1.1 403 FORBIDDEN\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Provided user is not \"admin\"\"}\r\n" },
                        { 409, "HTTP/1.1 409 CONFLICT\r\nContent-Type: application/json\r\n\r\n{\"message\": \"At least one card in the packages already exists\"}\r\n" },
                    };

                case "transactions/packages":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"A package has been successfully bought\"}\r\n" },
                    { 401, "HTTP/1.1 401 UNAUTHORIZED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 403, "HTTP/1.1 403 NOT ENOUGH MONEY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Not enough money for buying a card package\"}\r\n" },
                    { 404, "HTTP/1.1 404 NO PACKAGE AVAILABLE\r\nContent-Type: application/json\r\n\r\n{\"message\": \"No card package available for buying\"}\r\n" },
                };

                case "cards":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The user has cards, the response contains these\"}\r\n" },
                    { 204, "HTTP/1.1 204 NO CARD\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The request was fine, but the user doesn't have any cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };

                case "GETdeck":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deck has cards, the response contains these\"}\r\n" },
                    { 204, "HTTP/1.1 204 NO CARD\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The request was fine, but the deck doesn't have any cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };

                case "PUTdeck":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deck has been successfully configured\"}\r\n" },
                    { 400, "HTTP/1.1 400 AMOUNT ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The provided deck did not include the required amount of cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 403, "HTTP/1.1 403 NOT YOUR CARD\r\nContent-Type: application/json\r\n\r\n{\"message\": \"At least one of the provided cards does not belong to the user or is not available.\"}\r\n" }
                };

                case "GETuser":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Data successfully retrieved\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 404, "HTTP/1.1 404 NOT FOUND\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User not found.\"}\r\n" }
                };

                case "PUTuser":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Data successfully updated\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 404, "HTTP/1.1 404 NOT FOUND\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User not found.\"}\r\n" }
                };

                case "GETstats":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The stats could be retrieved successfully.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };

                case "scoreboard":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The scoreboard could be retrieved successfully.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };

                case "GETtrade":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"There are trading deals available, the response contains these.\"}\r\n" },
                    { 205, "HTTP/1.1 205 OKAY(NO CONTENT)\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The request was fine, but there are no trading deals available.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid.\"}\r\n" }
                };

                case "POSTtrade":
                    return new Dictionary<int, string>
                {
                    { 201, "HTTP/1.1 201 SUCCESFULL TRADE\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Trading deal successfully created.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid.\"}\r\n" },
                    { 403, "HTTP/1.1 403 WRONG TRADE\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deal contains a card that is not owned by the user or locked in the deck.\"}\r\n" },
                    { 409, "HTTP/1.1 409 TRADE ALREADY EXISTS\r\nContent-Type: application/json\r\n\r\n{\"message\": \"A deal with this deal ID already exists..\"}\r\n" }
                };

                case "DELETEtrade":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Trading deal successfully deleted.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid.\"}\r\n" },
                    { 403, "HTTP/1.1 403 FORBIDDEN\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deal contains a card that is not owned by the user.\"}\r\n" },
                    { 404, "HTTP/1.1 404 ID COULDN'T BE FOUND\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The provided deal ID was not found.\"}\r\n" },
                    { 409, "HTTP/1.1 409 TRADE ALREADY EXISTS\r\nContent-Type: application/json\r\n\r\n{\"message\": \"A deal with this deal ID already exists.\"}\r\n" }
                };

                case "SUCCESStrade":
                    return new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OKAY\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Trading deal successfully executed.\"}\r\n" },
                    { 401, "HTTP/1.1 401 TOKEN ERROR\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid.\"}\r\n" },
                    { 403, "HTTP/1.1 403 WRONG TRADE\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The offered card is not owned by the user, or the requirements are not met (Type, MinimumDamage), or the offered card is locked in the deck.\"}\r\n" },
                    { 404, "HTTP/1.1 404 ID COULDN'T BE FOUND\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The provided deal ID was not found.\"}\r\n" },
                    { 410, "HTTP/1.1 410 USER TRADE FAILED\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The User wanted to Trade with himself.\"}\r\n" },
                    { 411, "HTTP/1.1 411 DAMAGE TOO LOW\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The Damage of the Provided Card is too low.\"}\r\n" }
                };

                default:
                    return new Dictionary<int, string>();
            }
        }
        public string GetResponseMessage(int statusCode)
        {
            if (messages.TryGetValue(statusCode, out string message))
            {
                return message;
            }
            return $"HTTP/1.1 {statusCode} \r\nContent-Type: text/plain\r\n\r\nUnknown Status Code";
        }
    }
}