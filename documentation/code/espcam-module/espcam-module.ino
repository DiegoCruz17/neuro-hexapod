#include "esp_camera.h"
#include <WiFi.h>
#include "ESP32_OV5640_AF.h"
#include "esp_http_server.h"
#include "img_converters.h"

#define CAMERA_MODEL_XIAO_ESP32S3 // Has PSRAM

// Camera pin definitions for XIAO ESP32S3 (embedded)
#define PWDN_GPIO_NUM     -1  // Power down pin (not used)
#define RESET_GPIO_NUM    -1  // Reset pin (not used)
#define XCLK_GPIO_NUM     10  // External clock
#define SIOD_GPIO_NUM     40  // I2C SDA for camera control
#define SIOC_GPIO_NUM     39  // I2C SCL for camera control

// Data pins
#define Y9_GPIO_NUM       48  // DVP_Y9
#define Y8_GPIO_NUM       11  // DVP_Y8
#define Y7_GPIO_NUM       12  // DVP_Y7
#define Y6_GPIO_NUM       14  // DVP_Y6
#define Y5_GPIO_NUM       16  // DVP_Y5
#define Y4_GPIO_NUM       18  // DVP_Y4
#define Y3_GPIO_NUM       17  // DVP_Y3
#define Y2_GPIO_NUM       15  // DVP_Y2

// Control pins
#define VSYNC_GPIO_NUM    38  // DVP_VSYNC
#define HREF_GPIO_NUM     47  // DVP_HREF
#define PCLK_GPIO_NUM     13  // DVP_PCLK

// ===== NETWORK CONFIGURATION =====
// Replace with your actual WiFi credentials
const char* ssid = "Diego’s iPhone";
const char* password = "123456789";

// Optional: Static IP configuration (comment out for DHCP)
// IPAddress local_IP(192, 168, 1, 200);
// IPAddress gateway(192, 168, 1, 1);
// IPAddress subnet(255, 255, 255, 0);

// ===== DEVICE CONFIGURATION =====
const char* device_name = "hexapod-camera";
const int status_led = LED_BUILTIN; // Built-in LED for status

// ===== GLOBAL VARIABLES =====
httpd_handle_t camera_httpd = NULL;
OV5640 ov5640 = OV5640();
bool camera_ready = false;
bool wifi_connected = false;
unsigned long last_heartbeat = 0;
unsigned long connection_start = 0;

void startCameraServer();
void setupLedFlash(int pin);
void blinkStatusLED(int times);

void setup() {
  Serial.begin(115200);
  while(!Serial);
  Serial.setDebugOutput(true);
  Serial.println("=== ESP32-S3 Camera Starting ===");
  
  // Initialize status LED
  pinMode(status_led, OUTPUT);
  digitalWrite(status_led, LOW);
  
  // ===== CAMERA INITIALIZATION =====
  Serial.println("Initializing camera...");
  camera_config_t config;
  config.ledc_channel = LEDC_CHANNEL_0;
  config.ledc_timer = LEDC_TIMER_0;
  config.pin_d0 = Y2_GPIO_NUM;
  config.pin_d1 = Y3_GPIO_NUM;
  config.pin_d2 = Y4_GPIO_NUM;
  config.pin_d3 = Y5_GPIO_NUM;
  config.pin_d4 = Y6_GPIO_NUM;
  config.pin_d5 = Y7_GPIO_NUM;
  config.pin_d6 = Y8_GPIO_NUM;
  config.pin_d7 = Y9_GPIO_NUM;
  config.pin_xclk = XCLK_GPIO_NUM;
  config.pin_pclk = PCLK_GPIO_NUM;
  config.pin_vsync = VSYNC_GPIO_NUM;
  config.pin_href = HREF_GPIO_NUM;
  config.pin_sscb_sda = SIOD_GPIO_NUM;
  config.pin_sscb_scl = SIOC_GPIO_NUM;
  config.pin_pwdn = PWDN_GPIO_NUM;
  config.pin_reset = RESET_GPIO_NUM;
  config.xclk_freq_hz = 20000000;
  config.pixel_format = PIXFORMAT_JPEG; // for streaming
  config.grab_mode = CAMERA_GRAB_WHEN_EMPTY;
  config.fb_location = CAMERA_FB_IN_PSRAM;
  config.jpeg_quality = 12;
  config.fb_count = 1;
  
  // Start with smaller frame size for better stability
  if(psramFound()){
    Serial.println("PSRAM found - using optimized settings");
    config.frame_size = FRAMESIZE_SVGA;  // Start smaller (800x600)
    config.jpeg_quality = 10;
    config.fb_count = 2;
    config.grab_mode = CAMERA_GRAB_LATEST;
  } else {
    Serial.println("PSRAM not found - using basic settings");
    config.frame_size = FRAMESIZE_VGA;   // Even smaller (640x480)
    config.fb_location = CAMERA_FB_IN_DRAM;
    config.jpeg_quality = 12;
    config.fb_count = 1;
  }
  
  // Initialize camera
  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK) {
    Serial.printf("Camera init failed with error 0x%x\n", err);
    blinkStatusLED(10); // Error indication
    return;
  }
  
  Serial.println("Camera initialized successfully");
  
  // Test camera capture immediately
  Serial.println("Testing camera capture...");
  camera_fb_t *fb = esp_camera_fb_get();
  if (fb) {
    Serial.printf("Camera test successful! Frame: %dx%d, size: %d bytes\n", 
                  fb->width, fb->height, fb->len);
    esp_camera_fb_return(fb);
    camera_ready = true;
  } else {
    Serial.println("Camera test failed - no frame buffer");
    camera_ready = false;
  }
  
  // Initialize autofocus only if basic camera works
  if (camera_ready) {
    Serial.println("Initializing autofocus...");
    sensor_t * s = esp_camera_sensor_get();
    ov5640.start(s);
    
    if (ov5640.focusInit() == 0) {
      Serial.println("OV5640 Focus Init Successful!");
      if (ov5640.autoFocusMode() == 0) {
        Serial.println("OV5640 Auto Focus Enabled!");
      } else {
        Serial.println("OV5640 Auto Focus Failed - continuing without autofocus");
      }
    } else {
      Serial.println("OV5640 Focus Init Failed - continuing without autofocus");
    }
  }

  // Setup LED Flash if available
