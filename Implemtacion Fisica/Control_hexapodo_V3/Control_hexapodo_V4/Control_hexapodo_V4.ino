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
  {13, 14, 15},     // FR (Frontal Derecha) - Driver 1 
  {6, 5, 4},     // MR (Media Derecha)   - Driver 1
  {2, 1, 0},  // RR (Trasera Derecha) - Driver 1 
  {2, 3, 1},  // FL (Frontal Izq)     - Driver 2 
  {6, 5, 4},     // ML (Media Izq)       - Driver 2
  {14, 13, 15}    // RL (Trasera Izq)     - Driver 2
};

// Drivers para cada pata
int pataDrivers[6] = {1, 1, 1, 2, 2, 2};

// ==== OFFSETS para ajustar el cero de cada servo ====
double offsetsPatas[6][3] = {
  {45, 0, -15},    // FR: Coxa, Femur, Tibia
  {-10, 0, 0},   // MR: Coxa, Femur, Tibia
  {-43, 0, 0},   // RR: Coxa, Femur, Tibia
  {-45, 0, 0},   // FL: Coxa, Femur, Tibia
  {-5, 0, 0},    // ML: Coxa, Femur, Tibia
  {45, 0, 0}     // RL: Coxa, Femur, Tibia
};

// Offsets para servos extra
double offsetsExtra[6] = {0, 0, 0, 0, 0, 0};

// ==== Servos cola y tenazas ====
Servo cola1;       // pin 12
Servo cola2;       // pin 13
Servo tenazaIzq1;  // pin 27
Servo tenazaIzq2;  // pin 14
Servo tenazaDer1;  // pin 25
Servo tenazaDer2;  // pin 26

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

