import time
import network
import socket
import json
from irUtility import IRController
from machine import Pin
from webUtility import ConnectionEvent, WebManager, blink_led
from config import SSID, PASSWORD

def on_get(e: ConnectionEvent, files: dict[str, str]) -> str:
    """GETリクエストを処理する関数"""
    print("GET request")
    response = str('HTTP/1.0 200 OK\r\nContent-type: %s\r\nCache-Control: public, max-age=31536000, immutable\r\n\r\n')
    
    if (e.headers.get('path') == '/'):
        print("HTML request")
        response = response % 'text/html' + files['page.html']
    
    elif (e.headers.get('path') == '/style.css'):
        print("CSS request")
        response = response % 'text/css' + files['style.css']
    
    elif (e.headers.get('path') == '/scripts.js'):
        print("JS request")
        response = response % 'application/javascript' + files['scripts.js']
    
    elif (e.headers.get('path') == '/favicon.png'):
        print("Favicon request")
        response = response % 'image/png'
        response += 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAC4jAAAuIwF4pT92AAAAB3RJTUUH6QcODhUGeygl4gAAABl0RVh0Q29tbWVudABDcmVhdGVkIHdpdGggR0lNUFeBDhcAAAQbSURBVDgRARAE7/sAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAar4wAAAAAACWQtAAAAAAAQAAAAAAAAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAP9qvjAAar4wAAAAAADN1j4AzdY+AGq+MAAAAAD/AAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAABJFnUAIai7AAAAAADfWEUAt+qLAAAAAAAAAAABAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAD/SRZ1AEH5wQC/Bz8AAAAAAEH5wQC/Bz8At+qLAAAAAAEAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAA/4oPNgBUG/QAAAAAAAAAAAAAAAAAAAAAAAAAAACs5QwAdvHKAAAAAAEAAAAAAAAAAAIAAAAAAAAAAAAAAAAAAAAAVBv0AAAAAAAAAAAAITUYAAAAAAAAAAAAITUYAFQb9AAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAITUYAAAAAACLsPQAAAAAAAAAAACLsPQAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAACLsPQAAAAAAFQb9AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAA/94qKgCs5QwAAAAAAAAAAAAAAAAAVBv0ACLW1gAAAAABAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/94qKgCs5QwAAAAAAFQb9AAi1tYAAAAAAQAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/94qKgAAAAAAItbWAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHjTR9R5jCj/AAAAAElFTkSuQmCC'
    
    else:
        print("Unknown GET request")
        response = 'HTTP/1.0 404 Not Found\r\nContent-type: text/html\r\n\r\n' + '<h1>404 Not Found</h1>'
    return response

def on_options(e: ConnectionEvent, files: dict[str, str]) -> str:
    """OPTIONSリクエストを処理する関数"""
    print("OPTIONS request")
                
    return 'HTTP/1.0 200 OK\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: POST, GET, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type\r\nContent-type: text/html\r\n\r\n'

def on_post (e: ConnectionEvent, files: dict[str, str]) -> str:
    """POSTリクエストを処理する関数"""
    print("POST request")
    
    try:
        # Json取得
        pl = json.loads(e.payload)
    except Exception as ex:
        print("Error: " + str(ex))
        return 'HTTP/1.0 400 Bad Request\r\nContent-type: text/html\r\n\r\n' + '<h1>400 Bad Request</h1>'
    
    # クラスがclのとき、typeに応じて赤外線コマンドを送信
    if 'class' in pl and pl['class'] == 'cl':
        commands = get_commands(ir) # type: ignore
        if pl['type'] in commands:
            commands[pl['type']]() # type: ignore
        elif pl['type'] == 'rw':
            raw_data = pl['data']
            repeat_count = pl['repeat']
            print("Sending raw data: " + str(raw_data))
            if isinstance(raw_data, list):
                # Rawデータ送信
                ir.ir_transmitter.send_raw(raw_data, repeat=repeat_count if repeat_count < 1 else 1)
            
        return 'HTTP/1.0 200 OK\r\nContent-type: text/html\r\n\r\n' + '{ \"status\" : \"OK\" }'
    
    return 'HTTP/1.0 200 OK\r\nContent-type: text/html\r\n\r\n' + '{ \"status\" : \"NG\" }'

def get_commands(ir: IRController):
    """赤外線コマンドのマッピングを返す関数"""
    return {
        "pw": ir.command_power,
        "nl": ir.command_night_light,
        "bt": ir.command_bright,
        "dk": ir.command_dark,
        "wt": ir.command_white,
        "wc": ir.command_warm,
        "fp": ir.command_full_power,
        "rb": ir.command_reading,
        "hw": ir.command_housework,
        "ma": ir.command_memory_a,
        "rx": ir.command_relax,
        "es": ir.command_energy_saving,
        "mb": ir.command_memory_b
    }

if __name__ == '__main__':
    #自宅Wi-FiのSSIDとパスワード
    ssid = SSID
    password = PASSWORD
    files = ['page.html', 'style.css', 'scripts.js']
    led = Pin("LED", Pin.OUT) # 内蔵LEDを使用
    led.value(0)
    ir = IRController(15)  # GP15に赤外線LED接続
    print("IR Controller initialized")
    
    while True:
        # 少し待つ
        time.sleep(2)
        
        # 2回点滅: 起動開始
        blink_led(led, 2, 0.1)
        
        print("Starting IR Controller Web Server")
        
        wm = WebManager(ssid, password, files, led)
        
        # コールバック関数を設定
        wm.on_get = on_get # type: ignore
        wm.on_options = on_options # type: ignore
        wm.on_post = on_post # type: ignore
        
        try:
            # Webサーバーを起動
            wm.run_server()
        except KeyboardInterrupt:
            raise KeyboardInterrupt
        except Exception as ex:
            # 例外が発生したら再起動
            print('Error: ' + str(ex))
            print('connection closed')
            print('reboot')
