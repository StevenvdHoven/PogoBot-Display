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

namespace PacoBot_Station
{
    public class BehaviorHandler
    {
        public static List<string> m_CriticalWords;

        public BehaviorHandler()
        {
            LoadBadWords();
        }

        public static async Task<BehaviorCheckFeedBack> IsBadWord(SocketUserMessage _message)
        {
            if (StandardLoopCheck(_message.Content))
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

        private void LoadBadWords()
        {
            m_CriticalWords = new List<string>()
            {
                "nigger",
                "faggot",
                "cancer",
                "dumbass",
                "n1gger",
                "n!gger",
                "fagg0t",
                "faggat",
                "fegget",
            };
        }

        private static bool StandardLoopCheck(string _context)
        {
            foreach (string item in m_CriticalWords)
            {
                if (item == _context)
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
}