#if defined(LED_GPIO_NUM)
  setupLedFlash(LED_GPIO_NUM);
#endif

  // ===== WIFI INITIALIZATION =====
  Serial.println("Connecting to WiFi...");
  connection_start = millis();
  
  // Set hostname for easier identification
  WiFi.setHostname(device_name);
  
  // Configure static IP if defined
  // if (!WiFi.config(local_IP, gateway, subnet)) {
  //   Serial.println("STA Failed to configure");
  // }
  
  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false); // Disable WiFi sleep for stable streaming
  WiFi.begin(ssid, password);
  
  // Wait for connection with timeout
  int wifi_retry_count = 0;
  while (WiFi.status() != WL_CONNECTED && wifi_retry_count < 60) {
    delay(500);
    Serial.print(".");
    wifi_retry_count++;
    
    // Blink LED while connecting
    digitalWrite(status_led, !digitalRead(status_led));
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    wifi_connected = true;
    digitalWrite(status_led, HIGH); // Solid LED when connected
    
    Serial.println("\n=== WiFi Connected Successfully ===");
    Serial.printf("SSID: %s\n", ssid);
    Serial.printf("IP Address: %s\n", WiFi.localIP().toString().c_str());
    Serial.printf("Gateway: %s\n", WiFi.gatewayIP().toString().c_str());
    Serial.printf("DNS: %s\n", WiFi.dnsIP().toString().c_str());
    Serial.printf("Hostname: %s\n", device_name);
    Serial.printf("MAC Address: %s\n", WiFi.macAddress().c_str());
    Serial.printf("Signal Strength: %d dBm\n", WiFi.RSSI());
    Serial.printf("Connection Time: %lu ms\n", millis() - connection_start);
    
    // Start camera server with all routes
    startCameraServer();
    
    Serial.println("\n=== Services Started ===");
    Serial.printf("Camera Stream: http://%s/stream\n", WiFi.localIP().toString().c_str());
    Serial.printf("Camera Capture: http://%s/capture\n", WiFi.localIP().toString().c_str());
    Serial.printf("Device Status: http://%s/status\n", WiFi.localIP().toString().c_str());
    Serial.printf("Device Info: http://%s/info\n", WiFi.localIP().toString().c_str());
    Serial.println("=== Ready for Connections ===");
    
  } else {
    Serial.println("\nWiFi connection failed!");
    Serial.println("Please check your credentials and try again.");
    blinkStatusLED(5); // Error indication
  }
}

