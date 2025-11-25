import json
import win32pipe, win32file, pywintypes
import asyncio
import websockets
import threading
from queue import Queue
import time

PIPE_NAME = r'\\.\pipe\UnityToPython'

class PipeToWebSocketBridge:
    def __init__(self, esp32_ip="192.168.58.119", esp32_port=80):
        self.esp32_ip = esp32_ip
        self.esp32_port = esp32_port
        self.esp32_url = f"ws://{esp32_ip}:{esp32_port}/ws"
        
        # Control de frecuencia de envío
        self.send_rate_hz = 20  # 🔧 Frecuencia de envío (Hz) - ajustable
        self.send_interval = 1.0 / self.send_rate_hz  # Intervalo entre envíos
        self.last_send_time = 0
        
        # Datos más recientes (no cola)
        self.latest_angles = None
        self.angles_lock = threading.Lock()
        
        # Estado de conexiones
        self.pipe_connected = False
        self.websocket_connected = False
        self.websocket = None
        
        # Estadísticas
        self.angles_received = 0
        self.angles_sent = 0
        self.angles_dropped = 0  # Ángulos que llegaron pero no se enviaron

    def create_pipe(self):
        """Crear el named pipe"""
        return win32pipe.CreateNamedPipe(
            PIPE_NAME,
            win32pipe.PIPE_ACCESS_INBOUND,
            win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
            1, 65536, 65536,
            0,
            None
        )

    def process_angles(self, angles):
        """Procesa los ángulos y guarda solo los más recientes"""
        if len(angles) != 18:
            print(f"⚠️  Advertencia: Se esperaban 18 ángulos, recibidos {len(angles)}")
            return

        self.angles_received += 1
        
        # Actualizar con los ángulos más recientes (thread-safe)
        with self.angles_lock:
            if self.latest_angles is not None:
                self.angles_dropped += 1  # Contador de datos no enviados
            
            self.latest_angles = {
                "type": "joint_angles",
                "angles": angles,
                "timestamp": int(time.time() * 1000)
            }
        
        # Debug cada 100 mensajes
        if self.angles_received % 100 == 0:
            efficiency = (self.angles_sent / self.angles_received * 100) if self.angles_received > 0 else 0
            print(f"📊 Recibidos: {self.angles_received}, Enviados: {self.angles_sent}, Eficiencia: {efficiency:.1f}%")

    def set_send_rate(self, hz):
        """Cambiar la frecuencia de envío en tiempo real"""
        self.send_rate_hz = max(1, min(hz, 100))  # Límite entre 1-100 Hz
        self.send_interval = 1.0 / self.send_rate_hz
        print(f"🔧 Frecuencia de envío cambiada a {self.send_rate_hz} Hz")

    async def websocket_handler(self):
        """Maneja la conexión WebSocket con el ESP32"""
        while True:
            try:
                print(f"🔄 Conectando a ESP32: {self.esp32_url}")
                async with websockets.connect(self.esp32_url) as websocket:
                    self.websocket = websocket
                    self.websocket_connected = True
                    print(f"✅ Conectado al ESP32 (enviando a {self.send_rate_hz} Hz)")
                    
                    while True:
                        try:
                            current_time = time.time()
                            
                            # Verificar si es tiempo de enviar
                            if current_time - self.last_send_time >= self.send_interval:
                                
                                # Obtener los datos más recientes
                                with self.angles_lock:
                                    if self.latest_angles is not None:
                                        message_to_send = self.latest_angles.copy()
                                        self.latest_angles = None  # Limpiar después de usar
                                    else:
                                        message_to_send = None
                                
                                # Enviar si hay datos
                                if message_to_send:
                                    angles = message_to_send["angles"]

                                    # Enviar 6 comandos, uno por pata
                                    for leg_id in range(6):
                                        q1 = int(angles[leg_id * 3 + 0])
                                        q2 = int(angles[leg_id * 3 + 1])
                                        q3 = int(angles[leg_id * 3 + 2])
                                        command = f"M,{leg_id},{q1},{q2},{q3}"
                                        await websocket.send(command)
                                        await asyncio.sleep(0.001)  # pequeña pausa opcional

                                    self.angles_sent += 1
                                    self.last_send_time = current_time
                                    
                                    # Debug ocasional
                                    if self.angles_sent % 50 == 0:
                                        print(f"📡 Enviado mensaje #{self.angles_sent} al ESP32")
                            
                            # Pausa corta para no saturar el CPU
                            await asyncio.sleep(0.005)  # 5ms
                            
                        except websockets.exceptions.ConnectionClosed:
                            print("❌ WebSocket cerrado por ESP32")
                            break
                        except Exception as e:
                            print(f"❌ Error enviando a ESP32: {e}")
                            break
                            
            except Exception as e:
                print(f"❌ Error conectando a ESP32: {e}")
                self.websocket_connected = False
                print("⏳ Reintentando en 5 segundos...")
                await asyncio.sleep(5)

    def pipe_handler(self):
        """Maneja el named pipe de Unity"""
        pipe = self.create_pipe()
        print("⏳ Esperando conexión desde Unity...")
        
        try:
            win32pipe.ConnectNamedPipe(pipe, None)
            self.pipe_connected = True
            print("✅ Unity conectado")
            
            while True:
                try:
                    result, data = win32file.ReadFile(pipe, 64*1024)
                    message = data.decode('utf-8').strip()
                    
                    if not message:
                        continue

                    if message.lower() == "exit":
                        print("🛑 Comando de salida recibido de Unity")
                        break

                    # Procesar mensaje JSON
                    try:
                        payload = json.loads(message)
                        angles = payload.get("angles", [])
                        
                        if angles:
                            self.process_angles(angles)
                        else:
                            print("⚠️  No se encontraron ángulos en el mensaje")
                            
                    except json.JSONDecodeError:
                        print(f"❌ Error decodificando JSON: {message}")

                except pywintypes.error as e:
                    if e.winerror == 109:  # Pipe desconectado
                        print("❌ Unity desconectado")
                        break
                    else:
                        print(f"❌ Error de pipe: {e}")
                        break

        except Exception as e:
            print(f"❌ Error en pipe: {e}")
        finally:
            self.pipe_connected = False
            win32file.CloseHandle(pipe)
            print("🔌 Pipe cerrado")

    def start(self):
        """Iniciar el bridge"""
        print("🚀 Iniciando bridge Unity -> ESP32")
        print(f"🎯 Target ESP32: {self.esp32_url}")
        print(f"📡 Frecuencia de envío: {self.send_rate_hz} Hz")
        
        # Iniciar hilo para el pipe
        pipe_thread = threading.Thread(target=self.pipe_handler, daemon=True)
        pipe_thread.start()
        
        # Iniciar bucle asyncio para WebSocket
        asyncio.run(self.websocket_handler())

    def get_status(self):
        """Obtener estado de las conexiones"""
        return {
            "pipe_connected": self.pipe_connected,
            "websocket_connected": self.websocket_connected,
            "angles_received": self.angles_received,
            "angles_sent": self.angles_sent,
            "angles_dropped": self.angles_dropped,
            "send_rate_hz": self.send_rate_hz,
            "efficiency": (self.angles_sent / self.angles_received * 100) if self.angles_received > 0 else 0
        }

