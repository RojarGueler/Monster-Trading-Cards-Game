using rgueler_mtcg;
using rgueler_mtcg.Database;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace rgueler_mtcg
{
    public class Program
    {
        static async Task Main()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 10001;

            TcpListener tcpListener = new TcpListener(ipAddress, port);

            DBRepository dBinitRepository = new DBRepository();
            dBinitRepository.InitDB();

            tcpListener.Start();
            Console.WriteLine($"Server started. Listening on {ipAddress}:{port}");

            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                Task.Run(() => { HandleClient(client); });
            }
        }

        static async Task HandleClient(TcpClient client)
        {
            using (NetworkStream networkStream = client.GetStream())
            using (StreamReader reader = new StreamReader(networkStream))
            using (StreamWriter writer = new StreamWriter(networkStream))
            {
                StringBuilder requestBuilder = new StringBuilder();
                string line;

                while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
                {
                    requestBuilder.AppendLine(line);
                }

                string fullRequest = requestBuilder.ToString();
                Console.WriteLine($"Received request: {fullRequest}");

                int contentLength = ExtractContentLength(fullRequest);
                char[] requestBody = new char[contentLength];
                if (contentLength > 0) 
                { 
                    await reader.ReadAsync(requestBody, 0, contentLength);
                }
                
                string jsonBody = new string(requestBody);


                string concatinatedRequest = fullRequest + jsonBody;
                Request HandleRequest = new Request(concatinatedRequest);

                byte[] responseData = Encoding.UTF8.GetBytes(HandleRequest.response);
                await networkStream.WriteAsync(responseData, 0, responseData.Length);
            }
            Console.WriteLine("Client disconnected");
        }

        private static int ExtractContentLength(string request)
        {
            const string contentLengthHeader = "Content-Length:";
            int index = request.IndexOf(contentLengthHeader, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                int start = index + contentLengthHeader.Length;
                int end = request.IndexOf("\r\n", start, StringComparison.OrdinalIgnoreCase);
                if (end > start && int.TryParse(request.Substring(start, end - start).Trim(), out int length))
                {
                    return length;
                }
            }
            return 0;
        }
    }
}