void loop() {
  // Monitor autofocus status (less frequently)
  static unsigned long last_focus_check = 0;
  if (millis() - last_focus_check > 5000) { // Check every 5 seconds
    if (camera_ready) {
      uint8_t rc = ov5640.getFWStatus();
      
      if (rc == -1) {
        // Serial.println("Warning: OV5640 communication error");
      } else if (rc == FW_STATUS_S_FOCUSED) {
        // Serial.println("Camera: Focused"); // Comment out to reduce spam
      } else if (rc == FW_STATUS_S_FOCUSING) {
        Serial.println("Camera: Focusing...");
      }
    }
    last_focus_check = millis();
  }
  
  // Send periodic heartbeat to serial (for Raspberry Pi monitoring)
  if (millis() - last_heartbeat > 10000) { // Every 10 seconds
    Serial.printf("HEARTBEAT: IP=%s, RSSI=%d, Uptime=%lu, Free_Heap=%d\n", 
                  WiFi.localIP().toString().c_str(), 
                  WiFi.RSSI(), 
                  millis() / 1000,
                  ESP.getFreeHeap());
    last_heartbeat = millis();
  }
  
  // Monitor WiFi connection
  if (WiFi.status() != WL_CONNECTED && wifi_connected) {
    Serial.println("WiFi connection lost! Attempting to reconnect...");
    wifi_connected = false;
    digitalWrite(status_led, LOW);
    WiFi.reconnect();
  } else if (WiFi.status() == WL_CONNECTED && !wifi_connected) {
    Serial.println("WiFi reconnected!");
    wifi_connected = true;
    digitalWrite(status_led, HIGH);
  }
  
  delay(100);
}

void blinkStatusLED(int times) {
  for (int i = 0; i < times; i++) {
    digitalWrite(status_led, HIGH);
    delay(200);
    digitalWrite(status_led, LOW);
    delay(200);
  }
}

// Camera streaming functions
static const char* _STREAM_CONTENT_TYPE = "multipart/x-mixed-replace;boundary=123456789000000000000987654321";
static const char* _STREAM_BOUNDARY = "\r\n--123456789000000000000987654321\r\n";
static const char* _STREAM_PART = "Content-Type: image/jpeg\r\nContent-Length: %u\r\n\r\n";

static esp_err_t capture_handler(httpd_req_t *req) {
  camera_fb_t * fb = NULL;
  esp_err_t res = ESP_OK;
  int retry_count = 0;
  
  // Try to get frame buffer with retries
  while (retry_count < 3) {
    fb = esp_camera_fb_get();
    if (fb) {
      break;
    }
    Serial.printf("Camera capture attempt %d failed, retrying...\n", retry_count + 1);
    delay(100);
    retry_count++;
  }
  
  if (!fb) {
    Serial.println("Camera capture failed after retries");
    httpd_resp_send_500(req);
    return ESP_FAIL;
  }

  Serial.printf("Camera capture successful: %dx%d, %d bytes\n", 
                fb->width, fb->height, fb->len);

  httpd_resp_set_type(req, "image/jpeg");
  httpd_resp_set_hdr(req, "Content-Disposition", "inline; filename=capture.jpg");
  httpd_resp_set_hdr(req, "Access-Control-Allow-Origin", "*");

  // Send the frame buffer directly
  res = httpd_resp_send(req, (const char *)fb->buf, fb->len);
  
  esp_camera_fb_return(fb);
  return res;
}

static esp_err_t stream_handler(httpd_req_t *req) {
  camera_fb_t * fb = NULL;
  esp_err_t res = ESP_OK;
  size_t _jpg_buf_len = 0;
  uint8_t * _jpg_buf = NULL;
  char part_buf[64];

  res = httpd_resp_set_type(req, _STREAM_CONTENT_TYPE);
  if(res != ESP_OK){
    return res;
  }

  httpd_resp_set_hdr(req, "Access-Control-Allow-Origin", "*");
  httpd_resp_set_hdr(req, "Access-Control-Allow-Methods", "GET");
  httpd_resp_set_hdr(req, "Cache-Control", "no-cache");
  
  Serial.println("Starting video stream...");

  while(true){
    fb = esp_camera_fb_get();
    if (!fb) {
      Serial.println("Stream: Camera capture failed");
      res = ESP_FAIL;
      break;
    }
    
    _jpg_buf_len = fb->len;
    _jpg_buf = fb->buf;
    
    if(res == ESP_OK){
      size_t hlen = snprintf(part_buf, 64, _STREAM_PART, _jpg_buf_len);
      res = httpd_resp_send_chunk(req, part_buf, hlen);
    }
    if(res == ESP_OK){
      res = httpd_resp_send_chunk(req, (const char *)_jpg_buf, _jpg_buf_len);
    }
    if(res == ESP_OK){
      res = httpd_resp_send_chunk(req, _STREAM_BOUNDARY, strlen(_STREAM_BOUNDARY));
    }
    
    esp_camera_fb_return(fb);
    fb = NULL;
    _jpg_buf = NULL;
    
    if(res != ESP_OK){
      Serial.println("Stream connection closed by client");
      break;
    }
    
    // Minimal delay for stability - don't block too long
    delay(10);  // ~100 FPS max, but will be limited by camera capture speed
  }
  
  Serial.println("Video stream ended");
  return res;
}

static size_t jpg_encode_stream(void * arg, size_t index, const void* data, size_t len){
  httpd_req_t * req = (httpd_req_t *)arg;
  if(!req){
    return 0;
  }
  if(httpd_resp_send_chunk(req, (const char *)data, len) != ESP_OK){
    return 0;
  }
  return len;
}

