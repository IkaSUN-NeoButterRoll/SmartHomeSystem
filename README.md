# SmartHomeSystem

Raspberry Pi PicoをIRコントローラー化し、複数のシステムと連携して照明などの赤外線対応機器を自動制御するスマートホームシステムです。

![toggle](https://github.com/user-attachments/assets/a744b9bd-a85c-4621-ad78-6e2ffd71686d)

## システム構成

### 全体構成

```
[スマートフォン / ブラウザ]  [DiscordHomeAPIProvider]  [LanDeviceWatcher]
             ↓                          ↓                       ↓
             └──────────────────────────┴───────────────────────┘
                                        ↓ HTTP POST
                          [RaspberryPiPicoIrController]
                                        ↓ PWM（38kHz搬送波）
                                   [赤外線LED]
                                        ↓
                               [照明などの赤外線対応機器]
```

### 操作パターン

**1. ブラウザ・スマートフォンから直接操作**
```
[ブラウザ / スマートフォン] → HTTP POST → [Pico] → [照明]
```
同一LAN内であればブラウザからコントロールパネルにアクセスして操作できます。

**2. Discordから操作**
```
[Discord] → [DiscordHomeAPIProvider] → HTTP POST → [Pico] → [照明]
```
Discordのコマンドで照明を操作します。操作前後のカメラ映像も送信されます。

**3. デバイスの接続・切断を検知して自動操作**
```
[スマートフォン] ←Ping監視→ [LanDeviceWatcher] → HTTP POST → [Pico] → [照明]
```
スマートフォンのLAN接続・切断を検知して照明を自動でON/OFFします。帰宅・外出の検知に利用できます。

---

## プロジェクト構成

| プロジェクト | 言語 | 概要 |
|---|---|---|
| [RaspberryPiPicoIrController](#raspberrypipicoircontroller) | MicroPython | PicoをWebサーバー化しIR信号を送信 |
| [LanDeviceWatcher](#landevicewatcher) | C# | LAN上のデバイスを監視しAPI連携 |
| [DiscordHomeAPIProvider](#discordhomeapiprovider) | C# | DiscordBotでカメラ映像取得・IR制御 |

---

## RaspberryPiPicoIrController

Raspberry Pi Pico WをHTTPサーバー化し、ブラウザUIまたはHTTP APIで赤外線信号を送信します。

詳細は [RaspberryPiPicoIrController/Readme.md](RaspberryPiPicoIrController/Readme.md) を参照してください。

**主な技術要素**
- HTTPサーバーをフレームワーク不使用でスクラッチ実装
- PWMで38kHz搬送波を生成して赤外線信号を送信
- GET / POST / OPTIONSのルーティングを自前実装

**動作環境**
- Raspberry Pi Pico W
- MicroPython v1.20以上

**セットアップ**

事前にPicoWのIPアドレスを静的に設定してください。

```bash
cp config.example.py config.py
# config.pyにSSIDとパスワードを設定後、全ファイルをPicoに転送
```

---

## LanDeviceWatcher

LAN上の指定デバイスをPingで監視し、接続・切断を検知してIR APIを呼び出します。

**主な技術要素**
- 一定間隔でPingを送信しデバイスの接続状態を監視
- 猶予時間（GraceMilliSeconds）を設定し、一時的な切断での誤検知を防止
- 状態変化時のみAPIを呼び出す設計（Connected → Disconnected、またはその逆）
- ログローテーション・保存期間管理付きのLoggerクラス

**動作環境**
- .NET 6以上
- Windows

**セットアップ**

事前にスマートフォン(監視対象)のIPアドレスを静的に設定してください。

```bash
cp appsettings.example.json appsettings.json
```

`appsettings.json`を編集して設定します。

```json
{
  "App": {
    "WatchTargetAddress": "192.168.0.xxx",
    "ApiUrl": "http://<PicoのIPアドレス>",
    "ApiJsonBody": "{\"class\": \"cl\", \"type\": \"pw\"}",
    "GraceMilliSeconds": "1000",
    "IntervalMilliSeconds": "5000"
  }
}
```

---

## DiscordHomeAPIProvider

Discord Botとして動作し、コマンドを受信してカメラ映像の取得・IR APIの呼び出しを行います。

**主な技術要素**
- Discord.Net を使用したBot実装
- OpenCVSharpでカメラ映像をリアルタイム取得・送信
- コマンドとハンドラをDictionaryで管理
- 監視チャンネル・コマンドプレフィックスを設定ファイルで管理

**動作環境**
- .NET 6以上
- Windows（カメラデバイスが必要）

**セットアップ**

```bash
cp appsettings.example.json appsettings.json
```

`appsettings.json`を編集して設定します。

```json
{
  "App": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "CommandPrefix": "!",
    "ObservedChannelName": "YOUR_CHANNEL_NAME",
    "ApiUrl": "http://<PicoのIPアドレス>",
    "ApiJsonBody": "{\"class\": \"cl\", \"type\": \"pw\"}"
  }
}
```

**コマンド一覧**

| コマンド | 動作 |
|---|---|
| `!ping` | 現在のカメラ映像を送信 |
| `!toggle` | 照明をトグル（操作前後の映像を送信） |

---

## 注意事項

本システムは自宅LAN内での使用を想定しています。

- RaspberryPiPicoIrControllerのAPIに認証機能はありません。外部公開する場合はAPIキー認証等の追加を推奨します。
- 各プロジェクトの設定ファイル（`config.py` / `appsettings.json`）はGit管理対象外です。`*.example.*`ファイルを参考に作成してください。
