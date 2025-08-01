import asyncio
import websockets
import json

connected_clients = set()

async def handle_connection(websocket, path):
    # Registrar cliente
    connected_clients.add(websocket)
    client_ip = websocket.remote_address[0]
    print(f"✅ Cliente conectado: {client_ip}")

    try:
        async for message in websocket:
            try:
                data = json.loads(message)
                print(f"📨 Datos recibidos de {client_ip}: {data}")

                # Aquí puedes procesar los datos, por ejemplo:
                if "angles" in data:
                    print("🎯 Ángulos recibidos:", data["angles"])

                # Enviar respuesta opcional:
                # await websocket.send("ACK")
            except json.JSONDecodeError:
                print(f"⚠️ Mensaje no válido: {message}")
    except websockets.exceptions.ConnectionClosedOK:
        print(f"🔌 Cliente {client_ip} se desconectó correctamente.")
    except websockets.exceptions.ConnectionClosedError as e:
        print(f"❌ Error de conexión con {client_ip}: {e}")
    finally:
        connected_clients.remove(websocket)
        print(f"🚪 Cliente eliminado: {client_ip}")

async def start_server():
    print("🚀 Servidor WebSocket iniciado en ws://localhost:8765")
    async with websockets.serve(handle_connection, "0.0.0.0", 8765):
        await asyncio.Future()  # espera infinita

if __name__ == "__main__":
    try:
        asyncio.run(start_server())
    except KeyboardInterrupt:
        print("\n🛑 Servidor detenido manualmente.")