// Status and info handlers
static esp_err_t status_handler(httpd_req_t *req) {
  httpd_resp_set_type(req, "application/json");
  httpd_resp_set_hdr(req, "Access-Control-Allow-Origin", "*");
  
  char json_response[1024];
  snprintf(json_response, sizeof(json_response),
    "{"
    "\"device_name\":\"%s\","
    "\"ip_address\":\"%s\","
    "\"mac_address\":\"%s\","
    "\"wifi_ssid\":\"%s\","
    "\"wifi_rssi\":%d,"
    "\"wifi_connected\":%s,"
    "\"camera_ready\":%s,"
    "\"uptime_seconds\":%lu,"
    "\"free_heap\":%d,"
    "\"psram_found\":%s"
    "}",
    device_name,
    WiFi.localIP().toString().c_str(),
    WiFi.macAddress().c_str(),
    WiFi.SSID().c_str(),
    WiFi.RSSI(),
    WiFi.status() == WL_CONNECTED ? "true" : "false",
    camera_ready ? "true" : "false",
    millis() / 1000,
    ESP.getFreeHeap(),
    psramFound() ? "true" : "false"
  );
  
  return httpd_resp_send(req, json_response, HTTPD_RESP_USE_STRLEN);
}

static esp_err_t info_handler(httpd_req_t *req) {
  httpd_resp_set_type(req, "text/html");
  httpd_resp_set_hdr(req, "Access-Control-Allow-Origin", "*");
  
  char html_response[2048];
  snprintf(html_response, sizeof(html_response),
    "<html><body>"
    "<h1>ESP32-S3 Camera Device</h1>"
    "<p><strong>Device:</strong> %s</p>"
    "<p><strong>IP:</strong> %s</p>"
    "<p><strong>MAC:</strong> %s</p>"
    "<p><strong>WiFi:</strong> %s (%d dBm)</p>"
    "<p><strong>Uptime:</strong> %lu seconds</p>"
    "<p><strong>Camera:</strong> %s</p>"
    "<br>"
    "<p><a href='/stream'>Live Stream</a></p>"
    "<p><a href='/capture'>Capture Image</a></p>"
    "<p><a href='/status'>Status JSON</a></p>"
    "</body></html>",
    device_name,
    WiFi.localIP().toString().c_str(),
    WiFi.macAddress().c_str(),
    WiFi.SSID().c_str(),
    WiFi.RSSI(),
    millis() / 1000,
    camera_ready ? "Ready" : "Error"
  );
  
  return httpd_resp_send(req, html_response, HTTPD_RESP_USE_STRLEN);
}

static esp_err_t root_handler(httpd_req_t *req) {
  // Redirect to info page
  httpd_resp_set_status(req, "302 Found");
  httpd_resp_set_hdr(req, "Location", "/info");
  return httpd_resp_send(req, NULL, 0);
}

void startCameraServer() {
  httpd_config_t config = HTTPD_DEFAULT_CONFIG();
  config.server_port = 80;
  config.max_uri_handlers = 8;  // Increase to handle more routes

  // Camera endpoints
  httpd_uri_t capture_uri = {
    .uri       = "/capture",
    .method    = HTTP_GET,
    .handler   = capture_handler,
    .user_ctx  = NULL
  };

  httpd_uri_t stream_uri = {
    .uri       = "/stream",
    .method    = HTTP_GET,
    .handler   = stream_handler,
    .user_ctx  = NULL
  };

  // Status endpoints
  httpd_uri_t status_uri = {
    .uri       = "/status",
    .method    = HTTP_GET,
    .handler   = status_handler,
    .user_ctx  = NULL
  };

  httpd_uri_t info_uri = {
    .uri       = "/info",
    .method    = HTTP_GET,
    .handler   = info_handler,
    .user_ctx  = NULL
  };

  httpd_uri_t root_uri = {
    .uri       = "/",
    .method    = HTTP_GET,
    .handler   = root_handler,
    .user_ctx  = NULL
  };

  if (httpd_start(&camera_httpd, &config) == ESP_OK) {
    // Register all handlers
    httpd_register_uri_handler(camera_httpd, &capture_uri);
    httpd_register_uri_handler(camera_httpd, &stream_uri);
    httpd_register_uri_handler(camera_httpd, &status_uri);
    httpd_register_uri_handler(camera_httpd, &info_uri);
    httpd_register_uri_handler(camera_httpd, &root_uri);
    
    Serial.println("Camera server started with all routes");
  } else {
    Serial.println("Error starting camera server!");
  }
}

// LED flash setup (if available)
#if defined(LED_GPIO_NUM)
void setupLedFlash(int pin) {
  pinMode(pin, OUTPUT);
  digitalWrite(pin, LOW);
}
#endif
