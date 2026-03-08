from machine import Pin, PWM
import time
from config import REPEAT_COUNT

class IRTransmitter:
    """赤外線信号を送信するクラス"""
    def __init__(self, pin_no: int, carrier_freq: int = 38000):
        self.ir_led = PWM(Pin(pin_no))
        self.carrier_freq = carrier_freq
        
        self.led = Pin(1, Pin.OUT)

    def _enable_pwm(self):
        self.ir_led.freq(self.carrier_freq)
        self.ir_led.duty_u16(32768)  # 50% duty比

    def _disable_pwm(self):
        self.ir_led.duty_u16(0)

    def send_raw(self, data: list[int], repeat: int = REPEAT_COUNT, delay: int = 7420):
        self.led.value(1)
        
        for _ in range(repeat):
            for i, duration in enumerate(data):
                if i % 2 == 0:
                    # 出力ON期間: PWMを有効化
                    self._enable_pwm()
                else:
                    # 出力OFF期間: PWMを無効化
                    self._disable_pwm()
                time.sleep_us(duration)
            self._disable_pwm()
            
        self.led.value(0)
        time.sleep_us(delay)

class IRController:
    """
    赤外線リモコンのコマンドを定義するクラス
    対象: Iris OhyamaのLEDライト
    """
    def __init__(self, pin_no: int):
        self.ir_transmitter = IRTransmitter(pin_no)
    
    def command_power(self):
        # Power command sequence
        self.ir_transmitter.send_raw([
            2980,1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,370,380,370,380,370,380,1120,380,370,380,1120,380,370,380,370,
            380
        ])
        
    def command_night_light(self):
        # Night light command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,370,380,1120,380,1120,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
    
    def command_bright(self):
        # Bright command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,370,380,370,380,1120,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
    
    def command_dark(self):
        # Dark command sequence
        self.ir_transmitter.send_raw([
            2980,1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,370,380,370,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
    
    def command_white(self):
        # White command sequence
        self.ir_transmitter.send_raw([
            2980,1470,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
        
    def command_warm(self):
        # Warm command sequence
        self.ir_transmitter.send_raw([
            2980,1470,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,370,380,1120,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
        
    def command_full_power(self):
        # Full power command sequence
        self.ir_transmitter.send_raw([
            2980,1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,370,380,370,380,1120,380,370,380,370,380,1120,380,1120,380,370,
            380
            ])
    
    def command_reading(self):
        # Reading command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,370,380,370,380,370,380,1120,380,1120,380,370,
            380
            ])
        
    def command_housework(self):
        # Housework command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380, 1120, 380, 1120,
            380, 370, 380, 370,
            380, 1120,
            380, 370, 380, 370, 380, 370,
            380, 1120, 380, 1120, 380, 1120,
            380, 370,
            380, 1120,
            380, 370, 380, 370, 380, 370, 380, 370, 380, 370,
            380, 1120,
            380, 370, 380, 370,
            380, 1120, 380, 1120,
            380, 370,
            380
        ])
    
    def command_relax(self):
        # Relax command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,370,380,1120,380,370,380,370,380,1120,380,1120,380,370,
            380
        ])
        
    def command_energy_saving(self):
        # Energy saving command sequence
        self.ir_transmitter.send_raw([
            2980,1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,370,380,1120,380,1120,380,370,380,370,380,1120,380,1120,380,370,
            380
        ])
    
    def command_memory_a(self):
        # Memory A command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,1120,380,370,
            380
        ])
        
    def command_memory_b(self):
        # Memory B command sequence
        self.ir_transmitter.send_raw([
            2980, 1520,
            380,1120,380,1120,380,370,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,1120,380,1120,380,370,380,1120,380,370,380,370,380,370,
            380,1120,380,370,380,370,380,1120,380,1120,380,370,380,1120,380,370,
            380
        ])
