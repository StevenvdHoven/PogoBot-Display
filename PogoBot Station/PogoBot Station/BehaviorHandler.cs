using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Rest;
using System.Linq;
using Newtonsoft.Json;

namespace PacoBot_Station
{
    public class BehaviorHandler
    {
        public static List<string> m_CriticalWords;

        private delegate Task OnCommandTask(SocketUserMessage Context);

        private static Dictionary<string, OnCommandTask> Commands;

        public BehaviorHandler()
        {
            LoadBadWords();

            Commands = new Dictionary<string, OnCommandTask>
            {
                { "!acw",CheckAddWord },
                { "!rcw",CheckRemoveWord },
                { "!lcw",GetWords },
            };
        }

        public static async Task OnCommand(SocketUserMessage _message)
        {
            string _command = _message.Content.Split(' ')[0];
            if (Commands.ContainsKey(_command.ToLower()))
            {
                await Commands[_command]?.Invoke(_message);
            }
        }

        public static async Task<BehaviorCheckFeedBack> IsBadWord(SocketUserMessage _message)
        {
            if (StandardLoopCheck(_message.Content.ToLower()))
                return new BehaviorCheckFeedBack
                {
                    IsBad = true,
                    Context = null,
                };
            else
            {
                BehaviorCheckFeedBack feedback = await DetailedLoopCheck(_message);
                if (feedback.IsBad)
                {
                    return feedback;
                }
            }

            return new BehaviorCheckFeedBack
            {
                IsBad = false,
            };
        }

        public static async Task AddWord(string _word)
        {
            if (m_CriticalWords.Contains(_word))
                await Task.CompletedTask;

            m_CriticalWords.Add(_word);
            SaveWords();
            await Task.CompletedTask;
        }

        public static async Task RemoveWord(string _word)
        {
            if (!m_CriticalWords.Contains(_word))
                await Task.CompletedTask;
            m_CriticalWords.Remove(_word);
            SaveWords();
            await Task.CompletedTask;
        }

        private async Task GetWords(SocketUserMessage Context)
        {
            if (Context != null)
            {
                if (m_CriticalWords == null)
                {
                    await PacoBot.SendMessage(Context.Channel, "Critial List is empty/NULL restart bot!!!");
                }
                else
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.Timestamp = Context.Timestamp;

                    embedBuilder.Title = "Behavior:[Critical Words]";

                    embedBuilder.Color = Color.Red;

                    EmbedFieldBuilder embedFieldBuilder = new EmbedFieldBuilder();
                    embedFieldBuilder.WithName("[Bad Words]");

                    string _words = "";
                    for (int i = 0; i < m_CriticalWords.Count; i++)
                    {
                        _words += $"{m_CriticalWords[i]}\n";
                    }
                    if (_words == "")
                    {
                        _words = "None";
                    }

                    embedFieldBuilder.WithValue(_words);
                    embedBuilder.AddField(embedFieldBuilder);

                    EmbedAuthorBuilder embedAuthorBuilder = new EmbedAuthorBuilder();
                    embedAuthorBuilder.IconUrl = PacoBot.Client.CurrentUser.GetAvatarUrl();
                    embedAuthorBuilder.Name = "PogoBot";
                    embedBuilder.Author = embedAuthorBuilder;

                    embedBuilder.ThumbnailUrl = PacoBot.Client.CurrentUser.GetAvatarUrl();
                    Embed embed = embedBuilder.Build();

                    await Context.Channel.SendMessageAsync(null, false, embed);
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Context is null dont know why");
            }
        }

        private async Task CheckAddWord(SocketUserMessage Context)
        {
            string[] _context = Context.Content.Split(' ');
            if (_context[_context.Length - 1].ToLower() == "acw")
                await Task.CompletedTask;

            await AddWord(_context[_context.Length - 1]);
            await Context.Channel.SendMessageAsync($"[{_context[_context.Length - 1]}] Has been added to the critical Words.");
        }

