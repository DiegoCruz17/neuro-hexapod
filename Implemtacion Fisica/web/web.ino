#include <WiFi.h>
#include <ESPAsyncWebServer.h>
#include <ArduinoJson.h>
#include <AsyncTCP.h>

// Configura tus credenciales WiFi
const char* ssid = "FLIA_ORTEGA_M";
const char* password = "60314014";

// Inicializa el servidor y WebSocket
AsyncWebServer server(80);
AsyncWebSocket ws("/ws");

void notFound(AsyncWebServerRequest *request) {
  request->send(404, "text/plain", "Not found");
}

// Función para manejar mensajes WebSocket
void handleWebSocketMessage(void *arg, uint8_t *data, size_t len) {
  AwsFrameInfo *info = (AwsFrameInfo *)arg;
  if (info->final && info->index == 0 && info->len == len && info->opcode == WS_TEXT) {
    char message[len + 1];             // ✅ Buffer seguro
    memcpy(message, data, len);
    message[len] = '\0';               // Termina cadena como string C

    StaticJsonDocument<1024> doc;      // Tamaño ampliado por seguridad
    DeserializationError error = deserializeJson(doc, message);

    if (!error && doc.containsKey("angles") && doc["angles"].is<JsonArray>()) {
      JsonArray angles = doc["angles"].as<JsonArray>();

      if (angles && angles.size() == 18) {
        String output = "MOVE:";
        for (int i = 0; i < 18; i++) {
          float angle = angles[i];
          output += String(angle, 2);
          if (i < 17) output += ",";
        }

        Serial.println(output); // ✅ Se imprime el mensaje final por Serial
      } else {
        Serial.println("ERROR: JSON válido, pero ángulos mal formateados o incompletos");
      }
    } else {
      Serial.print("ERROR: JSON inválido o faltan ángulos. Mensaje: ");
      Serial.println(message);
    }
  }
}

void onEvent(AsyncWebSocket *server, AsyncWebSocketClient *client, AwsEventType type,
             void *arg, uint8_t *data, size_t len) {
  if (type == WS_EVT_DATA) {
    handleWebSocketMessage(arg, data, len);
  }
}

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
  // Nada en loop; todo es asíncrono
}
