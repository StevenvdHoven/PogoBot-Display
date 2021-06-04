using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord.Rest;

namespace PacoBot_Station
{
    internal class CommandHandler
    {
        public static List<ulong> ModlogRoles { get; private set; }

        public delegate Task CommandTask(SocketUserMessage _message);

        public Dictionary<string, CommandTask> m_Commands;
        public ModLogHandler ModLogHandler;

        public CommandService CommandService;

        public CommandHandler(CommandService _Service, ModLogHandler _modLogHandler)
        {
            CommandService = _Service;
            ModLogHandler = _modLogHandler;
            SetModLogRoles();
        }

        public static bool AllowedRole(SocketGuildUser _user)
        {
            if (_user == null)
            {
                return false;
            }
            foreach (ulong item in ModlogRoles)
            {
                SocketRole _role = PacoBot.Guild.GetRole(item);
                List<SocketRole> _userRoles = _user.Roles.ToList();
                if (_userRoles.Contains(_role))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task InstallCommandsAsync()
        {
            PacoBot.Client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await CommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private void SetModLogRoles()
        {
            ModlogRoles = new List<ulong>()
            {
                { 816047479589961780 },
                { 603328495829123093 },
            };
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(PacoBot.Client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(PacoBot.Client, message);

            SocketGuildUser _user = message.Author as SocketGuildUser;
            if (AllowedRole(_user))
            {
                // Execute the command with the command context we just
                // created, along with the service provider for precondition checks.
                await CommandService.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: null);
            }
        }

        public async Task AddressBadWork(SocketUserMessage _message, BehaviorCheckFeedBack _feedback)
        {
            string _messageContext = "";
            if (_feedback.Context == null)
            {
                _messageContext = _message.Content;
            }
            else
            {
                _messageContext = _feedback.Context;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = "Warning Log";
            embedBuilder.Color = Color.Red;
            embedBuilder.AddField("Discord User", _message.Author);
            embedBuilder.AddField("Context:", _messageContext);
            embedBuilder.WithAuthor(PacoBot.Client.CurrentUser);
            embedBuilder.ThumbnailUrl = _message.Author.GetAvatarUrl();

            Embed embed = embedBuilder.Build();
            IMessageChannel _channel = PacoBot.Client.GetChannel(ModLogHandler.ModChatID) as IMessageChannel;
            string _mentions = "";
            for (int i = 0; i < ModlogRoles.Count; i++)
            {
                SocketRole _role = PacoBot.Guild.GetRole(ModlogRoles[i]);
                _mentions += _role.Mention + " ";
            }

            await _channel.SendMessageAsync(_mentions, false, embed);

            if (_feedback.DeleteMessages != null)
            {
                foreach (RestMessage item in _feedback.DeleteMessages)
                {
                    if (_feedback.Context.Contains(item.Content.ToLower()))
                        await item.DeleteAsync();
                }
            }
        }
    }

    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("Gives you all commands")]
        public async Task ReplyToPerson()
        {
            List<string> _explaination = new List<string>()
            {
                {"**Open**: Open a new modlog to write" },
                {"**Close**: Close the log and send it in modlog chat" },
                {"**Action** [context]: Fill the action field in for a modlog" },
                {"**UserId** [DiscordID]: Set the userID in the modlog" },
                {"**Reason** [context]: Set the reason of the modlog" },
            };

            await ModLogHandler.HelpMeMessage(Context.Message, _explaination);

            await Context.Message.DeleteAsync();
        }
    }

    public class ExcelSheetCommand : ModuleBase<SocketCommandContext>
    {
        [Command("excel")]
        [Summary("excel reqeust")]
        public async Task ExcelReqeust()
        {
            ExcelSheetHandler.SaveData();
            await Task.CompletedTask;
        }
    }
}