        private async Task CheckRemoveWord(SocketUserMessage Context)
        {
            string[] _context = Context.Content.Split(' ');
            if (_context[_context.Length - 1].ToLower() == "rcw")
                await Task.CompletedTask;

            await RemoveWord(_context[_context.Length - 1]);

            await Context.Channel.SendMessageAsync($"[{_context[_context.Length - 1]}] Has been Remove from critical Words.");
        }

        private static void SaveWords()
        {
            using (StreamWriter stream = File.CreateText("Critalwords_data.json"))
            {
                SavedCritalWords savedCritalWords = new SavedCritalWords
                {
                    IsSaved = true,
                    Words = m_CriticalWords,
                };
                string _jsonstring = JsonConvert.SerializeObject(savedCritalWords);
                stream.Write(_jsonstring);
                stream.Close();
            }
        }

        private static SavedCritalWords LoadCritcalWords()
        {
            if (File.Exists("Critalwords_data.json") == false)
                return new SavedCritalWords { IsSaved = false, Words = new List<string>(), };

            using (StreamReader stream = File.OpenText("Critalwords_data.json"))
            {
                string _ReadedEnd = stream.ReadToEnd();
                SavedCritalWords critalWords = JsonConvert.DeserializeObject<SavedCritalWords>(_ReadedEnd);

                stream.Close();
                return critalWords;
            }
        }

        private void LoadBadWords()
        {
            SavedCritalWords _data = LoadCritcalWords();
            if (_data.IsSaved == false)
            {
                m_CriticalWords = new List<string>()
                {
                    "nigger",
                    "faggot",
                    "cancer",
                    "n1gger",
                    "n!gger",
                    "fagg0t",
                    "faggat",
                    "fegget",
                    "testf",
                };
                SaveWords();
                return;
            }
            else
            {
                m_CriticalWords = _data.Words;
                return;
            }
        }

        private static bool StandardLoopCheck(string _context)
        {
            foreach (string item in m_CriticalWords)
            {
                if (_context.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<BehaviorCheckFeedBack> DetailedLoopCheck(SocketUserMessage _message)
        {
            ulong _currentID = _message.Author.Id;

            RestGuildChannel _channel = await PacoBot.RestGuild.GetChannelAsync(_message.Channel.Id);

            IRestMessageChannel restMessageChannel = _channel as IRestMessageChannel;

            if (_channel != null)
            {
                foreach (string word in m_CriticalWords)
                {
                    try
                    {
                        List<RestMessage> _deleteMessages = new List<RestMessage>();
                        IAsyncEnumerable<IReadOnlyCollection<RestMessage>> messages = restMessageChannel.GetMessagesAsync(word.Length);
                        List<IReadOnlyCollection<RestMessage>> listmessages = await messages.ToListAsync();
                        IEnumerator<RestMessage> enumerator = listmessages[0].GetEnumerator();

                        string _curseWord = "";

                        while (enumerator.MoveNext())
                        {
                            RestMessage _currentMessage = enumerator.Current;
                            if (_currentID == _currentMessage.Author.Id)
                            {
                                _curseWord += _currentMessage.Content.ToLower();
                                _deleteMessages.Add(_currentMessage);
                            }
                        }
                        string _reversedWord = "";
                        for (int i = _curseWord.Length - 1; i >= 0; i--)
                        {
                            _reversedWord += _curseWord[i];
                        }

                        if (_reversedWord == word)
                        {
                            return new BehaviorCheckFeedBack
                            {
                                IsBad = true,
                                Context = _reversedWord,
                                DeleteMessages = _deleteMessages.ToArray(),
                            };
                        }
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                }
            }
            return default;
        }
    }

    public struct BehaviorCheckFeedBack
    {
        public bool IsBad;
        public string Context;
        public RestMessage[] DeleteMessages;
    }

    [Serializable]
    public struct SavedCritalWords
    {
        public bool IsSaved;
        public List<string> Words;
    }
}