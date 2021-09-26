using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord.API;
using Discord.Rest;

namespace PacoBot_Station
{
    public class PacoBot
    {
        public const ulong GuildID = 0;

        public static DiscordSocketClient Client;
        public static DiscordSocketRestClient RestClient;
        public static SocketGuild Guild;
        public static RestGuild RestGuild;

        private CommandService m_Command;

        private BehaviorHandler m_BehaviorChecker;

        private CommandHandler m_CommandHandler;
        private ModLogHandler m_ModLogHandler;

        public async Task MainAsync()
        {
            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            Client = new DiscordSocketClient(config);

            Client.MessageReceived += OnMesssage;
            Client.ReactionAdded += OnReaction;

            CommandServiceConfig commandServiceConfig = new CommandServiceConfig();

            m_ModLogHandler = new ModLogHandler();
            m_CommandHandler = new CommandHandler(new CommandService(commandServiceConfig), m_ModLogHandler);
            m_BehaviorChecker = new BehaviorHandler();

            ExcelSheetHandler.ConnectSheet();
            await m_CommandHandler.InstallCommandsAsync();

            string token = "token";

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task OnReaction(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage socketUserMessage = await message.DownloadAsync();

            await Task.CompletedTask;
        }

        private async Task OnMesssage(SocketMessage _message)
        {
            try
            {
                if (Guild == null)  // if the guild is null we second check for it
                {
                    Guild = Client.GetGuild(GuildID);
                    RestClient = Client.Rest;
                    RestGuild = await RestClient.GetGuildAsync(GuildID);
                }

                SocketUserMessage message = _message as SocketUserMessage;  // Converting the SocketMessage to a SocketUserMessage for easier use.

                if (message == null)    // If message is NULL we stop and return.
                    return;

                StartBadWordThreading(message); // We start the bad word algeritm to check if we need to send a warning.

                string[] _contextMessage = message.Content.Split(' ');        // Split up the message in bits so we can see the command.

                SocketGuildUser _user = message.Author as SocketGuildUser;    // Getting the user of the message.

                StartModLogThread(message, _contextMessage, _user);           // Starting the modlog thread if that command is active.
                StartBehaviorCommandThread(message, _contextMessage, _user);  // Starting the behavior thread if that command if active.
            }
            catch (Exception _E)
            {
                Console.WriteLine(_E);
            }
        }

        private void StartBehaviorCommandThread(SocketUserMessage message, string[] _contextMessage, SocketGuildUser _user)
        {
            if (_contextMessage[0].Contains('!') && CommandHandler.AllowedRole(_user))
            {
                Task task = BehaviorHandler.OnCommand(message);
                task.Wait();
                return;
            }
        }

        private void StartModLogThread(SocketUserMessage message, string[] _contextMessage, SocketGuildUser _user)
        {
            if (_contextMessage[0].ToLower() == "!modlog" && CommandHandler.AllowedRole(_user))
            {
                Thread _thread = new Thread(new ThreadStart(async () =>
                {
                    await m_ModLogHandler.ModLogCommand(message);
                }));
                _thread.Start();
            }
        }

        private void StartBadWordThreading(SocketUserMessage message)
        {
            if (message.Content.ToLower().StartsWith("!") && CommandHandler.AllowedRole(message.Author as SocketGuildUser))
                return;
            Thread _thread = new Thread(new ThreadStart(async () =>
            {
                BehaviorCheckFeedBack _feedback = await BehaviorHandler.IsBadWord(message);

                if (_feedback.IsBad)
                {
                    Thread _tempThread = new Thread(new ThreadStart(async () =>
                    {
                        await m_CommandHandler.AddressBadWork(message, _feedback);
                    }));
                    _tempThread.Start();
                }
            }));

            if (message.Author.IsBot == false)
                _thread.Start();
        }

        public static async Task SendMessage(ISocketMessageChannel _Channel, string msg)
        {
            await _Channel.SendMessageAsync(msg);
        }
    }
}