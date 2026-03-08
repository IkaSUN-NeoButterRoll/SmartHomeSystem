using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LanDeviceWatcher
{
    internal class Logger
    {
        /// <summary>
        /// デフォルトのログ出力先のパス
        /// </summary>
        private static readonly string DEFAULT_PATH = Path.Combine(AppContext.BaseDirectory, "logs", "Logger.log");

        /// <summary>
        /// ファイルを削除しないことを示す定数
        /// </summary>
        private static readonly int DO_NOT_DELETE = -1;

        /// <summary>
        /// Loggerクラスのインスタンス
        /// </summary>
        /// <remarks>シングルトン用</remarks>
        private static Logger? Instance;

        /// <summary>
        /// アプリケーションの設定ファイルを読み込むためのIConfigurationオブジェクト
        /// </summary>
        private static readonly IConfiguration CONFIG =
            new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        /// <summary>
        /// 出力する最小ログレベル
        /// </summary>
        private readonly LogLevel Level;

        /// <summary>
        /// ログの保存先のパス
        /// </summary>
        private readonly string OutputPath;

        /// <summary>
        /// ローテーション設定
        /// </summary>
        private readonly bool RotateLog;

        /// <summary>
        /// ログの最大保存日数
        /// </summary>
        private readonly int MaximumRetentionDays;

        /// <summary>
        /// ログレベルを表す列挙型
        /// </summary>
        enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// Loggerクラスのコンストラクタ
        /// </summary>
        private Logger()
        {
            // ログレベルをappsettings.jsonから取得する
            this.Level = CONFIG["Logger:Level"] switch
            {
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Info,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                _ => LogLevel.Info
            };

            // ログの保存先のパスをappsettings.jsonから取得する
            this.OutputPath = CONFIG["Logger:LogFilePath"] ?? "";

            if (IsInvalidFilePath(OutputPath))
            {
                Console.WriteLine($"ログの保存先をデフォルトのパスに変更します");
                OutputPath = DEFAULT_PATH;
            }

            // ログのローテーション設定をappsettings.jsonから取得する
            this.RotateLog = bool.TryParse(CONFIG["Logger:RotateLog"], out bool rotate) ? rotate : false;

            // ログの最大保存日数をappsettings.jsonから取得する
            this.MaximumRetentionDays = int.TryParse(CONFIG["Logger:MaximumRetentionDays"], out int days) ? days : DO_NOT_DELETE;
        }

        /// <summary>
        /// ログの保存先のパスが無効かどうかを判定する
        /// </summary>
        /// <param name="fullPath">検証先</param>
        /// <returns>有効かどうかの真偽値</returns>
        private static bool IsInvalidFilePath(string fullPath)
        {
            bool hasInvalidChars = fullPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Console.WriteLine($"ログの保存先のパスが設定されていないまたは、空白です パス: \"{fullPath}\"");
                return true;
            }
            else if (hasInvalidChars)
            {
                Console.WriteLine($"ログの保存先のパスに無効な文字が含まれています パス: {fullPath}");
                return true;
            }
            else if (!HasAccessControll(fullPath))
            {
                Console.WriteLine($"ログの保存先のパスにアクセスできません パス: {fullPath}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// パス先にアクセスできるかどうかを判定する
        /// </summary>
        /// <param name="fullPath">アクセス先</param>
        /// <returns>アクセスできるかどうかの真偽値</returns>
        private static bool HasAccessControll(string fullPath)
        {
            try
            {
                var dirPath = Path.GetDirectoryName(fullPath) ?? "";
                if (!Directory.Exists(dirPath))
                {
                    // ディレクトリが存在しない場合は作成する
                    Directory.CreateDirectory(dirPath);
                }

                // ファイルを作成してアクセスできるかテストする
                string testFile = Path.Combine(dirPath, $"logTestFile_{Guid.NewGuid()}.tmp");
                using (FileStream fs = new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    // ファイルの書き込みテスト
                    byte[] data = System.Text.Encoding.UTF8.GetBytes("test");
                    fs.Write(data, 0, data.Length);
                }

                File.Delete(testFile);
                return true;
            }
            catch
            {
                // 例外が発生した場合はアクセスできないと判断する
                return false;
            }
        }

        /// <summary>
        /// ログのローテーションを行う
        /// </summary>
        private void RotateLogFile()
        {
            if (RotateLog && File.Exists(OutputPath))
            {
                // ローテーションする設定が有効かつ、ログファイルが存在する場合はローテーションする

                DateTime lastWrite = File.GetLastWriteTime(OutputPath);
                if (lastWrite.Date == DateTime.Now.Date)
                {
                    // ファイルの最終更新日が今日の場合はローテーションしない
                    return;
                }

                string dir = Path.GetDirectoryName(OutputPath) ?? "";
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(OutputPath);
                string ext = Path.GetExtension(OutputPath);

                string newFileName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string newPath = Path.Combine(dir, newFileName);

                File.Move(OutputPath, newPath);
            }
        }

        /// <summary>
        /// 保存期間が過ぎたログファイルを削除する
        /// </summary>
        private void DeleteExpiredFiles()
        {
            if (MaximumRetentionDays == DO_NOT_DELETE)
            {
                // ログを削除しない設定の場合は何もしない
                return;
            }
            string dir = Path.GetDirectoryName(OutputPath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(OutputPath);
            string ext = Path.GetExtension(OutputPath);
            var logFiles = Directory.GetFiles(dir, $"{fileNameWithoutExt}_*{ext}");
            foreach (var file in logFiles)
            {
                DateTime lastWrite = File.GetLastWriteTime(file);
                if ((DateTime.Now - lastWrite).TotalDays > MaximumRetentionDays)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // ファイルの削除に失敗した場合は無視する
                    }
                }
            }
        }

        /// <summary>
        /// ログ出力先のディレクトリとファイルが存在することを保証する
        /// </summary>
        private void GuaranteeLogDirectoryAndFile()
        {
            var dir = Path.GetDirectoryName(OutputPath);
            if (!Path.Exists(dir))
            {
                // ログの保存先のディレクトリが存在しない場合は作成する
                Directory.CreateDirectory(dir);
            }
            if (!File.Exists(OutputPath))
            {
                // ファイルが存在しない場合は作成する
                using (FileStream fs = File.Create(OutputPath))
                {

                }
            }
        }

        /// <summary>
        /// ログを出力する
        /// </summary>
        /// <param name="message">ログ本体</param>
        private void WriteLog(LogLevel level, string message)
        {
            // ログレベルが出力する最小ログレベル以上の場合はログを出力する
            if (level >= Level)
            {
                RotateLogFile();

                DeleteExpiredFiles();

                GuaranteeLogDirectoryAndFile();

                // ログをコンソールとファイルに出力する
                Console.WriteLine(message);
                using (StreamWriter sw = new StreamWriter(OutputPath, true))
                {
                    sw.WriteLine(message);
                }
            }

        }

        /// <summary>
        /// デバッグレベルログを出力する
        /// </summary>
        /// <param name="message">ログ本体</param>
        internal static void Debug(object message)
        {
            Write(LogLevel.Debug, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DEBUG {message}");
        }

        /// <summary>
        /// インフォレベルログを出力する
        /// </summary>
        /// <param name="message">ログ本体</param>
        internal static void Info(object message)
        {
            Write(LogLevel.Info, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] INFO {message}");
        }

        /// <summary>
        /// 警告レベルログを出力する
        /// </summary>
        /// <param name="message">ログ本体</param>
        internal static void Warn(object message)
        {
            Write(LogLevel.Warning, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] WARNING: {message}");
        }

        /// <summary>
        /// エラーレベルログを出力する
        /// </summary>
        /// <param name="message">ログ本体</param>
        internal static void Error(object message)
        {
            Write(LogLevel.Error, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: {message}");
        }

        /// <summary>
        /// ログを出力するための共通メソッド
        /// </summary>
        /// <param name="message">ログ本体</param>
        private static void Write(LogLevel level, string message)
        {
            // Loggerクラスのインスタンスが存在しない場合は作成する
            Instance ??= new Logger();
            Instance.WriteLog(level, message);
        }
    }
}
