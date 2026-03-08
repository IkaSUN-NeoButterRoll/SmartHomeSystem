using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHomeAPIProvider
{
    /// <summary>
    /// DiscordSocketClientを管理するクラス
    /// </summary>
    internal class DiscordClient
    {
        /// <summary>
        /// DiscordSocketClientのインスタンス
        /// </summary>
        private readonly DiscordSocketClient Client;

        /// <summary>
        /// Discord Botのトークン
        /// </summary>
        private readonly string Token;

        private readonly string ObservedChannelName;

        /// <summary>
        /// コマンドのプレフィックス
        /// </summary>
        private readonly string CommandPrefix;

        /// <summary>
        /// コマンドとコマンド実行関数の対応を保持するDict
        /// </summary>
        private readonly Dictionary<string, Func<SocketMessage, Task>> Commands = new();

        /// <summary>
        /// Discordクライアントのコンストラクタ
        /// </summary>
        /// <param name="token">Discord Botのトークン</param>
        /// <param name="commandPrefix">コマンドの接頭辞</param>
        /// <exception cref="ArgumentException"></exception>
        internal DiscordClient(string token, string observedChannelName, string commandPrefix = "!")
        {
            Logger.Debug("DiscordClient初期化中...");

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("トークンは空白にできません ", nameof(token));
            }

            if (string.IsNullOrWhiteSpace(observedChannelName))
            {
                throw new ArgumentException("監視するチャンネルの名前は空白にできません ", nameof(observedChannelName));
            }

            if (string.IsNullOrWhiteSpace(commandPrefix))
            {
                throw new ArgumentException("コマンドプレフィックスは空白にできません ", nameof(commandPrefix));
            }

            this.Token = token;
            this.ObservedChannelName = observedChannelName;
            this.CommandPrefix = commandPrefix;

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            // メッセージ受信のイベントハンドラーを設定する
            Client.MessageReceived += OnMessageReceivedAsync;

            Logger.Debug("DiscordClient初期化完了...");
        }

        /// <summary>
        /// Discordクライアントを起動する
        /// </summary>
        /// <returns></returns>
        internal async Task RunAsync()
        {
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            // 終了しないように待機
            await Task.Delay(-1);
        }

        /// <summary>
        /// Logのイベントハンドラーを設定する
        /// </summary>
        /// <param name="fnc">イベントハンドラー</param>
        internal void SetLog(Func<LogMessage, Task> fnc)
        {
            Client.Log += fnc;
        }

        /// <summary>
        /// メッセージ受信のイベントハンドラーを設定する
        /// </summary>
        /// <param name="func">イベントハンドラー</param>
        internal void SetMessageReceived(Func<SocketMessage, Task> func)
        {
            Client.MessageReceived += func;
        }

        /// <summary>
        /// Discordクライアントにコマンドを設定する
        /// </summary>
        /// <param name="command">コマンド</param>
        /// <param name="commandFunc">コマンドとして実行する処理</param>
        internal void SetCommands(string command, Func<SocketMessage, Task> commandFunc)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("コマンドは空白にできません", nameof(command));
            }

            Commands[command] = commandFunc;
        }

        /// <summary>
        /// コマンドが含まれるメッセージを受信したときのイベントハンドラー
        /// </summary>
        /// <param name="message">受信メッセージ</param>
        /// <returns></returns>
        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (message.Channel.Name != ObservedChannelName || message.Author.IsBot)
            {
                // 監視対象外のチャンネルまたは、Botのメッセージは無視する
                return;
            }

            string content = message.Content;

            if (!content.StartsWith(CommandPrefix))
            {
                // コマンドプレフィックスで始まらないメッセージは無視する
                return;
            }

            string commandText = content.Substring(CommandPrefix.Length);

            if (Commands.TryGetValue(commandText, out var commandFunc))
            {
                // コマンドが存在する場合、コマンド関数を実行する
                await commandFunc(message);
            }
        }
    }
}
