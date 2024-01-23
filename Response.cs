using System;
using System.IO;
using System.Text;

namespace rgueler_mtcg
{
    public class Response
    {
        public string Status { get; private set; }
        public string ContentType { get; private set; }
        public string Content { get; private set; }

        private readonly Dictionary<int, string> messages;
        public Response(string path)
        {
            switch (path)
            {
                case "users":
                    messages = new Dictionary<int, string>
                {
                    { 201, "HTTP/1.1 201 Created\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User successfully created\"}\r\n" },
                    { 409, "HTTP/1.1 409 Conflict\r\nContent-Type: application/json\r\n\r\n{\"message\": \"This User already exists!\"}\r\n" },
                };
                    break;
                case "sessions":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User login was successful\"}\r\n" },
                    { 401, "HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Invalid username/password\"}\r\n"}
                };
                    break;

                case "packages":
                    messages = new Dictionary<int, string>
                {
                    { 201, "HTTP/1.1 201 Created\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Package and cards successfully created\"}\r\n" },
                    { 401, "HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 403, "HTTP/1.1 403 Forbidden\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Provided user is not \"admin\"\"}\r\n" },
                    { 409, "HTTP/1.1 409 Conflict\r\nContent-Type: application/json\r\n\r\n{\"message\": \"At least one card in the packages already exists\"}\r\n" },
                };
                    break;

                case "transactions/packages":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"A package has been successfully bought\"}\r\n" },
                    { 401, "HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 403, "HTTP/1.1 403 No Money\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Not enough money for buying a card package\"}\r\n" },
                    { 404, "HTTP/1.1 404 No Package\r\nContent-Type: application/json\r\n\r\n{\"message\": \"No card package available for buying\"}\r\n" },
                };
                    break;

                case "cards":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The user has cards, the response contains these\"}\r\n" },
                    { 204, "HTTP/1.1 204 No Card\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The request was fine, but the user doesn't have any cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 Token Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };
                    break;

                case "deckGET":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deck has cards, the response contains these\"}\r\n" },
                    { 204, "HTTP/1.1 204 No Card\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The request was fine, but the deck doesn't have any cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 Token Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" }
                };
                    break;

                case "deckPUT":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The deck has been successfully configured\"}\r\n" },
                    { 400, "HTTP/1.1 400 Amount Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"The provided deck did not include the required amount of cards\"}\r\n" },
                    { 401, "HTTP/1.1 401 Token Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 403, "HTTP/1.1 403 Not Your Card\r\nContent-Type: application/json\r\n\r\n{\"message\": \"At least one of the provided cards does not belong to the user or is not available.\"}\r\n" }
                };
                    break;

                case "GETuser":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Data successfully retrieved\"}\r\n" },
                    { 401, "HTTP/1.1 401 Token Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 404, "HTTP/1.1 404 Not Found\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User not found.\"}\r\n" }
                };
                    break;

                case "PUTuser":
                    messages = new Dictionary<int, string>
                {
                    { 200, "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Data successfully updated\"}\r\n" },
                    { 401, "HTTP/1.1 401 Token Error\r\nContent-Type: application/json\r\n\r\n{\"message\": \"Access token is missing or invalid\"}\r\n" },
                    { 404, "HTTP/1.1 404 Not Found\r\nContent-Type: application/json\r\n\r\n{\"message\": \"User not found.\"}\r\n" }
                };
                    break;

                default:
                    // Handle the default case, if any
                    break;
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