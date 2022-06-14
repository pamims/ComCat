using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ComCat.Services
{
    public class LogService
    {
        private static DiscordSocketClient _client;
        public LogService(DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _client.GuildAvailable += OnGuildAvailable;
            _client.Ready += OnReady;
            _client.Log += LogAsync;
            _client.LoggedIn += OnLoggedIn;
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.LoggedOut += OnLoggedOut;
            _client.LatencyUpdated += OnHeartBeat;
            //_client.LeftGuild
            //_client.GuildUnavailable
            //_client.JoinedGuild
            commands.Log += LogAsync;
        }

        private static void InternalLog(string message, ConsoleColor color = ConsoleColor.DarkCyan)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now,-19} [Liber] {message}");
            Console.ResetColor();
        }

        private static Task OnGuildAvailable(SocketGuild guild)
        {

            InternalLog($"Info for {guild.Name} ({guild.Id}) downloaded.");
            return Task.CompletedTask;
        }

        private static Task OnConnected()
        {
            InternalLog($"Connected to discord.", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        private static Task OnLoggedIn()
        {
            InternalLog($"Successfully logged in to discord.", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        private static Task OnReady()
        {
            InternalLog($"Connected as {_client.CurrentUser.Username}"
                + $"#{_client.CurrentUser.Discriminator}", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        private static Task OnHeartBeat(int first, int second)
        {
            InternalLog($"HeartBeat <3 {first} : {second}", ConsoleColor.DarkMagenta);
            return Task.CompletedTask;
        }

        private static Task OnDisconnected(Exception e)
        {
            InternalLog($"Disconnected from discord: {e.Message}", ConsoleColor.DarkRed);
            return Task.CompletedTask;
        }

        private static Task OnLoggedOut()
        {
            InternalLog($"Logged out of discord.", ConsoleColor.DarkRed);
            return Task.CompletedTask;
        }

        private static Task LogAsync(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    break;
            }
            if (msg.Exception is CommandException cmdException)
            {
                Console.WriteLine($"{DateTime.Now,-19} [Command/{msg.Severity,8}]"
                    + $" {msg.Source}: {msg.Message} {cmdException.Command.Aliases.First()}"
                    + $" Failed to execute in {cmdException.Context.Channel}");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now,-19} [General/{msg.Severity,8}]"
                    + $" {msg.Source}: {msg.Message} {msg.Exception}");
            }
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
