using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Rest;

namespace PacoBot_Station
{
    public class ModLogHandler
    {
        public enum LogField
        {
            Action,
            UserName,
            UserID,
            Reason,
        }

        public const string StevenIconUrl = "LogoUrl";

        public bool IsOpened { get; private set; }

        public ulong CurrentUserID { get; private set; }

        public const ulong ModLogChatID = 0;
        public const ulong ModChatID = 0;

        private Dictionary<LogField, object> m_CurrentModLogValues;

        private delegate Task OnModLogCommand(SocketUserMessage _message);

        private Dictionary<LogField, OnModLogCommand> m_Commands;

        private Dictionary<string, LogField> m_CommandStrings;

        private List<SocketUserMessage> m_DeleteCommandMessageIDs;

        private List<string> m_Explaination;

        public ModLogHandler()
        {
            MakeExplaination();
            m_DeleteCommandMessageIDs = new List<SocketUserMessage>();
            m_CurrentModLogValues = new Dictionary<LogField, object>();

            m_CommandStrings = new Dictionary<string, LogField>
            {
                {"action",LogField.Action },
                {"userid",LogField.UserID },
                {"reason",LogField.Reason },
            };

            m_Commands = new Dictionary<LogField, OnModLogCommand>
            {
                {LogField.Action, SetActionModLog},
                {LogField.UserID, SetUserID},
                {LogField.Reason, SetReasonModLog },
            };
        }

        public static async Task HelpMeMessage(SocketUserMessage _message, List<string> _explaination)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Timestamp = _message.Timestamp;
            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            embedBuilder.Title = "ModLog help";

            footer.Text = "Need help message discordID";
            footer.IconUrl = _message.Author.GetAvatarUrl();

            embedBuilder.Color = Color.DarkBlue;
            embedBuilder.Footer = footer;

            string _Commands = "";
            for (int i = 0; i < _explaination.Count; i++)
            {
                _Commands += ".**!modlog** " + _explaination[i] + "\n";
            }
            EmbedFieldBuilder embedFieldBuilder1 = new EmbedFieldBuilder();
            embedFieldBuilder1.WithName("**Modlog Commands**");
            embedFieldBuilder1.WithValue(_Commands);

            embedBuilder.AddField(embedFieldBuilder1);

            EmbedFieldBuilder embedFieldBuilder2 = new EmbedFieldBuilder();
            embedFieldBuilder2.WithName("**Setting Commands**");

            string _Context = "";

            _Context += $"**!acw** : add a critical word\n";
            _Context += $"**!rcw** : Remove a critical word\n";
            _Context += $"**!lcw** : Show critical word list\n";
            embedFieldBuilder2.WithValue(_Context);

            embedBuilder.AddField(embedFieldBuilder2);

            EmbedAuthorBuilder embedAuthorBuilder = new EmbedAuthorBuilder();
            embedAuthorBuilder.IconUrl = PacoBot.Client.CurrentUser.GetAvatarUrl();
            embedAuthorBuilder.Name = "PogoBot";
            embedBuilder.Author = embedAuthorBuilder;

            embedBuilder.ThumbnailUrl = PacoBot.Client.CurrentUser.GetAvatarUrl();
            Embed embed = embedBuilder.Build();

            await _message.Author.SendMessageAsync(null, false, embed);
        }

        public async Task ModLogCommand(SocketUserMessage _message)
        {
            if (_message.Author.Id != CurrentUserID && IsOpened)
                return;
            m_DeleteCommandMessageIDs.Add(_message);

            if (CheckForOpenModLog(_message) && IsOpened != true)
                OpenModLog(_message.Author);
            else if (CheckForCloseModLog(_message) && IsOpened == true)
                await CloseModLog(_message);

            string[] _commandContext = _message.Content.Split(' ');
            if (m_CommandStrings.ContainsKey(_commandContext[1]))
            {
                await m_Commands[m_CommandStrings[_commandContext[1]]]?.Invoke(_message);
            }
        }

        public bool CheckForOpenModLog(SocketUserMessage _message)
        {
            if (_message.Content.ToLower() == "!modlog open")
            {
                return true;
            }
            return false;
        }

        public bool CheckForCloseModLog(SocketUserMessage _message)
        {
            if (_message.Content.ToLower() == "!modlog close")
            {
                return true;
            }
            return false;
        }

