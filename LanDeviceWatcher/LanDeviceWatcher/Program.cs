using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LanDeviceWatcher
{
    class Program
    {
        /// <summary>
        /// デフォルトの猶予時間(ms)
        /// </summary>
        private static readonly int DEFAULT_GRACE_MS = 1000;

        /// <summary>
        /// デフォルトの監視インターバル(ms)
        /// </summary>
        private static readonly int DEFAULT_INTERVAL_MS = 5000;

        /// <summary>
        /// アプリケーションの設定ファイルを読み込むためのIConfigurationオブジェクト
        /// </summary>
        private static readonly IConfiguration CONFIG =
            new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        /// <summary>
        /// 監視対象のIPアドレス
        /// </summary>
        private static readonly string TARGET_IP = CONFIG["App:WatchTargetAddress"] ?? "";

        /// <summary>
        /// APIのURL
        /// </summary>
        private static readonly string API_URL = CONFIG["App:ApiUrl"] ?? "";

        /// <summary>
        /// APIに投げるJson
        /// </summary>
        private static readonly string API_JSON_BODY = CONFIG["App:ApiJsonBody"] ?? "";

        /// <summary>
        /// 状態遷移の猶予時間(ms)
        /// 一時的にネットワークが切れた場合などに、すぐに状態をDisconnectedにしないための猶予時間
        /// </summary>
        private static readonly int GRACE_MILLISECONDS = int.TryParse(CONFIG["App:GraceMilliSeconds"], out int graceMillSec) ? graceMillSec : DEFAULT_GRACE_MS;

        /// <summary>
        /// 監視のインターバル(ms)
        /// </summary>
        private static readonly int INTERVAL_MILLISECONDS = int.TryParse(CONFIG["App:IntervalMilliSeconds"], out int graceMillSec) ? graceMillSec : DEFAULT_INTERVAL_MS;

        /// <summary>
        /// 監視用のタイマー
        /// </summary>
        private static System.Timers.Timer timer;

        /// <summary>
        /// 最後にデバイスが見つかった時間
        /// </summary>
        private static DateTime lastFoundTime = DateTime.Now;

        /// <summary>
        /// 接続状態
        /// </summary>
        private static string state = "Connected";

        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = false;
            Logger.Info($"Start watching... target IP:{TARGET_IP}");

            // 監視用のタイマーを設定し、一定間隔でデバイスの状態をチェックする
            timer = new System.Timers.Timer(INTERVAL_MILLISECONDS);
            timer.Elapsed += TimerElapsed;
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.ReadLine(); // 終了待機
            timer.Stop();
            timer.Dispose();
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckDevice(TARGET_IP);
        }

        /// <summary>
        /// 監視対象のIPアドレスに対してPingを送り、接続状態を確認するメソッド
        /// </summary>
        /// <param name="ip">監視対象のIP</param>
        private static void CheckDevice(string ip)
        {
            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(ip, 1000); // タイムアウト1秒
                    if (reply.Status == IPStatus.Success)
                    {
                        // デバイスが見つかった場合、状態がDisconnectedからConnectedに変わるときにAPIを呼び出す
                        if (state == "Disconnected")
                        {
                            Logger.Info($"{ip}: Connected");
                            state = "Connected";
                            CallAPI().Wait();
                        }

                        lastFoundTime = DateTime.Now;
                    }
                    else
                    {
                        // デバイスが一定時間見つからない場合、状態がConnectedからDisconnectedに変わるときにAPIを呼び出す
                        if (state == "Connected" && lastFoundTime.AddMilliseconds(GRACE_MILLISECONDS) < DateTime.Now)
                        {
                            Logger.Info($"{ip}: Disconnected");
                            state = "Disconnected";
                            CallAPI().Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error: {ex.Message}");
                }
            }
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
