using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string serverIP = "127.0.0.1"; // Change to server's IP if needed
            int port = 8888;

            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(serverIP, port);
                Console.WriteLine($"Connected to server at {serverIP}:{port}");

                NetworkStream stream = client.GetStream();

                // Start a task to listen for incoming messages from the server
                _ = ReceiveMessagesAsync(stream);

                // Client input loop to send messages to the server
                while (true)
                {
                    string message = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                    if (message.ToLower() == "exit")
                    {
                        Console.WriteLine("Disconnecting from server...");
                        break;
                    }
                }

                client.Close();
                Console.WriteLine("Disconnected from server.");
            }
            catch (SocketException)
            {
                Console.WriteLine("Unable to connect to server. Make sure the server is running and reachable.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // Asynchronously receive messages from the server
        private static async Task ReceiveMessagesAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // Server has closed the connection
                        Console.WriteLine("Server has disconnected.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message.TrimEnd('\n', '\r'));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection closed: {ex.Message}");
            }
        }
    }
}