        private async Task SetActionModLog(SocketUserMessage _message)
        {
            try
            {
                string[] _commandContext = _message.Content.Split(' ');

                if (_commandContext.Length >= 3)
                {
                    if (m_CurrentModLogValues.ContainsKey(LogField.Action))
                    {
                        m_CurrentModLogValues[LogField.Action] = _commandContext[2];
                    }
                    else
                    {
                        m_CurrentModLogValues.Add(LogField.Action, _commandContext[2]);
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception _e)
            {
                Console.WriteLine(_e);
            }
        }

        private async Task SetReasonModLog(SocketUserMessage _message)
        {
            try
            {
                string[] _commandContext = _message.Content.Split(' ');

                string _fullcontext = "";
                if (_commandContext.Length >= 3)
                {
                    for (int i = 2; i < _commandContext.Length; i++)
                    {
                        _fullcontext += _commandContext[i];
                        _fullcontext += " ";
                    }

                    if (m_CurrentModLogValues.ContainsKey(LogField.Reason))
                    {
                        m_CurrentModLogValues[LogField.Reason] = _fullcontext;
                    }
                    else
                    {
                        m_CurrentModLogValues.Add(LogField.Reason, _fullcontext);
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception _e)
            {
                Console.WriteLine(_e);
            }
        }

        private async Task SetUserID(SocketUserMessage _message)
        {
            try
            {
                string[] _commandContext = _message.Content.Split(' ');
                if (_commandContext.Length >= 3)
                {
                    ulong id = Convert.ToUInt64(_commandContext[2]);
                    RestGuildUser user = await GetUser(id);

                    if (m_CurrentModLogValues.ContainsKey(LogField.UserName))
                    {
                        m_CurrentModLogValues[LogField.UserID] = user.Id;
                        m_CurrentModLogValues[LogField.UserName] = user.Username + "#" + user.DiscriminatorValue;
                    }
                    else
                    {
                        m_CurrentModLogValues.Add(LogField.UserID, user.Id);
                        m_CurrentModLogValues.Add(LogField.UserName, user.Username + "#" + user.DiscriminatorValue);
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<RestGuildUser> GetUser(ulong id)
        {
            return await PacoBot.RestGuild.GetUserAsync(id);
        }

        private void OpenModLog(SocketUser _currentuser)
        {
            CurrentUserID = _currentuser.Id;
            IsOpened = true;
        }

        private async Task CloseModLog(SocketUserMessage _message)
        {
            if (m_CurrentModLogValues.ContainsKey(LogField.Action) && m_CurrentModLogValues.ContainsKey(LogField.UserName)
                && m_CurrentModLogValues.ContainsKey(LogField.UserID) && m_CurrentModLogValues.ContainsKey(LogField.Reason))
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Color = Color.DarkRed;
                embedBuilder.AddField("Action", m_CurrentModLogValues[LogField.Action]);
                embedBuilder.AddField("User", m_CurrentModLogValues[LogField.UserName]);
                embedBuilder.AddField("Discord ID", m_CurrentModLogValues[LogField.UserID]);
                embedBuilder.AddField("Reason", m_CurrentModLogValues[LogField.Reason]);

                RestUser _author = await PacoBot.RestGuild.GetUserAsync(CurrentUserID);

                embedBuilder.AddField("Author", _author.Username);

                RestUser _user = await PacoBot.RestGuild.GetUserAsync((ulong)m_CurrentModLogValues[LogField.UserID]);
                embedBuilder.ThumbnailUrl = _user.GetAvatarUrl();

                Embed embed = embedBuilder.Build();

                IMessageChannel _Channel = (IMessageChannel)PacoBot.Client.GetChannel(ModLogChatID);
                await _Channel.SendMessageAsync(null, false, embed);

                foreach (SocketUserMessage item in m_DeleteCommandMessageIDs)
                {
                    await item.DeleteAsync();
                }

                ExcelSheetHandler.AddOrUpdate(new ExcelSheetHandler.ModLogSaveData
                {
                    UserName = (string)m_CurrentModLogValues[LogField.UserName],
                    UserID = (ulong)m_CurrentModLogValues[LogField.UserID],
                    WarningLevel = 1,
                    Kicks = 0,
                    Banned = "No",
                    Note = (string)m_CurrentModLogValues[LogField.Reason],
                });
            }

            m_DeleteCommandMessageIDs.Clear();
            m_CurrentModLogValues.Clear();
            IsOpened = false;
            CurrentUserID = 0;
        }

        private void MakeExplaination()
        {
            m_Explaination = new List<string>()
            {
                {"**Open**: Open a new modlog to write" },
                {"**Close**: Close the log and send it in modlog chat" },
                {"**Action** [context]: Fill the action field in for a modlog" },
                {"**UserId** [DiscordID]: Set the userID in the modlog" },
                {"**Reason** [context]: Set the reason of the modlog" },
            };
        }
    }
}