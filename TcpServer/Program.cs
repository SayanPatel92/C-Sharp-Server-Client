using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TCPServer
{
    class Program
    {
        // ConcurrentDictionary to hold connected clients
        private static ConcurrentDictionary<TcpClient, string> clients = new ConcurrentDictionary<TcpClient, string>();

        static async Task Main(string[] args)
        {
            // Define the server's IP address and port
            IPAddress ipAddress = IPAddress.Any; // Listen on all network interfaces
            int port = 8888;
            TcpListener listener = new TcpListener(ipAddress, port);

            listener.Start();
            Console.WriteLine($"Server started. Listening on {IPAddress.Any}:{port}...");

            // Start accepting clients asynchronously
            _ = AcceptClientsAsync(listener);

            // Server command input loop
            while (true)
            {
                string serverInput = Console.ReadLine();
                if (serverInput.ToLower() == "exit")
                {
                    Console.WriteLine("Shutting down server...");
                    break;
                }

                // Broadcast message to all connected clients
                await BroadcastMessageAsync($"Server: {serverInput}");
            }

            // Stop the listener and disconnect all clients
            listener.Stop();
            foreach (var client in clients.Keys)
            {
                client.Close();
            }
            Console.WriteLine("Server stopped.");
        }

        // Asynchronously accept incoming client connections
        private static async Task AcceptClientsAsync(TcpListener listener)
        {
            while (true)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    clients.TryAdd(client, client.Client.RemoteEndPoint.ToString());
                    Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

                    // Start handling the client in a separate task
                    _ = HandleClientAsync(client);
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been stopped
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        // Asynchronously handle communication with a connected client
        private static async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"{clients[client]}: {message}");

                    // Optionally, broadcast the message to other clients
                    await BroadcastMessageAsync($"{clients[client]}: {message}", client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clients[client]}: {ex.Message}");
            }
            finally
            {
                // Remove and close the client
                clients.TryRemove(client, out _);
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                client.Close();
            }
        }

        // Asynchronously broadcast a message to all connected clients
        private static async Task BroadcastMessageAsync(string message, TcpClient excludeClient = null)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            foreach (var client in clients.Keys)
            {
                if (client == excludeClient)
                    continue;

                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message to {clients[client]}: {ex.Message}");
                }
            }
        }
    }
}