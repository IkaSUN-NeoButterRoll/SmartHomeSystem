using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using OpenCvSharp;
using System.Diagnostics.Metrics;
using System.Text;
using System.Timers;

namespace DiscordHomeAPIProvider
{
    internal class Program
    {
        /// <summary>
        /// VideoCaptureオブジェクトを管理するVideoCaptureManagerクラスのインスタンス
        /// </summary>
        private static readonly VideoCaptureManager VCM = new VideoCaptureManager();

        /// <summary>
        /// アプリケーションの設定ファイルを読み込むためのIConfigurationオブジェクト
        /// </summary>
        private static readonly IConfiguration CONFIG =
            new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        /// <summary>
        /// Discord Botのトークン
        /// AppSettings.jsonのApp:BotTokenに記載されている値を使用する
        /// 値が存在しない場合は空文字列を使用する
        /// </summary>
        private static readonly string TOKEN = CONFIG["App:BotToken"] ?? "";

        /// <summary>
        /// コマンドのプレフィックス
        /// </summary>
        private static readonly string COMMAND_PREFIX = CONFIG["App:CommandPrefix"] ?? "";

        /// <summary>
        /// 監視するチャンネルの名前
        /// </summary>
        private static readonly string OBSERVED_CHANNEL_NAME = CONFIG["App:ObservedChannelName"] ?? "";

        /// <summary>
        /// APIを呼び出すためのURL
        /// </summary>
        private static readonly string API_URL = CONFIG["App:ApiUrl"] ?? "";

        /// <summary>
        /// APIを呼び出すためのJSON形式のリクエストボディ
        /// </summary>
        private static readonly string API_JSON_BODY = CONFIG["App:ApiJsonBody"] ?? "";


        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var discordClient = new DiscordClient(TOKEN, OBSERVED_CHANNEL_NAME, COMMAND_PREFIX);

            // ログイベントの設定
            discordClient.SetLog(async (log) =>
            {
                Logger.Info(log.ToString());
                await Task.CompletedTask;
            });

            // メッセージ受信イベントの設定
            discordClient.SetMessageReceived(async (message) =>
            {
                await Task.Run(() =>
                {
                    Logger.Info($"Channel: {message.Channel.Name}, Author: {message.Author.Username}, Message: {message}");
                });
            });

            // 生存確認兼、点灯状態確認コマンド
            discordClient.SetCommands("ping", async (message) =>
            {
                await message.Channel.SendMessageAsync("Pong!");


                using var frame = new Mat();
                VCM.Capture.Read(frame);
                if (frame.Empty())
                {
                    Logger.Error("フレーム取得失敗");
                    return;
                }
                Cv2.ImEncode(".jpg", frame, out byte[] imageBytes);
                using var stream = new MemoryStream(imageBytes);

                await message.Channel.SendFileAsync(stream, "test.jpg");
            });

            // 切り替えコマンド
            discordClient.SetCommands("toggle", async (message) =>
            {

                using var frame1 = new Mat();
                VCM.Capture.Read(frame1);
                if (frame1.Empty())
                {
                    Logger.Error("フレーム取得失敗");
                    return;
                }
                Cv2.ImEncode(".jpg", frame1, out byte[] imageBytes1);
                using var stream1 = new MemoryStream(imageBytes1);
                

                await message.Channel.SendFileAsync(stream1, "test.jpg");

                // API呼び出しが完了するまで待機
                await CallAPI();

                using var frame = new Mat();
                VCM.Capture.Read(frame);
                if (frame.Empty())
                {
                    Logger.Error("フレーム取得失敗");
                    return;
                }
                Cv2.ImEncode(".jpg", frame, out byte[] imageBytes);
                using var stream = new MemoryStream(imageBytes);


                await message.Channel.SendFileAsync(stream, "test.jpg");

            });

            // Botの起動
            await discordClient.RunAsync();
        }



        /// <summary>
        /// IR APIを呼び出すための非同期メソッド
        /// </summary>
        private static async Task CallAPI()
        {
            using (var client = new HttpClient())
            using (var content = new StringContent(API_JSON_BODY, Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.ConnectionClose = true;
                try
                {
                    HttpResponseMessage response = await client.PostAsync(API_URL, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Logger.Info($"Status code: {(int)response.StatusCode}");
                    Logger.Info($"Response: {responseBody}");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error: " + ex.Message);
                }
            }
        }
    }
}
