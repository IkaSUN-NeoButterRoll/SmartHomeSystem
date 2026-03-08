# from typing import List, Callable
import time
import socket
import network
from machine import Pin

def blink_led(led: Pin, times: int = 3, interval: float = 0.5):
    """LEDを指定回数点滅させる"""   
    for _ in range(times):
        led.value(1)  # LED ON
        time.sleep(interval)
        led.value(0)  # LED OFF
        time.sleep(interval)

class ConnectionEvent:
    """クライアントからの接続イベントを表すクラス"""
    @staticmethod
    def build_connection_event(conn: socket.socket) -> 'ConnectionEvent':
        return ConnectionEvent(conn)
    
    def __init__(self, conn: socket.socket) -> None:
        self.headers: dict[str, str]
        self.payload: str
        self._parse_request(conn)
        
    def _parse_request(self, conn: socket.socket) -> None:
        """リクエストを解析して、ヘッダーとペイロードを抽出する"""
        request = b""
        while b"\r\n\r\n" not in request:
            print("Receiving request data...")
            request += conn.recv(1024)
            print(f"Received {len(request)} bytes so far")
            if not request:
                print("No more data received, breaking the loop")
                break
        
        print(str(request))
        
        # リクエストがからの場合は処理終了
        if len(request) == 0:
            return
        
        headers_str, self.payload = request.decode('utf-8').split("\r\n\r\n", 1)
        
        header_lines = headers_str.split('\r\n')
        
        # ヘッダーをDictで保存
        self.headers = {}
        for line in header_lines:
            if ': ' in line:
                key, value = line.split(': ', 1)
                self.headers[key] = value
            elif line.startswith('GET ') or line.startswith('POST ') or line.startswith('OPTIONS '):
                parts = line.split(' ')
                if len(parts) >= 2:
                    self.headers['method'] = parts[0]
                    self.headers['path'] = parts[1]
                    self.headers['version'] = parts[2] if len(parts) > 2 else 'HTTP/1.1'
                else:
                    self.headers['method'] = ''
                    self.headers['path'] = ''
                    self.headers['version'] = ''
        
        print("Received headers:")
        print(self.headers)
        
        # Content-Lengthヘッダーがある場合は、ペイロードを完全に受信するまで待機する
        if 'Content-Length' in self.headers:
            while len(self.payload) < int(self.headers['Content-Length']):
                self.payload += conn.recv(1024).decode('utf-8')
            
        else:
            self.payload = str("")
            
        print("Received payload:")
        print(self.payload)

class WebManager:
    
    def __init__(self, ssid: str, password: str, files: list[str], led_pin: Pin):
        self.ip: str
        self.files: dict[str, str]
        self.led = led_pin
        
        self.on_get = lambda *args, **kwargs: None
        self.on_post = lambda *args, **kwargs: None
        self.on_options = lambda *args, **kwargs: None
        
        # 2回点滅: 初期化開始、3回点滅: 接続成功、4回点滅: 初期化完了
        blink_led(self.led, 2, 0.1)
        self.init_network(ssid, password)
        blink_led(self.led, 3, 0.2)
        self.load_files(files, self.ip)
        blink_led(self.led, 4, 0.4)
        

    
    def load_files(self, file_names: list[str],ip_address: str):
        """
        ファイルを読み込む
        IPアドレスのプレースホルダーがある場合は置換する
        """
        self.files = {}
        for file_name in file_names:
            with open(file_name, "r") as f:
                file = f.read()
                if ('%s' in file):
                    file = file % ip_address
                self.files[file_name] = file
            
        print("files loaded")
    
    def init_network(self, ssid: str, password: str):
        """Wi-Fiに接続する"""
        wlan = network.WLAN(network.STA_IF)
        wlan.active(True)
        wlan.disconnect()
        wlan.connect(ssid, password)
        print('Connecting to network...')
        
        time.sleep(1)
        
        # Wi-Fi接続の完了を待機する
        max_wait = 10
        while max_wait > 0:
            # 2回点滅: 接続中、 高速点滅: 接続失敗 再起動または設定の見直しが必要
            blink_led(self.led, 2, 0.4)
            if wlan.status() == network.STAT_GOT_IP:
                break
            if wlan.status() in [network.STAT_WRONG_PASSWORD, network.STAT_NO_AP_FOUND, network.STAT_CONNECT_FAIL]:
                print(f'Connection failed with status {wlan.status()}')
                while True:
                    # リトライはせず、LEDを高速点滅させ続ける
                    blink_led(self.led, 100, 0.05)
            
            print('waiting for connection...')
            time.sleep(1)
            max_wait -= 1
        
        if wlan.status() != network.STAT_GOT_IP:
            # 10回点滅: 接続失敗(タイムアウト)
            blink_led(self.led, 10, 0.05)
            raise RuntimeError('network connection timeout')
        
        print('Connected')
        status = wlan.ifconfig()
        print('ip = ' + status[0])
        self.ip = str(status[0])
        
    def run_server(self):
        # ソケットを作成して、ポート80で待ち受ける
        addr = socket.getaddrinfo('0.0.0.0', 80)[0][-1]
        s = socket.socket()
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1) 
        s.bind(addr)
        s.listen(1)
        print('listening on', addr)
        
        # 4回点滅: サーバー起動完了
        blink_led(self.led, 4, 0.4)
        
        # 接続を待ち受け、クライアントに応答する
        while True:
            try:
                self.led.value(0)
                print("Waiting for client connection...")
                self.led.value(1)
                # 接続してくるのを待つ
                cl, addr = s.accept()
                # 接続のタイムアウトを設定する
                cl.settimeout(2)
                
                print('client connected from', addr)
                
                self.led.value(0)
                e = ConnectionEvent.build_connection_event(cl)
                self.led.value(1)
                
                response = 'HTTP/1.0 400 Bad Request\r\nContent-type: text/html\r\n\r\n' + '<h1>400 Bad Request</h1>'
                
                # HTTPメソッドに応じて処理を分岐する
                if e.headers.get('method') == 'GET':
                    response = self.on_get(e, self.files)
                
                if e.headers.get('method') == 'OPTIONS':
                    response = self.on_options(e, self.files)
                
                if e.headers.get('method') == 'POST':
                    response = self.on_post(e, self.files)
                    
                cl.send(response)
                cl.close()
                print('Response sent')
                
            except OSError as e:
                cl.close()
                print('catch error')
                print(f'detail: {str(e)}')
                print('connection closed')
                # 例外が発生した場合はLEDを高速点滅させて知らせる
                blink_led(self.led, 10, 0.05)