using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace JsonTester
{
    // initial version derived from https://codereview.stackexchange.com/questions/41591/websockets-client-code-and-making-it-production-ready
    // and from https://github.com/aiusepsi/SourceRcon
    class Program
    {
        private static object consoleLock = new object();
        private const int receiveChunkSize = 1024;
        private const bool verbose = true;
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(1000);

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Minimum expected arguments are [host] [port] [password]");
                return;
            }

            var uri = $"ws://{args[0]}:{args[1]}/{args[2]}";
            Connect(uri).Wait();
        }

        public static async Task Connect(string uri)
        {
            ClientWebSocket webSocket = null;

            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                do
                {
                    await Send(webSocket, string.Empty);
                    await Task.Delay(delay);
                    await Receive(webSocket);
                } while (webSocket.State == WebSocketState.Open);
                //                await Task.WhenAll(Receive(webSocket), Send(webSocket, "ping"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
                Console.WriteLine();

                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("WebSocket closed.");
                    Console.ResetColor();
                }
            }
        }

        static UTF8Encoding encoder = new UTF8Encoding();

        private static async Task Send(ClientWebSocket webSocket, string message)
        {
            WebRcon rcon = new WebRcon();

            if (string.IsNullOrEmpty(message))
            {
                rcon.Message = string.Empty;
                rcon.Identifier = -1;
            }
            else
            {
                rcon.Message = $"say {TrimString(message)}";
                rcon.Identifier = 1;
            }

            var js = new JavaScriptSerializer();
            var jsonString = js.Serialize(rcon);
            byte[] buffer = encoder.GetBytes(jsonString);

            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                CancellationToken.None);

            //            while (webSocket.State == WebSocketState.Open)
            //            {
            //                await Task.Delay(delay);
            //            }
        }

        private static async Task Receive(ClientWebSocket webSocket)
        {
            //            while (webSocket.State == WebSocketState.Open)
            //            {
            string messageString = string.Empty;
            WebSocketReceiveResult result;
            var message = new ArraySegment<byte>(new byte[receiveChunkSize]);
            //try
            //{
            do
            {

                result = await webSocket.ReceiveAsync(message, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                        CancellationToken.None);
                }

                LogStatus(true, message.ToArray(), message.ToArray().Length);

                var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                messageString += encoder.GetString(messageBytes);

            }
            while (!result.EndOfMessage);

            //} catch (System.Exception e)
            //{
            //	LogStatus(false, encoder.GetBytes(e.Message), e.Message.Length);
            //}

            if (messageString.Contains("SERVER"))
                return;

            try
            {
                var js = new JavaScriptSerializer();
                var obj = js.Deserialize<WebRcon>(messageString);
                if (obj.Message.Contains("killed") || obj.Message.Contains("suicide") || obj.Message.Contains("died"))
                {
                    await Send(webSocket, obj.Message);
                }
            }
            catch (Exception e)
            {
                LogStatus(true, encoder.GetBytes(e.Message), e.Message.Length);
            }
            //            }
        }

        private static void LogStatus(bool receiving, byte[] buffer, int length)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = receiving ? ConsoleColor.Green : ConsoleColor.Gray;

                if (verbose)
                {
                    var log = encoder.GetString(buffer).Trim().Replace("\0", string.Empty);
                    Console.WriteLine(log);
                }

                Console.ResetColor();
            }
        }

        private static string TrimString(string original)
        {
            // remove the steamId and whatever from the message (i.e.  Solomon Grundy[1234532/123948149328923] was born on a Mundy)
            var index1 = original.IndexOf("[");
            var index2 = original.IndexOf("]");
            if (index1 == -1 || index2 == -1)
                return original;

            var userName = original.Substring(0, index1);

            if (original.ToLower().Contains("suicide"))
            {
                return $"{userName} died as they lived, a traitor (suicide).";
            }
            else if (original.ToLower().Contains("bear"))
            {
                return $"I come bearing terrible news {userName} was just barely killed (bear).";
            }
            else if (original.ToLower().Contains("boar"))
            {
                return $"{userName} was killed by boredom (boar).";
            }
            else if (original.ToLower().Contains("cargo_plane"))
            {
                return "An airdrop is inbound.";
            }

            var scrubbed = original.Substring(0, index1) + original.Substring(index2 + 1, original.Length - index2 - 1);
            return scrubbed;
        }
    }

    internal class WebRcon
    {
        public int Identifier { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Stacktrace { get; set; }
        public double Time { get; set; }
        public double UserId { get; set; }
    }
}

