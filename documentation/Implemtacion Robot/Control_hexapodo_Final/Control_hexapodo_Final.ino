#include <Wire.h>
#include <Adafruit_PWMServoDriver.h>
#include <ESP32Servo.h>

// ==== Config PCA9685 ====
Adafruit_PWMServoDriver pca9685_1(0x40);
Adafruit_PWMServoDriver pca9685_2(0x41);

#define SERVOMIN  120
#define SERVOMAX  530
#define FREQUENCY 50

// Rangos de movimiento por articulación [min, max]
struct RangosArticulacion {
  double min;
  double max;
};

// Definir rangos específicos para cada articulación
RangosArticulacion rangosPatas[3] = {
  {-90, 90},  // Coxa (q1)
  {-180, 180},  // Femur (q2) 
  {-180, 180}   // Tibia (q3) - rango más limitado después del escalado
};

RangosArticulacion rangosExtras = {0, 180}; // Para servos de cola y tenazas

// Configuración de canales para cada pata [q1, q2, q3]
int pataCanales[6][3] = {
  {14, 13, 15},      // FL (Frontal Izq)     - Driver 2 
  {6, 5, 4},      // ML (Media Izq)       - Driver 2
  {2, 3, 1} ,   // RL (Trasera Izq)     - Driver 2
  {2, 1, 0},   // FR (Frontal Derecha) - Driver 1 
  {6, 5, 4},      // MR (Media Derecha)   - Driver 1
  {13, 14, 15}      // RR (Trasera Derecha) - Driver 1

};

// Drivers para cada pata
int pataDrivers[6] = {2, 2, 2, 1, 1, 1};

// ==== OFFSETS para ajustar el cero de cada servo ====
double offsetsPatas[6][3] = {
  {-45, 0, 0},    // FR: Coxa, Femur, Tibia
  {-10, 0, 0},   // MR: Coxa, Femur, Tibia
  {43, 0, 0},   // RR: Coxa, Femur, Tibia
  {45, 0, 0},   // FL: Coxa, Femur, Tibia
  {-10, 0, 0},    // ML: Coxa, Femur, Tibia
  {-35, 0, -10}     // RL: Coxa, Femur, Tibia
};

// Offsets para servos extra
double offsetsExtra[6] = {0, 0, 0, 0, 0, 0};


// Pines para Serial2
#define RX2_PIN 16
#define TX2_PIN 17

// ==== Variables para control simultáneo ====
struct PosicionCompleta {
  double patas[6][3];    // 6 patas, 3 articulaciones cada una
  int extras[6];         // 6 servos extra (cola y tenazas)
};

// ==== FUNCIONES DE CONSTRAINT MEJORADAS ====

// Función constraint personalizada para double
double constrainDouble(double valor, double minVal, double maxVal) {
  if (valor < minVal) return minVal;
  if (valor > maxVal) return maxVal;
  return valor;
}

// Función constraint con logging para debugs
double constrainConLog(double valor, double minVal, double maxVal, String contexto) {
  double valorOriginal = valor;
  valor = constrainDouble(valor, minVal, maxVal);
  
  if (valorOriginal != valor) {
    Serial.print("CONSTRAINT aplicado en ");
    Serial.print(contexto);
    Serial.print(": ");
    Serial.print(valorOriginal);
    Serial.print(" -> ");
    Serial.println(valor);
  }
  
  return valor;
}

// Validar y constrair ángulo para articulación específica
double validarAnguloArticulacion(double angulo, int articulacion, String contexto = "") {
  // Aplicar constraint según el tipo de articulación
  RangosArticulacion rango = rangosPatas[articulacion];
  
  if (contexto != "") {
    return constrainConLog(angulo, rango.min, rango.max, contexto);
  } else {
    return constrainDouble(angulo, rango.min, rango.max);
  }
}


// ==== Funciones auxiliares ====
double angleToPulse(double angle) {
  double map_factor = (SERVOMAX - SERVOMIN) / 180.0;
  double mapped = angle + 90;
  return SERVOMIN + mapped * map_factor;
}

double mapDouble(double x, double in_min, double in_max, double out_min, double out_max) {
  return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}

double aplicarInversionOrientacion(double angulo, int articulacion) {
  if (articulacion == 1 || articulacion == 0) {
    return -angulo;
  }
  return angulo;
}

double aplicarEscaladoTibia(double angulo, int articulacion) {
  if (articulacion == 2) {
    return mapDouble(angulo, -90, 90, -60, 60);
  }
  return angulo;
}



void procesarComando(String comando) {
  comando.trim();
  comando.toUpperCase();

  // Comando único esperado: M,<pata>,<q1>,<q2>,<q3>
  if (comando.startsWith("M,")) {
    comando.remove(0, 2);  // Eliminar "M,"

    // Separar los 4 valores esperados
    int valores[4];  // pata, q1, q2, q3
    for (int i = 0; i < 4; i++) {
      int coma = comando.indexOf(',');
      if (coma != -1) {
        valores[i] = comando.substring(0, coma).toInt();
        comando = comando.substring(coma + 1);
      } else if (i == 3) {
        valores[i] = comando.toInt();
      } else {
        Serial.println("ERROR: Formato incorrecto. Use M,<pata>,<q1>,<q2>,<q3>");
        return;
      }
    }

    int pata = valores[0];
    if (pata < 0 || pata >= 6) {
      Serial.println("ERROR: Número de pata fuera de rango (0-5)");
      return;
    }

    // Aplicar movimiento a la pata
    for (int i = 0; i < 3; i++) {
      double angulo = validarAnguloArticulacion((double)valores[i + 1], i);
      angulo = aplicarInversionOrientacion(angulo, i);
      angulo = aplicarEscaladoTibia(angulo, i);
      angulo += offsetsPatas[pata][i];
      angulo = constrainDouble(angulo, -90, 90);

      int canal = pataCanales[pata][i];
      Adafruit_PWMServoDriver* driver = (pataDrivers[pata] == 1) ? &pca9685_1 : &pca9685_2;
      driver->setPWM(canal, 0, angleToPulse(angulo));
    }

    Serial.println("OK");
  } else {
    Serial.println("ERROR: Comando no reconocido");
  }
}

void leerSeriales() {
  if (Serial.available()) {
    String comando = Serial.readStringUntil('\n');
    procesarComando(comando);
  }
  
  if (Serial2.available()) {
    String comando = Serial2.readStringUntil('\n');
    procesarComando(comando);
  }
}

// ==== Setup ====
void setup() {
  Serial.begin(115200);
  Serial2.begin(115200, SERIAL_8N1, RX2_PIN, TX2_PIN);
  
  Wire.begin();
  pca9685_1.begin();
  pca9685_2.begin();
  pca9685_1.setPWMFreq(FREQUENCY);
  pca9685_2.setPWMFreq(FREQUENCY);

}

// ==== Loop principal ====
void loop() {
  leerSeriales();
}