# RaspberryPi Pico IR Controller

Raspberry Pi PicoをWebサーバー化し、ブラウザUIまたはHTTP APIで赤外線信号を送信するシステムです。

## 概要

```
[ブラウザ / 外部アプリ]
        ↓ HTTP POST
[Raspberry Pi Pico（Webサーバー）]
        ↓ PWM（38kHz搬送波）
[赤外線LED]
        ↓
[照明などの赤外線対応機器]
```

- Pico自身がHTTPサーバーとして動作し、フレームワーク不使用でHTTP通信をスクラッチ実装
- 38kHz PWMで赤外線信号を生成・送信
- ブラウザからボタン操作、またはAPIを直接叩いて操作可能
- 同一LAN内の他システムと連携可能（例：デバイス監視システムと組み合わせて自動制御）

## 動作環境

- Raspberry Pi Pico W
- MicroPython v1.20以上

## ハードウェア構成

| 部品 | 接続先 |
|------|--------|
| 赤外線LED | GP15 |
| 内蔵LED | LED（ステータス表示用） |

## セットアップ

**1. config.pyを作成する**

```bash
cp config.example.py config.py
```

`config.py`を編集してWi-FiのSSIDとパスワード、送信繰り返し回数を設定します。

```python
SSID = "your_ssid"
PASSWORD = "your_password"
REPEAT_COUNT = 1  # 赤外線信号の送信繰り返し回数
```

> **`REPEAT_COUNT` について**
> 赤外線信号を送信する繰り返し回数です。受信側の感度が低い場合や誤動作が多い場合は値を大きくすることで改善することがあります。通常は`1`で問題ありません。

**2. ファイルをPicoに転送する**

以下のファイルを全てPicoのルートディレクトリに転送します。

```
main.py
irUtility.py
webUtility.py
config.py
page.html
style.css
scripts.js
```

**3. Picoを起動する**

起動するとWi-Fiに接続し、LEDの点滅でステータスを表示します。

| 点滅パターン | 状態 |
|------------|------|
| 2回点滅 | 起動・初期化開始 |
| 3回点滅 | Wi-Fi接続成功 |
| 4回点滅 | サーバー起動完了 |
| 高速点滅 | エラー（Wi-Fi接続失敗など） |

シリアルコンソールにIPアドレスが表示されます。

## 使い方

### ブラウザから操作

`http://<PicoのIPアドレス>/` にアクセスするとコントロールパネルが表示されます。

### APIから操作

```bash
curl -X POST http://<PicoのIPアドレス> \
  -H "Content-Type: application/json" \
  -d '{"class": "cl", "type": "pw"}'
```

#### コマンド一覧

| type | 動作 |
|------|------|
| `pw` | 電源ON/OFF |
| `nl` | 常夜灯 |
| `bt` | 明るく |
| `dk` | 暗く |
| `wt` | 昼白色 |
| `wc` | 電球色 |
| `fp` | 全灯 |
| `rb` | 読書 |
| `hw` | 家事 |
| `rx` | リラックス |
| `es` | 省エネ |
| `ma` | メモリA |
| `mb` | メモリB |
| `rw` | Rawデータ送信 |

#### Rawデータ送信

任意の赤外線信号を送信する場合は`rw`を使用します。

```bash
curl -X POST http://<PicoのIPアドレス> \
  -H "Content-Type: application/json" \
  -d '{"class": "cl", "type": "rw", "data": [2980, 1520, 380, 1120, ...], "repeat": 1}'
```

## ファイル構成

```
.
├── main.py              # エントリポイント・HTTPハンドラ定義
├── irUtility.py         # 赤外線送信クラス（IRTransmitter / IRController）
├── webUtility.py        # WebサーバークラスとHTTPパーサー
├── page.html            # ブラウザUI
├── scripts.js           # フロントエンドJS
├── style.css            # スタイルシート
├── config.example.py    # 設定ファイルのテンプレート
└── config.py            # 設定ファイル（Git管理外）
```

## 注意事項

本システムは自宅LAN内での使用を想定しており、APIに認証機能は実装していません。外部公開する場合はAPIキー認証等の追加を推奨します。