# Versión simplificada con control de frecuencia
def simple_version_throttled():
    """Versión simplificada con control de frecuencia"""
    ESP32_IP = "192.168.58.119"
    ESP32_PORT = 80
    esp32_url = f"ws://{ESP32_IP}:{ESP32_PORT}/ws"
    
    # Control de frecuencia
    SEND_RATE_HZ = 15  # 🔧 Ajusta esta frecuencia
    send_interval = 1.0 / SEND_RATE_HZ
    last_send_time = 0
    
    # Datos más recientes
    latest_angles = None
    angles_lock = threading.Lock()
    
    def pipe_worker():
        """Hilo para manejar el pipe de Unity"""
        nonlocal latest_angles
        
        pipe = win32pipe.CreateNamedPipe(
            PIPE_NAME,
            win32pipe.PIPE_ACCESS_INBOUND,
            win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
            1, 65536, 65536, 0, None
        )
        
        print("Esperando conexión desde Unity...")
        win32pipe.ConnectNamedPipe(pipe, None)
        print("Unity conectado.")
        
        try:
            while True:
                result, data = win32file.ReadFile(pipe, 64*1024)
                message = data.decode('utf-8').strip()
                
                if not message or message.lower() == "exit":
                    break
                
                try:
                    payload = json.loads(message)
                    angles = payload.get("angles", [])
                    
                    if angles:
                        # Actualizar solo los datos más recientes
                        with angles_lock:
                            latest_angles = {
                                "angles": angles,
                                "timestamp": int(time.time() * 1000)
                            }
                        
                        print(f"Actualizados {len(angles)} ángulos")
                        
                except json.JSONDecodeError:
                    print(f"Error JSON: {message}")
                    
        except Exception as e:
            print(f"Error pipe: {e}")
        finally:
            win32file.CloseHandle(pipe)
    
    async def websocket_worker():
        """Corrutina para manejar WebSocket con control de frecuencia"""
        nonlocal latest_angles, last_send_time
        
        while True:
            try:
                print(f"Conectando a ESP32: {esp32_url} (enviando a {SEND_RATE_HZ} Hz)")
                async with websockets.connect(esp32_url) as websocket:
                    print("ESP32 conectado!")
                    
                    while True:
                        current_time = time.time()
                        
                        # Verificar si es tiempo de enviar
                        if current_time - last_send_time >= send_interval:
                            
                            # Obtener datos más recientes
                            with angles_lock:
                                if latest_angles is not None:
                                    message_to_send = latest_angles.copy()
                                    latest_angles = None  # Limpiar
                                else:
                                    message_to_send = None
                            
                            # Enviar si hay datos
                            if message_to_send:
                                angles = message_to_send["angles"]

                                # Enviar 6 comandos, uno por pata
                                for leg_id in range(6):
                                    q1 = int(angles[leg_id * 3 + 0])
                                    q2 = int(angles[leg_id * 3 + 1])
                                    q3 = int(angles[leg_id * 3 + 2])
                                    command = f"M,{leg_id},{q1},{q2},{q3}"
                                    await websocket.send(command)
                                    await asyncio.sleep(0.001)  # opcional: pausa mínima para evitar atasco

                                last_send_time = current_time
                                print(f"📡 Enviado al ESP32: {len(message_to_send['angles'])} ángulos")
                        
                        await asyncio.sleep(0.01)  # 10ms de pausa
                        
            except Exception as e:
                print(f"Error WebSocket: {e}")
                await asyncio.sleep(3)
    
    # Iniciar hilo del pipe
    pipe_thread = threading.Thread(target=pipe_worker, daemon=True)
    pipe_thread.start()
    
    # Ejecutar WebSocket
    asyncio.run(websocket_worker())

if __name__ == "__main__":
    print("Selecciona la versión:")
    print("1. Versión con clase y control de frecuencia")
    print("2. Versión simple con control de frecuencia")
    
    choice = input("Opción (1/2): ").strip()
    
    if choice == "1":
        # Crear bridge con frecuencia controlada
        bridge = PipeToWebSocketBridge(esp32_ip="192.168.58.119", esp32_port=80)
        
        # 🔧 Opcional: cambiar frecuencia
        print("\nFrecuencias recomendadas:")
        print("- 5 Hz: Muy suave, para pruebas")
        print("- 15 Hz: Equilibrado, recomendado")
        print("- 30 Hz: Rápido, solo si el ESP32 puede manejarlo")
        
        try:
            hz = int(input(f"Frecuencia deseada (1-100 Hz, default {bridge.send_rate_hz}): ") or bridge.send_rate_hz)
            bridge.set_send_rate(hz)
        except:
            pass
            
        bridge.start()
    else:
        simple_version_throttled()