// Validar ángulo para servo extra
int validarAnguloExtra(int angulo, String contexto = "") {
  if (contexto != "") {
    return (int)constrainConLog(angulo, rangosExtras.min, rangosExtras.max, contexto);
  } else {
    return (int)constrainDouble(angulo, rangosExtras.min, rangosExtras.max);
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
  if (articulacion == 2) {
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

// ==== FUNCIÓN MEJORADA: Control simultáneo de todas las patas ====
void moverTodasLasPatas(double angulos[6][3]) {
  Serial.println("Moviendo todas las patas simultáneamente...");
  
  for (int pata = 0; pata < 6; pata++) {
    int* canales = pataCanales[pata];
    int driverID = pataDrivers[pata];
    Adafruit_PWMServoDriver* driver = (driverID == 1) ? &pca9685_1 : &pca9685_2;
    
    for (int articulacion = 0; articulacion < 3; articulacion++) {
      // 1. Aplicar constraint inicial al ángulo de entrada
      String contextoConstraint = "P" + String(pata) + ":q" + String(articulacion+1) + " (entrada)";
      double anguloValidado = validarAnguloArticulacion(angulos[pata][articulacion], articulacion, contextoConstraint);
      
      // 2. Aplicar transformaciones
      double anguloInvertido = aplicarInversionOrientacion(anguloValidado, articulacion);
      double anguloEscalado = aplicarEscaladoTibia(anguloInvertido, articulacion);
      double anguloConOffset = anguloEscalado + offsetsPatas[pata][articulacion];
      
      // 3. Aplicar constraint final para límites del servo (-90 a +90)
      String contextoFinal = "P" + String(pata) + ":q" + String(articulacion+1) + " (final)";
      double anguloFinal = constrainConLog(anguloConOffset, -90, 90, contextoFinal);
      
      // 4. Mover servo
      driver->setPWM(canales[articulacion], 0, angleToPulse(anguloFinal));
    }
  }
  Serial.println("Movimiento completo de patas finalizado");
}

// ==== FUNCIÓN MEJORADA: Control simultáneo completo (patas + extras) ====
void moverPosicionCompleta(PosicionCompleta pos) {
  Serial.println("Moviendo a posición completa...");
  
  // Mover todas las patas
  moverTodasLasPatas(pos.patas);
  
  // Mover servos extra con constraint
  for (int i = 0; i < 6; i++) {
    String contexto = "Extra" + String(i);
    int anguloValidado = validarAnguloExtra(pos.extras[i], contexto);
    moverServoExtra(i, anguloValidado);
  }
  
  Serial.println("Posición completa establecida");
}

void moverServoExtra(int codigo, int angulo) {
  // Aplicar constraint antes del offset
  int anguloValidado = validarAnguloExtra(angulo, "ServoExtra" + String(codigo));
  int anguloConOffset = anguloValidado + offsetsExtra[codigo];
  
  // Constraint final para asegurar rango 0-180
  anguloConOffset = validarAnguloExtra(anguloConOffset, "ServoExtra" + String(codigo) + " (con offset)");
  
  switch(codigo) {
    case 0: cola1.write(anguloConOffset); break;
    case 1: cola2.write(anguloConOffset); break;
    case 2: tenazaIzq1.write(anguloConOffset); break;
    case 3: tenazaIzq2.write(anguloConOffset); break;
    case 4: tenazaDer1.write(anguloConOffset); break;
    case 5: tenazaDer2.write(anguloConOffset); break;
  }
}

// ==== FUNCIÓN MEJORADA: Parser para arreglo de ángulos ====
bool parsearAngulos(String comando, double angulos[6][3]) {
  // Formato esperado: MOVE:ang1,ang2,ang3,ang4,...,ang18
  // Donde los ángulos van en orden: P0(q1,q2,q3), P1(q1,q2,q3), ..., P5(q1,q2,q3)
  
  if (!comando.startsWith("MOVE:")) {
    return false;
  }
  
  String datos = comando.substring(5); // Remover "MOVE:"
  datos.trim();
  
  int indice = 0;
  int inicio = 0;
  
  for (int pata = 0; pata < 6; pata++) {
    for (int articulacion = 0; articulacion < 3; articulacion++) {
      int coma = datos.indexOf(',', inicio);
      String valorStr;
      
      if (coma == -1 && indice == 17) { // Último valor
        valorStr = datos.substring(inicio);
      } else if (coma != -1) {
        valorStr = datos.substring(inicio, coma);
        inicio = coma + 1;
      } else {
        Serial.println("ERROR: Formato incorrecto - faltan valores");
        return false;
      }
      
      double valor = valorStr.toDouble();
      
      // APLICAR CONSTRAINT en lugar de rechazar valores fuera de rango
      String contexto = "Parse P" + String(pata) + ":q" + String(articulacion+1);
      valor = validarAnguloArticulacion(valor, articulacion, contexto);
      
      angulos[pata][articulacion] = valor;
      indice++;
    }
  }
  
  if (indice != 18) {
    Serial.print("ERROR: Se esperaban 18 valores, se recibieron ");
    Serial.println(indice);
    return false;
  }
  
  return true;
}

// ==== FUNCIÓN MEJORADA: Parser para posición completa ====
bool parsearPosicionCompleta(String comando, PosicionCompleta* pos) {
  // Formato: FULL:ang1,ang2,...,ang18,ext1,ext2,ext3,ext4,ext5,ext6
  // Total: 24 valores (18 patas + 6 extras)
  
  if (!comando.startsWith("FULL:")) {
    return false;
  }
  
  String datos = comando.substring(5);
  datos.trim();
  
  int indice = 0;
  int inicio = 0;
  
  // Parsear ángulos de patas (18 valores)
  for (int pata = 0; pata < 6; pata++) {
    for (int articulacion = 0; articulacion < 3; articulacion++) {
      int coma = datos.indexOf(',', inicio);
      String valorStr;
      
      if (coma != -1) {
        valorStr = datos.substring(inicio, coma);
        inicio = coma + 1;
      } else if (indice == 23) { // Último valor
        valorStr = datos.substring(inicio);
      } else {
        Serial.println("ERROR: Formato incorrecto en FULL");
        return false;
      }
      
      double valor = valorStr.toDouble();
      
      // APLICAR CONSTRAINT en lugar de rechazar
      String contexto = "FULL P" + String(pata) + ":q" + String(articulacion+1);
      valor = validarAnguloArticulacion(valor, articulacion, contexto);
      
      pos->patas[pata][articulacion] = valor;
      indice++;
    }
  }
  
  // Parsear servos extra (6 valores)
  for (int extra = 0; extra < 6; extra++) {
    int coma = datos.indexOf(',', inicio);
    String valorStr;
    
    if (coma != -1) {
      valorStr = datos.substring(inicio, coma);
      inicio = coma + 1;
    } else if (indice == 23) { // Último valor
      valorStr = datos.substring(inicio);
    } else {
      Serial.println("ERROR: Formato incorrecto en extras");
      return false;
    }
    
    int valor = valorStr.toInt();
    
    // APLICAR CONSTRAINT en lugar de rechazar
    String contexto = "FULL Extra" + String(extra);
    valor = validarAnguloExtra(valor, contexto);
    
    pos->extras[extra] = valor;
    indice++;
  }
  
  return (indice == 24);
}

void mostrarAyuda() {
  Serial.println("\n========== COMANDOS ==========");
  Serial.println("COMANDOS INDIVIDUALES:");
  Serial.println("P<pata>:<articulacion>:<angulo> - Mover servo individual");
  Serial.println("S<codigo>:<angulo> - Mover servo extra");
  Serial.println();
  Serial.println("COMANDOS SIMULTÁNEOS:");
  Serial.println("MOVE:<18 ángulos separados por comas>");
  Serial.println("  Ejemplo: MOVE:0,10,-20,5,15,-10,0,0,0,0,0,0,0,0,0,0,0,0");
  Serial.println("  Orden: P0(q1,q2,q3), P1(q1,q2,q3), ..., P5(q1,q2,q3)");
  Serial.println("  * Los ángulos fuera de rango se ajustarán automáticamente");
  Serial.println();
  Serial.println("FULL:<24 valores> - Mover todo (18 patas + 6 extras)");
  Serial.println("  Ejemplo: FULL:0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,90,90,90,90,90,90");
  Serial.println("  * Valores fuera de rango se ajustarán automáticamente");
  Serial.println();
  Serial.println("COMANDOS ESPECIALES:");
  Serial.println("HELP - Mostrar ayuda");
  Serial.println("NEUTRAL - Posición neutral");
  Serial.println("INFO - Mostrar configuración");
  Serial.println("DEMO - Ejecutar secuencia de prueba");
  Serial.println("RANGES - Mostrar rangos permitidos");
  Serial.println("==============================");
}

void mostrarRangos() {
  Serial.println("\n========== RANGOS PERMITIDOS ==========");
  Serial.println("ARTICULACIONES DE PATAS:");
  Serial.println("  Coxa (q1): -90° a +90°");
  Serial.println("  Femur (q2): -90° a +90°");
  Serial.println("  Tibia (q3): -90° a +90° (escalado interno a -60° a +60°)");
  Serial.println();
  Serial.println("SERVOS EXTRA (Cola y Tenazas):");
  Serial.println("  Rango: 0° a 180°");
  Serial.println();
  Serial.println("NOTA: Los valores fuera de rango se ajustarán");
  Serial.println("automáticamente al límite más cercano.");
  Serial.println("=====================================");
}

void secuenciaDemostracion() {
  Serial.println("\n=== INICIANDO DEMOSTRACIÓN ===");
  
  // Posición 1: Todas las patas levantadas
  double pos1[6][3] = {
    {0, 45, -45}, {0, 45, -45}, {0, 45, -45},
    {0, 45, -45}, {0, 45, -45}, {0, 45, -45}
  };
  Serial.println("Posición 1: Patas levantadas");
  moverTodasLasPatas(pos1);
  delay(2000);
  
  // Posición 2: Patas extendidas
  double pos2[6][3] = {
    {30, -30, 60}, {-30, -30, 60}, {30, -30, 60},
    {-30, -30, 60}, {30, -30, 60}, {-30, -30, 60}
  };
  Serial.println("Posición 2: Patas extendidas");
  moverTodasLasPatas(pos2);
  delay(2000);
  
  // Posición 3: Neutral
  double pos3[6][3] = {
    {0, 0, 0}, {0, 0, 0}, {0, 0, 0},
    {0, 0, 0}, {0, 0, 0}, {0, 0, 0}
  };
  Serial.println("Posición 3: Neutral");
  moverTodasLasPatas(pos3);
  
  Serial.println("=== DEMOSTRACIÓN FINALIZADA ===");
}

void posicionNeutral() {
  Serial.println("Estableciendo posición neutral...");
  double neutral[6][3] = {
    {0, 0, 0}, {0, 0, 0}, {0, 0, 0},
    {0, 0, 0}, {0, 0, 0}, {0, 0, 0}
  };
  moverTodasLasPatas(neutral);
}

void mostrarInfo() {
  Serial.println("\n========== CONFIGURACIÓN ==========");
  Serial.println("PATAS (Driver - Canales Coxa:Femur:Tibia):");
  for (int i = 0; i < 6; i++) {
    Serial.print("Pata ");
    Serial.print(i);
    Serial.print(": Driver PCA9685_");
    Serial.print(pataDrivers[i]);
    Serial.print(" - Canales ");
    Serial.print(pataCanales[i][0]);
    Serial.print(":");
    Serial.print(pataCanales[i][1]);
    Serial.print(":");
    Serial.println(pataCanales[i][2]);
  }
  Serial.println();
  Serial.println("CARACTERÍSTICAS:");
  Serial.println("- Control simultáneo de todas las patas");
  Serial.println("- Control completo (patas + extras)");
  Serial.println("- Secuencias predefinidas");
  Serial.println("- CONSTRAINT automático para valores fuera de rango");
  Serial.println("- Logging de ajustes aplicados");
  Serial.println("==================================");
}

void procesarComando(String comando) {
  comando.trim();
  comando.toUpperCase();
  
  if (comando == "HELP" || comando == "H") {
    mostrarAyuda();
    return;
  }
  
  if (comando == "NEUTRAL" || comando == "N") {
    posicionNeutral();
    return;
  }
  
  if (comando == "INFO" || comando == "I") {
    mostrarInfo();
    return;
  }
  
  if (comando == "DEMO" || comando == "D") {
    secuenciaDemostracion();
    return;
  }
  
  if (comando == "RANGES" || comando == "R") {
    mostrarRangos();
    return;
  }
  
  // Comando para mover todas las patas simultáneamente
  if (comando.startsWith("MOVE:")) {
    double angulos[6][3];
    if (parsearAngulos(comando, angulos)) {
      moverTodasLasPatas(angulos);
      Serial.println("OK: Movimiento simultáneo completado");
    }
    return;
  }
  
  // Comando para posición completa
  if (comando.startsWith("FULL:")) {
    PosicionCompleta pos;
    if (parsearPosicionCompleta(comando, &pos)) {
      moverPosicionCompleta(pos);
      Serial.println("OK: Posición completa establecida");
    }
    return;
  }
  
  // Comandos individuales existentes (mantener compatibilidad)
  if (comando.charAt(0) == 'P') {
    int primerDosPuntos = comando.indexOf(':', 1);
    int segundoDosPuntos = comando.indexOf(':', primerDosPuntos + 1);
    
    if (primerDosPuntos == -1 || segundoDosPuntos == -1) {
      Serial.println("ERROR: Formato incorrecto. Use P<pata>:<articulacion>:<angulo>");
      return;
    }
    
    int pata = comando.substring(1, primerDosPuntos).toInt();
    int articulacion = comando.substring(primerDosPuntos + 1, segundoDosPuntos).toInt();
    double angulo = comando.substring(segundoDosPuntos + 1).toDouble();
    
    // Validar parámetros básicos
    if (pata < 0 || pata > 5 || articulacion < 0 || articulacion > 2) {
      Serial.println("ERROR: Pata o articulación fuera de rango");
      return;
    }
    
    // APLICAR CONSTRAINT en lugar de rechazar
    String contexto = "Individual P" + String(pata) + ":q" + String(articulacion+1);
    angulo = validarAnguloArticulacion(angulo, articulacion, contexto);
    
    int* canales = pataCanales[pata];
    int driverID = pataDrivers[pata];
    Adafruit_PWMServoDriver* driver = (driverID == 1) ? &pca9685_1 : &pca9685_2;
    
    double anguloInvertido = aplicarInversionOrientacion(angulo, articulacion);
    double anguloEscalado = aplicarEscaladoTibia(anguloInvertido, articulacion);
    double anguloConOffset = anguloEscalado + offsetsPatas[pata][articulacion];
    
    // Constraint final
    String contextoFinal = "Individual P" + String(pata) + ":q" + String(articulacion+1) + " (final)";
    double anguloFinal = constrainConLog(anguloConOffset, -90, 90, contextoFinal);
    
    driver->setPWM(canales[articulacion], 0, angleToPulse(anguloFinal));
    Serial.println("OK: Servo individual movido");
    return;
  }
  
  if (comando.charAt(0) == 'S') {
    int dosPuntos = comando.indexOf(':', 1);
    
    if (dosPuntos == -1) {
      Serial.println("ERROR: Formato incorrecto. Use S<codigo>:<angulo>");
      return;
    }
    
    int codigo = comando.substring(1, dosPuntos).toInt();
    int angulo = comando.substring(dosPuntos + 1).toInt();
    
    // Validar código
    if (codigo < 0 || codigo > 5) {
      Serial.println("ERROR: Código de servo fuera de rango (0-5)");
      return;
    }
    
    // APLICAR CONSTRAINT en lugar de rechazar
    String contexto = "Individual S" + String(codigo);
    angulo = validarAnguloExtra(angulo, contexto);
    
    moverServoExtra(codigo, angulo);
    Serial.println("OK: Servo extra movido");
    return;
  }
  
  Serial.println("ERROR: Comando no reconocido. Use HELP para ver comandos");
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
  
  // Configurar servos extra
  cola1.attach(12);
  cola2.attach(13);
  tenazaIzq1.attach(27);
  tenazaIzq2.attach(14);
  tenazaDer1.attach(25);
  tenazaDer2.attach(26);
  
  Serial.println("=== CONTROL SIMULTÁNEO DE SERVOS CON CONSTRAINT ===");
  Serial.println("Sistema listo - Valores fuera de rango se ajustarán automáticamente");
  Serial.println("Escriba HELP para ver todos los comandos");
  Serial.println("Escriba RANGES para ver los rangos permitidos");
  Serial.println("Escriba DEMO para ver una demostración");
}

// ==== Loop principal ====
void loop() {
  leerSeriales();
}