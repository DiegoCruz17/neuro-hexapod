#include <WiFi.h>
#include <ESPAsyncWebServer.h>
#include <AsyncTCP.h>

// Credenciales WiFi
const char* ssid = "CamiloS22";
const char* password = "camilin314";

// Servidor y WebSocket
AsyncWebServer server(80);
AsyncWebSocket ws("/ws");

// Función cuando no se encuentra una ruta
void notFound(AsyncWebServerRequest *request) {
  request->send(404, "text/plain", "Not found");
}

// Función para manejar mensajes WebSocket
void handleWebSocketMessage(void *arg, uint8_t *data, size_t len) {
  AwsFrameInfo *info = (AwsFrameInfo *)arg;

  if (info->final && info->index == 0 && info->len == len && info->opcode == WS_TEXT) {
    String message = String((char*)data).substring(0, len);  // Captura exacta del mensaje
    Serial.println(message);  // ✅ Reenvía exactamente el mismo comando recibido
  }
}

// Manejador de eventos del WebSocket
void onEvent(AsyncWebSocket *server, AsyncWebSocketClient *client,
             AwsEventType type, void *arg, uint8_t *data, size_t len) {
  if (type == WS_EVT_DATA) {
    handleWebSocketMessage(arg, data, len);
  }
}

// Configuración inicial
void setup() {
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  Serial.println("Conectando a WiFi...");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi conectado");
  Serial.println(WiFi.localIP());

  ws.onEvent(onEvent);
  server.addHandler(&ws);
  server.onNotFound(notFound);
  server.begin();
}

void loop() {
  // No se necesita nada aquí; todo es asíncrono
}
