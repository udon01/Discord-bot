using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Discord.Rest;
using System.Text.RegularExpressions;

namespace Discord翻訳bot_Deepl_
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _client = new DiscordSocketClient
                (new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent |
                             GatewayIntents.GuildMessages
            }
            );
            _client.Log += LogAsync;
            _client.Ready += onReady;
            _client.MessageReceived += onMessage;
            //_client.ReactionAdded += ReactionAsync;
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, "MyToken");
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task onReady()
        {
            Console.WriteLine($"{_client.CurrentUser} is Running!!");
            return Task.CompletedTask;
        }

        public async Task onMessage(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            string targetlang = "";
            
            if (message.Author is SocketGuildUser socketUser)
            {
                SocketGuild socketGuild = socketUser.Guild;
                SocketRole socketRole = socketGuild.GetRole(1097210685479460955);
                if (socketUser.Roles.Any(r => r.Id == socketRole.Id))
                    targetlang = "target_lang=EN";
                else
                    targetlang = "target_lang=JA";
            }
            if (targetlang == "target_lang=EN")
            {
                if (IsJapanese(message.Content) == false || message.Content.Length < 2)
                    return;
            }
            if (targetlang == "target_lang=JA")
            {
                if (IsEnglish(message.Content) == true || message.Content.Length < 2)
                    return;
            }

            string usermessage = "";

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api-free.deepl.com/v2/translate"))
                {
                    var contentList = new List<string>
                    {
                        "auth_key=MyAPIKey",
                        "text=" + message.Content,
                        targetlang
                    };

                    request.Content = new StringContent(string.Join("&", contentList));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);
                    
                    var resBodyStr = response.Content.ReadAsStringAsync().Result;
                    JObject deserial = (JObject)JsonConvert.DeserializeObject(resBodyStr);
                    usermessage = deserial["translations"][0]["text"].ToString();
                }
            }

            await message.Channel.SendMessageAsync(usermessage);
        }
        /*
        public async Task ReactionAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (_client.GetUser(reaction.UserId).IsBot)
                return;

            if (reaction.Emote.Name == "🇯🇵")
            {
                string usermessage = "";

                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api-free.deepl.com/v2/translate"))
                    {
                        var contentList = new List<string>();
                        contentList.Add("auth_key=MyAPIKey");
                        contentList.Add(channel.GetMessageAsync(Bot.MessageStatisticsID));
                        var messages = await Context.Channel
                   .GetMessagesAsync(Context.Message, Direction.Before, 1)
                   .FlattenAsync();
                        contentList.Add("target_lang=JA");

                        request.Content = new StringContent(string.Join("&", contentList));
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                        var response = await httpClient.SendAsync(request);

                        var resBodyStr = response.Content.ReadAsStringAsync().Result;
                        JObject deserial = (JObject)JsonConvert.DeserializeObject(resBodyStr);
                        usermessage = deserial["translations"][0]["text"].ToString();
                    }
                }

                ulong channelId = channel.Id;
                var toOtherChannel = _client.GetChannel(channelId) as IMessageChannel;
                await toOtherChannel.SendMessageAsync(usermessage);
            }

            if (reaction.Emote.Name == "🇺🇸")
            {
                string usermessage = "";

                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api-free.deepl.com/v2/translate"))
                    {
                        var contentList = new List<string>();
                        contentList.Add("auth_key=MyAPIKey");
                        contentList.Add(message.ToString());
                        contentList.Add("target_lang=EN");

                        request.Content = new StringContent(string.Join("&", contentList));
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                        var response = await httpClient.SendAsync(request);

                        var resBodyStr = response.Content.ReadAsStringAsync().Result;
                        JObject deserial = (JObject)JsonConvert.DeserializeObject(resBodyStr);
                        usermessage = deserial["translations"][0]["text"].ToString();
                    }
                }

                ulong channelId = channel.Id;
                var toOtherChannel = _client.GetChannel(channelId) as IMessageChannel;
                var embed = new EmbedBuilder();
                embed.WithColor(0x00B8FF);
                await toOtherChannel.SendMessageAsync(usermessage);
            }
        }
        */

        private bool IsJapanese(string text)
        {
            var isJapanese = Regex.IsMatch(text, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]+");
            return isJapanese;
        }
        public static bool IsEnglish(string text)
        {
            var isEnglish = Regex.IsMatch(text, "^[a-zA-Z]*$");
            return isEnglish;
        }
    }
}
