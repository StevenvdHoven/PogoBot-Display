using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
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

        private IMessageChannel m_channel;

        private BehaviorHandler m_BehaviorChecker;

        private CommandHandler m_CommandHandler;
        private ModLogHandler m_ModLogHandler;

        public async Task MainAsync()
        {
            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AlwaysDownloadUsers = true;

            Client = new DiscordSocketClient(config);

            m_channel = (ITextChannel)Client.GetChannel(843239738437533697);
            Client.MessageReceived += OnMesssage;
            Client.ReactionAdded += OnReaction;

            CommandServiceConfig commandServiceConfig = new CommandServiceConfig();

            m_ModLogHandler = new ModLogHandler();
            m_CommandHandler = new CommandHandler(new CommandService(commandServiceConfig), m_ModLogHandler);
            m_BehaviorChecker = new BehaviorHandler();

            ExcelSheetHandler.ConnectSheet();
            await m_CommandHandler.InstallCommandsAsync();

            string token = "Token";

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
            if (Guild == null)
            {
                Guild = Client.GetGuild(GuildID);
                RestClient = Client.Rest;
                RestGuild = await RestClient.GetGuildAsync(GuildID);
            }

            SocketUserMessage message = _message as SocketUserMessage;

            if (message == null)
                return;

            string[] _contextMessage = message.Content.Split(' ');

            SocketGuildUser _user = message.Author as SocketGuildUser;
            if (_contextMessage[0].ToLower() == "!modlog" && CommandHandler.AllowedRole(_user))
            {
                await m_ModLogHandler.ModLogCommand(message);
                return;
            }

            if (_contextMessage[0][0] != '!')
            {
                BehaviorCheckFeedBack _feedback = await BehaviorHandler.IsBadWord(message);
                if (_feedback.IsBad)
                {
                    await m_CommandHandler.AddressBadWork(message, _feedback);
                }
            }
        }

        public static async Task SendMessage(ISocketMessageChannel _Channel, string msg)
        {
            await _Channel.SendMessageAsync(msg);
        }
    }
}