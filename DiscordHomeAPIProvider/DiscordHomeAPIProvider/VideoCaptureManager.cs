using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordHomeAPIProvider
{
    /// <summary>
    /// VideoCaptureオブジェクトを管理するクラス
    /// </summary>
    internal class VideoCaptureManager
    {

        /// <summary>
        /// VideoCaptureオブジェクト
        /// </summary>
        internal VideoCapture Capture {  get; private set; }

        /// <summary>
        /// 一定時間ごとにVideoCaptureの生存確認を行うタイマー
        /// </summary>
        private readonly System.Timers.Timer AliveCheckTimer;

        /// <summary>
        /// VideoCaptureManagerクラスのコンストラクタ
        /// </summary>
        internal VideoCaptureManager()
        {
            this.Capture = GetVideoCaptureAsync().Result;
            this.AliveCheckTimer = GetAliveCheckTimer(5000, TimerElapsed);
        }

        /// <summary>
        /// VideoCaptureの生存確認用タイマーを取得する
        /// </summary>
        /// <param name="interval">生存確認のインターバル</param>
        /// <param name="elapsed">生存確認時に実行するイベント</param>
        /// <returns></returns>
        private static System.Timers.Timer GetAliveCheckTimer(int interval, System.Timers.ElapsedEventHandler elapsed)
        {
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += elapsed;
            timer.AutoReset = true;
            timer.Enabled = true;
            return timer;
        }

        /// <summary>
        /// VidecCaptureを初期化する
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Task<VideoCapture> GetVideoCaptureAsync()
        {
            return Task<VideoCapture>.Run(() =>
            {
                Logger.Debug("VideoCapture 初期化中...");

                var capture = new VideoCapture(0);
                if (!capture.IsOpened())
                {
                    Logger.Error("キャプチャデバイスを開けませんでした");
                    throw new Exception("キャプチャデバイスを開けませんでした");
                }

                Logger.Debug("VideoCapture 初期化完了...");
                return capture;

            });
        }

        /// <summary>
        /// VideoCaptureの生存確認用タイマーのイベントハンドラー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            EnsureAlive();
        }

        /// <summary>
        /// VideoCaptureが生きているか確認し、死んでいたら再初期化する
        /// </summary>
        private void EnsureAlive()
        {
            if (!Capture.IsOpened())
            {
                Capture = GetVideoCaptureAsync().Result;
            }
        }
    }
}
