/**
 * ╔══════════════════════════════════════════════════════════════════════════╗
 * ║        Smart Home Monitor – ESP32 Arduino IDE Firmware                   ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  Board   : ESP32 Dev Module  (Tools → Board → ESP32 Arduino → ESP32      ║
 * ║            Dev Module)                                                   ║
 * ║  Libraries: FirebaseClient  ← Library Manager → search "FirebaseClient"  ║
 * ║             by Mobizt, install latest (v2.x+)                            ║
 * ║             DHT sensor library  ← Library Manager → search               ║
 * ║             "DHT sensor library" by Adafruit, install latest             ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  IMPORTANT: Aceasta este versiunea migrată la noua librărie FirebaseClient
 * ║  (nu mai "Firebase ESP Client" v4.x care este deprecated/EOL).          ║
 * ║  BEFORE FLASHING – fill in every  ← YOU NEED THIS  comment below         ║
 * ╚══════════════════════════════════════════════════════════════════════════╝
 */

// ════════════════════════════════════════════════════════════════════════════
//  Enable only what we use – reduces flash size
//  (Activează doar funcțiile Firebase folosite pentru a economisi memorie flash)
// ════════════════════════════════════════════════════════════════════════════
#define ENABLE_USER_AUTH   // Email/password authentication
#define ENABLE_DATABASE    // Realtime Database

#include <WiFi.h>
#include <WiFiClientSecure.h> // Pentru conexiuni criptate SSL/TLS
#include <FirebaseClient.h>   // Noua librărie Firebase
#include <EEPROM.h>           // Pentru salvarea datelor în memoria non-volatilă
#include <map>                // Structură de date pentru a mapa pinii la starea lor
#include <DHT.h>              // Adafruit DHT sensor library

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 1 – Aici bag wifi ul
// ════════════════════════════════════════════════════════════════════════════
#define WIFI_SSID      "Internet_Name"
#define WIFI_PASSWORD  "Your_Password"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 2 – Firebase Project Settings(pt web)
// ════════════════════════════════════════════════════════════════════════════
#define FIREBASE_API_KEY   "**********"
#define FIREBASE_DB_URL    "**********"
#define FIREBASE_EMAIL     "**********"
#define FIREBASE_PASSWORD  "**********"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 3 – Your Firebase User UID
// ════════════════════════════════════════════════════════════════════════════
#define USER_UID  "**********"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 4 – Sensor Pins
// ════════════════════════════════════════════════════════════════════════════
#define PIN_DHT22   27    // Digital – DHT22 AM2302 temperature + humidity
#define DHT_TYPE    DHT22
#define PIN_WATER   35    // Analog  – water level sensor (flood detection)
#define PIN_PIR     32    // Digital – PIR HC-SR501 motion sensor
#define PIN_LDR     33    // Analog  – LDR 5537 photoresistor (voltage divider)

// Pragul (din 4095) peste care senzorul de apă declanșează alerta de inundație
#define WATER_FLOOD_THRESHOLD  500

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 5 – GPIO Pins Available for LEDs
//  Aceștia sunt pinii ESP-ului care vor reacționa la butoanele din site
// ════════════════════════════════════════════════════════════════════════════
const int AVAILABLE_PINS[]     = {2, 4, 5, 18, 19, 21, 22, 23, 13, 12, 14};
const int AVAILABLE_PINS_COUNT = sizeof(AVAILABLE_PINS) / sizeof(AVAILABLE_PINS[0]);

// Instanța obiectului pentru senzorul de temperatură
DHT dht(PIN_DHT22, DHT_TYPE);

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM Layout  (Alocarea memoriei persistente)
//  Aici definești cum sunt salvați pe memoria internă biții de informație
//  astfel încât să nu se piardă la o pană de curent.
// ════════════════════════════════════════════════════════════════════════════
#define EEPROM_SIZE      512
#define UART_MAGIC       0xAB  // Marker (semnătură) pentru a ști dacă EEPROM-ul a mai fost scris cu mesaje
#define FLOOD_MAGIC      0xCD  // Marker pentru a ști dacă s-au mai scris alerte de inundație
#define MSG_TEXT_LEN     32
#define MSG_RECORD_LEN   (MSG_TEXT_LEN + 1 + 4)   // 37 bytes per mesaj
#define MSG_COUNT        10    // Salvăm doar ultimele 10 mesaje UART
#define FLOOD_RECORD_LEN 5
#define FLOOD_COUNT      10    // Salvăm ultimele 10 inundații
#define ADDR_UART_MAGIC  0
#define ADDR_UART_IDX    1
#define ADDR_UART_DATA   2
#define ADDR_FLOOD_MAGIC (ADDR_UART_DATA + MSG_RECORD_LEN * MSG_COUNT)
#define ADDR_FLOOD_IDX   (ADDR_FLOOD_MAGIC + 1)
#define ADDR_FLOOD_DATA  (ADDR_FLOOD_IDX  + 1)

// ════════════════════════════════════════════════════════════════════════════
//  FirebaseClient objects  (replaces FirebaseData / FirebaseAuth / FirebaseConfig)
// ════════════════════════════════════════════════════════════════════════════

// 1. Obiectul care gestionează logarea
UserAuth user_auth(FIREBASE_API_KEY, FIREBASE_EMAIL, FIREBASE_PASSWORD);

// 2. Managerul intern al aplicației (ține token-urile "în viață")
FirebaseApp app;

// 3. Clienți SSL pentru conexiune securizată (necesită doi pentru noile update-uri asincrone)
WiFiClientSecure ssl_client1, ssl_client2;

// Wrapper-ul asincron impus de librăria nouă
using AsyncClient = AsyncClientClass;
AsyncClient aClient1(ssl_client1), aClient2(ssl_client2);

// 4. Baza de date efectivă
RealtimeDatabase Database;

// ════════════════════════════════════════════════════════════════════════════
//  Runtime state (Variabile globale de stare)
// ════════════════════════════════════════════════════════════════════════════
unsigned long    lastSensorPushMs = 0; // Când s-au trimis senzorii ultima oară
uint32_t         lastPendingMsgTs = 0; // Timestamp-ul ultimului mesaj executat de pe chat
bool             lastFloodState   = false; // Reține dacă ultima oară ploua/era inundație
std::map<int,bool> ledPinState;        // Un "dicționar" ce ține minte starea actuală (ON/OFF) a fiecărui pin

// Callback asincron: prinde și printează erorile interne ale Firebase
void asyncCB(AsyncResult &aResult) {
  if (aResult.isError()) {
    Serial.printf("[Firebase async] error: %s (%d)\n",
                  aResult.error().message().c_str(),
                  aResult.error().code());
  }
}

// Funcție scurtătură: construiește adresa bazei de date (ex: "/users/UID_UL_TAU/sensors")
static String fbPath(const char* suffix) {
  return String("/users/") + USER_UID + suffix;
}

// Funcție scurtătură: ne spune dacă Firebase-ul e conectat complet
static bool appReady() {
  return app.ready();
}

// ════════════════════════════════════════════════════════════════════════════
//  SETUP - Rulată o singură dată la alimentare
// ════════════════════════════════════════════════════════════════════════════
void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println("\n\n=== Smart Home Monitor – Booting (FirebaseClient) ===");

  // Setează toți pinii din listă ca ieșiri și îi trece pe 0V (LOW / opriți)
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++) {
    pinMode(AVAILABLE_PINS[i], OUTPUT);
    digitalWrite(AVAILABLE_PINS[i], LOW);
    ledPinState[AVAILABLE_PINS[i]] = false;
  }

  // Setează senzorul de mișcare ca intrare (să "asculte")
  pinMode(PIN_PIR, INPUT);
  dht.begin(); // Pornește senzorul DHT22

  // Pornește și verifică memoria EEPROM
  EEPROM.begin(EEPROM_SIZE);
  eepromInit();          // Verifică formatarea memoriei (scrie 0-uri dacă e prima oară)
  eepromPrintMessages(); // Printează istoricul salvat înainte de ultima repornire

  // ── Conectare la WiFi ──────────────────────────────────────────────────
  Serial.printf("[WiFi] Connecting to %s", WIFI_SSID);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.printf("\n[WiFi] Connected. IP: %s\n", WiFi.localIP().toString().c_str());

  // ── Configurare SSL Insecure (Cea mai ușoară variantă pt ESP) ──────────
  // Nu validează certificatul serverului Google, ci are direct încredere în el.
  ssl_client1.setInsecure();
  ssl_client2.setInsecure();

  // ── Inițializare conexiune Firebase ────────────────────────────────────
  initializeApp(aClient2, app, getAuth(user_auth), asyncCB, "authTask");
  app.getApp<RealtimeDatabase>(Database); // Leagă DB-ul la managerul aplicației
  Database.url(FIREBASE_DB_URL);          // Setează link-ul bazei de date

  // Buclă de așteptare: Până nu primește tokenul valid, stă aici maxim 20 sec.
  Serial.print("[Firebase] Authenticating");
  unsigned long t0 = millis();
  while (!app.ready()) {
    app.loop();   // MUST be called while waiting (Gestionează task-urile asincrone)
    if (millis() - t0 > 20000) {
      Serial.println("\n[Firebase] Timeout!");
      break;
    }
    delay(200);
    Serial.print(".");
  }
  Serial.println(app.ready() ? "\n[Firebase] Ready." : "\n[Firebase] Not ready – continuing anyway.");
  Serial.printf("[Firebase] Writing to: /users/%s/...\n\n", USER_UID);
}

// ════════════════════════════════════════════════════════════════════════════
//  LOOP - "Inima" codului, se repetă continuu
// ════════════════════════════════════════════════════════════════════════════
void loop() {
  // Obligatoriu pentru a nu lăsa conexiunea Firebase să expire
  app.loop();

  // Verifică dacă au trecut 5 secunde, apoi citește/trimite noii senzori
  if (millis() - lastSensorPushMs >= 5000) {
    lastSensorPushMs = millis();
    pushSensorData();
  }

  // Descarcă de pe web comutatoarele (LED-urile) la fiecare 2 secunde
  syncLEDStates();       
  
  // Verifică mesaje noi de pe chat-ul web la fiecare 3 secunde
  checkPendingMessage(); 

  // Dacă utilizatorul a scris ceva în Serial Monitor (ex: "A4"), preia și execută
  while (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() > 0) handleUARTCommand(cmd);
  }

  delay(50);
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – Formatare (Dacă placa e rulată prima oară)
// ════════════════════════════════════════════════════════════════════════════
void eepromInit() {
  // Verifică "amprentele" pentru a știi dacă secțiunile UART și FLOOD au mai fost folosite
  bool uartOK  = (EEPROM.read(ADDR_UART_MAGIC)  == UART_MAGIC);
  bool floodOK = (EEPROM.read(ADDR_FLOOD_MAGIC) == FLOOD_MAGIC);

  // Dacă nu, scrie amprentele și umple cu zero-uri blocurile de memorie
  if (!uartOK) {
    Serial.println("[EEPROM] First boot (UART buffer) – initialising.");
    EEPROM.write(ADDR_UART_MAGIC, UART_MAGIC);
    EEPROM.write(ADDR_UART_IDX, 0);
    for (int i = 0; i < MSG_RECORD_LEN * MSG_COUNT; i++)
      EEPROM.write(ADDR_UART_DATA + i, 0);
  }
  if (!floodOK) {
    Serial.println("[EEPROM] First boot (Flood buffer) – initialising.");
    EEPROM.write(ADDR_FLOOD_MAGIC, FLOOD_MAGIC);
    EEPROM.write(ADDR_FLOOD_IDX, 0);
    for (int i = 0; i < FLOOD_RECORD_LEN * FLOOD_COUNT; i++)
      EEPROM.write(ADDR_FLOOD_DATA + i, 0);
  }
  EEPROM.commit(); // Salvarea fizică pe flash
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – Salvează un mesaj venit pe chat sau Serial în EEPROM
// ════════════════════════════════════════════════════════════════════════════
void eepromSaveUARTMsg(const char* text, uint32_t ts) {
  // Se află la care index (din cele 10 permise) s-a ajuns (buffer circular)
  uint8_t idx  = EEPROM.read(ADDR_UART_IDX) % MSG_COUNT;
  int     base = ADDR_UART_DATA + idx * MSG_RECORD_LEN;
  int     len  = strlen(text);
  
  if (len > MSG_TEXT_LEN) len = MSG_TEXT_LEN;
  // Scrie literă cu literă mesajul
  for (int i = 0; i <= MSG_TEXT_LEN; i++)
    EEPROM.write(base + i, (i < len) ? (uint8_t)text[i] : 0);
    
  // Scrie timestamp-ul (timpul la care a fost trimis, descompus în 4 bytes)
  EEPROM.write(base + MSG_TEXT_LEN + 1, (uint8_t)(ts >> 24));
  EEPROM.write(base + MSG_TEXT_LEN + 2, (uint8_t)(ts >> 16));
  EEPROM.write(base + MSG_TEXT_LEN + 3, (uint8_t)(ts >>  8));
  EEPROM.write(base + MSG_TEXT_LEN + 4, (uint8_t)(ts));
  
  // Incrementează indexul pentru a scrie în slotul următor data viitoare
  EEPROM.write(ADDR_UART_IDX, (idx + 1) % MSG_COUNT);
  EEPROM.commit();
  Serial.printf("[EEPROM] Saved UART msg in slot %d\n", idx);
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – Afișează toate mesajele salvate (apelată doar la boot)
// ════════════════════════════════════════════════════════════════════════════
void eepromPrintMessages() {
  Serial.println("─── Stored UART Messages (EEPROM) ──────────────");
  bool found = false;
  // Trece prin toate cele 10 sloturi alocate
  for (int i = 0; i < MSG_COUNT; i++) {
    int  base = ADDR_UART_DATA + i * MSG_RECORD_LEN;
    char text[MSG_TEXT_LEN + 1];
    // Reconstruiește textul literă cu literă
    for (int j = 0; j <= MSG_TEXT_LEN; j++)
      text[j] = (char)EEPROM.read(base + j);
    text[MSG_TEXT_LEN] = '\0';
    
    if (text[0] == '\0') continue; // Sari dacă slotul e gol
    
    // Reconstruiește numărul mare de timestamp din cei 4 bytes
    uint32_t ts = ((uint32_t)EEPROM.read(base + MSG_TEXT_LEN + 1) << 24)
                | ((uint32_t)EEPROM.read(base + MSG_TEXT_LEN + 2) << 16)
                | ((uint32_t)EEPROM.read(base + MSG_TEXT_LEN + 3) <<  8)
                |  (uint32_t)EEPROM.read(base + MSG_TEXT_LEN + 4);
    Serial.printf("  Slot %2d | ts=%-10u | %s\n", i, ts, text);
    found = true;
  }
  if (!found) Serial.println("  (no messages stored yet)");
  Serial.println("─────────────────────────────────────────────────\n");
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – Salvează momentul în care s-a detectat apă (funcționare ca la UART)
// ════════════════════════════════════════════════════════════════════════════
void eepromSaveFloodEvent(uint32_t ts, uint8_t sensorVal) {
  uint8_t idx  = EEPROM.read(ADDR_FLOOD_IDX) % FLOOD_COUNT;
  int     base = ADDR_FLOOD_DATA + idx * FLOOD_RECORD_LEN;
  EEPROM.write(base,     (uint8_t)(ts >> 24));
  EEPROM.write(base + 1, (uint8_t)(ts >> 16));
  EEPROM.write(base + 2, (uint8_t)(ts >>  8));
  EEPROM.write(base + 3, (uint8_t)(ts));
  EEPROM.write(base + 4, sensorVal); // Salvează starea brută a senzorului
  EEPROM.write(ADDR_FLOOD_IDX, (idx + 1) % FLOOD_COUNT);
  EEPROM.commit();
  Serial.printf("[EEPROM] Flood event saved in slot %d\n", idx);
}

// ════════════════════════════════════════════════════════════════════════════
//  LED Helper – Manipulează starea (ON/OFF) a unui anumit pin fizic
// ════════════════════════════════════════════════════════════════════════════
void setLEDPin(int pin, bool state) {
  bool allowed = false;
  // Verifică dacă pinul cerut de web/chat chiar e un pin setat în configurare
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++)
    if (AVAILABLE_PINS[i] == pin) { allowed = true; break; }
    
  if (!allowed) {
    Serial.printf("[LED] Rejected pin %d – not in AVAILABLE_PINS!\n", pin);
    return; // Oprește-te dacă e un pin interzis/senzor
  }
  
  // Oprește execuția dacă pinul e deja în starea dorită (evită spam-ul de semnal)
  if (ledPinState.count(pin) && ledPinState[pin] == state) return;
  
  // Dă comandă fizică procesorului (HIGH=3.3v, LOW=0v)
  digitalWrite(pin, state ? HIGH : LOW);
  ledPinState[pin] = state; // Memorează noua stare
  Serial.printf("[LED] GPIO %-2d → %s\n", pin, state ? "ON" : "OFF");
}

// ════════════════════════════════════════════════════════════════════════════
//  UART Command Handler - Analizează textele ca "A12" sau "S4"
// ════════════════════════════════════════════════════════════════════════════
void handleUARTCommand(const String& cmd) {
  if (cmd.length() < 2) return;
  char action = cmd.charAt(0); // Ia prima literă
  if (action != 'A' && action != 'S') {
    Serial.printf("[UART] Unknown command: %s\n", cmd.c_str());
    return;
  }
  
  // Extrage numărul pinului (tot ce e după prima literă)
  int  pin   = cmd.substring(1).toInt();
  bool state = (action == 'A'); // Dacă a fost 'A' e TRUE (Aprins)
  
  setLEDPin(pin, state); // Execută aprinderea

  uint32_t ts = (uint32_t)(millis() / 1000UL);
  eepromSaveUARTMsg(cmd.c_str(), ts); // Salvează comanda primită în EEPROM

  // Urmează actualizarea bazei de date (ca site-ul să arate switch-ul ON dacă tu ai scris comanda manual din consolă)
  if (!appReady()) return;

  // Descarcă tot JSON-ul /leds
  String ledsPath = fbPath("/leds");
  String ledsJson = Database.get<String>(aClient1, ledsPath);

  if (aClient1.lastError().code() != 0) {
    Serial.printf("[UART] Failed to read /leds: %s\n",
                  aClient1.lastError().message().c_str());
    return;
  }

  // Caută prin textul bazei de date expresia "pin":<pinul tău>
  String pinStr = "\"pin\":" + String(pin);
  int pinIdx    = ledsJson.indexOf(pinStr);
  if (pinIdx < 0) {
    Serial.printf("[UART] Pin %d not found in /leds – adding anyway.\n", pin);
    return;
  }

  // Determină "numele nodului" (ex: "led_1" sau "led_2") asociat cu acel pin, mergând înapoi prin JSON
  int keyStart = ledsJson.lastIndexOf('"', pinIdx - 3);
  int keyEnd   = ledsJson.indexOf('"', keyStart + 1);
  if (keyStart < 0 || keyEnd < 0) return;
  
  String ledId     = ledsJson.substring(keyStart + 1, keyEnd);
  String statePath = fbPath("/leds/") + ledId + "/state";

  // Suprascrie în baza de date valoarea "state" la true sau false
  bool ok = Database.set<bool>(aClient1, statePath, state);
  if (!ok) {
    Serial.printf("[UART] Failed to update LED state: %s\n",
                  aClient1.lastError().message().c_str());
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Citire și Trimitere Date Senzori
// ════════════════════════════════════════════════════════════════════════════
void pushSensorData() {
  if (!appReady()) {
    Serial.println("[Sensor] Firebase not ready – skipping.");
    return;
  }

  // Citește fizic pinii senzorilor
  float tempC    = dht.readTemperature();
  float humidity = dht.readHumidity();
  // Dacă senzorul DHT22 e deconectat, previne crasarea și pune valori false (-99)
  if (isnan(tempC))    tempC    = -99.0f;
  if (isnan(humidity)) humidity =   0.0f;

  int  waterRaw = analogRead(PIN_WATER); // Citește tensiunea analogică apă (0-4095)
  bool flood    = (waterRaw > WATER_FLOOD_THRESHOLD); // E inundație doar dacă e peste prag (500)
  bool motion   = (digitalRead(PIN_PIR) == HIGH); // Senzorul de mișcare returnează direct o logică simplă
  int  ldrRaw   = analogRead(PIN_LDR); // Citește fotorezistorul
  int  lightPct = map(ldrRaw, 0, 4095, 100, 0); // Transformă 0-4095 în procente 100%-0%

  uint32_t ts = (uint32_t)(millis() / 1000UL); // Timpul relativ

  Serial.printf("[Sensor] temp=%.1f°C  hum=%.1f%%  flood=%d (raw=%d)  motion=%d  light=%d%%\n",
                tempC, humidity, (int)flood, waterRaw, (int)motion, lightPct);

  // ── Construirea Pachetului JSON cu noul sistem (JsonWriter) ───────────
  object_t obj_temp, obj_hum, obj_flood, obj_motion, obj_light, obj_ts;
  object_t sensorObj;
  JsonWriter writer;

  // Se creează perechi cheie-valoare: ex. "temperature": 25.4
  writer.create(obj_temp,   "temperature", number_t(tempC, 1));
  writer.create(obj_hum,    "humidity",    number_t(humidity, 1));
  writer.create(obj_flood,  "flood",       boolean_t(flood));
  writer.create(obj_motion, "motion",      boolean_t(motion));
  writer.create(obj_light,  "light",       lightPct);
  writer.create(obj_ts,     "lastUpdated", (int)ts);
  
  // Se contopesc toate într-un singur obiect "sensorObj"
  writer.join(sensorObj, 6, obj_temp, obj_hum, obj_flood, obj_motion, obj_light, obj_ts);

  // Trimite / Suprascrie JSON-ul nou construit în nodul "/sensors"
  String sensorsPath = fbPath("/sensors");
  bool ok = Database.set<object_t>(aClient1, sensorsPath, sensorObj);
  if (!ok) {
    Serial.printf("[Sensor] Push failed: %s\n",
                  aClient1.lastError().message().c_str());
    return;
  }

  // ── Adăugare Istoric Temperatură (ca să ai un grafic pe web) ────────────
  object_t histVal, histTs, histObj;
  writer.create(histVal, "value",     number_t(tempC, 1));
  writer.create(histTs,  "timestamp", (int)ts);
  writer.join(histObj, 2, histVal, histTs);

  String histPath = fbPath("/temperatureHistory");
  Database.push<object_t>(aClient1, histPath, histObj); // ".push" adaugă listei, ".set" suprascrie!

  // ── Generare Alertă Inundație (se declanșează doar o singură dată pe inundație) ──
  if (flood && !lastFloodState) {
    Serial.println("[Flood] FLOOD DETECTED!");
    eepromSaveFloodEvent(ts, 1);

    object_t evtTs, evtRaw, evtAck, evtObj;
    writer.create(evtTs,  "timestamp",    (int)ts);
    writer.create(evtRaw, "sensorValue",  waterRaw);
    writer.create(evtAck, "acknowledged", boolean_t(false)); // Până nu apeși OK pe web, rămâne fals
    writer.join(evtObj, 3, evtTs, evtRaw, evtAck);

    String evtPath = fbPath("/floodEvents");
    Database.push<object_t>(aClient1, evtPath, evtObj);
  }
  lastFloodState = flood; // Reține că acuma e deja inundație ca să nu dea spam 
}

// ════════════════════════════════════════════════════════════════════════════
//  Citirea Comutatoarelor (LED-urilor) de pe Firebase
// ════════════════════════════════════════════════════════════════════════════
void syncLEDStates() {
  static unsigned long lastSync = 0;
  if (millis() - lastSync < 2000) return; // Restricționează la 1 citire/2 sec.
  lastSync = millis();
  if (!appReady()) return;

  // Descarcă JSON-ul întreg din /leds ca un String (text simplu)
  String ledsPath = fbPath("/leds");
  String ledsJson = Database.get<String>(aClient1, ledsPath);

  // Debug: printează JSON-ul brut primit de la Firebase în serial
  Serial.println("[LED Sync] JSON primit:");
  Serial.println(ledsJson);
  Serial.println("[LED Sync] ───────────");

  if (aClient1.lastError().code() != 0) {
    Serial.printf("[LED Sync] Eroare get: %d %s\n",
                  aClient1.lastError().code(),
                  aClient1.lastError().message().c_str());
    return;
  }
  if (ledsJson.length() == 0) return;

  // Parsare (Extragere date) Manuală String:
  int searchFrom = 0;
  while (true) {
    // 1. Găsește primul loc unde apare cuvântul "pin":
    int pinKeyIdx = ledsJson.indexOf("\"pin\":", searchFrom);
    if (pinKeyIdx < 0) break;

    // 2. Extrage cifrele pin-ului (ignoră eventualele spații din JSON)
    int pinValStart = pinKeyIdx + 6;
    while (pinValStart < (int)ledsJson.length() && ledsJson[pinValStart] == ' ')
      pinValStart++;
    int pinValEnd = pinValStart;
    while (pinValEnd < (int)ledsJson.length() &&
           (isdigit(ledsJson[pinValEnd]) || ledsJson[pinValEnd] == '-'))
      pinValEnd++;
    int pin = ledsJson.substring(pinValStart, pinValEnd).toInt();

    // 3. Delimitează grupul { ... } al acelui pin pentru a fi sigur că se asociază corect valoarea
    int blockStart = ledsJson.lastIndexOf('{', pinKeyIdx);
    int blockEnd   = ledsJson.indexOf('}', pinKeyIdx);
    if (blockStart < 0 || blockEnd < 0) { searchFrom = pinValEnd; continue; }

    String block = ledsJson.substring(blockStart, blockEnd + 1);
    Serial.printf("[LED Sync] Pin %d | block: %s\n", pin, block.c_str());

    // 4. Caută valoarea "state": din acel grup delimitat. "t" de la true sau "1" e de ajuns ca să aprindă ledul.
    bool state = false;
    int stIdx = block.indexOf("\"state\":");
    if (stIdx >= 0) {
      int stValStart = stIdx + 8;
      while (stValStart < (int)block.length() && block[stValStart] == ' ')
        stValStart++;
      char c = block[stValStart];
      state = (c == 't' || c == '1');  // true sau 1
    }

    Serial.printf("[LED Sync] → GPIO %d = %s\n", pin, state ? "ON" : "OFF");
    setLEDPin(pin, state); // Setare fizică pin (delega la Funcția Helper explicată mai sus)
    
    searchFrom = blockEnd + 1; // Setează indexul ca bucla să caute URMĂTORUL pin
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Preluarea mesajelor de la secțiunea Chat/Consolă Web
// ════════════════════════════════════════════════════════════════════════════
void checkPendingMessage() {
  static unsigned long lastCheck = 0;
  if (millis() - lastCheck < 3000) return;
  lastCheck = millis();
  if (!appReady()) return;

  // Extrage JSON string din locația /pendingMessage
  String pmPath = fbPath("/pendingMessage");
  String pmJson = Database.get<String>(aClient1, pmPath);

  if (aClient1.lastError().code() != 0) return;
  if (pmJson.length() == 0) return;

  // Extrage timestamp-ul din String-ul JSON primită folosind logica de substring
  int tsIdx = pmJson.indexOf("\"timestamp\":");
  if (tsIdx < 0) return;
  int tsValStart = tsIdx + 12;
  int tsValEnd   = pmJson.indexOf(',', tsValStart);
  if (tsValEnd < 0) tsValEnd = pmJson.indexOf('}', tsValStart);
  if (tsValEnd < 0) return;
  uint32_t msgTs = (uint32_t)pmJson.substring(tsValStart, tsValEnd).toInt();

  // Se asigură că mesajul nu a mai fost citit odată (verificând timestamp-ul)
  if (msgTs <= lastPendingMsgTs) return;  
  lastPendingMsgTs = msgTs;

  // Extrage textul comenzii propriu-zis
  int textIdx = pmJson.indexOf("\"text\":\"");
  if (textIdx < 0) return;
  int textStart = textIdx + 8;
  int textEnd   = pmJson.indexOf('"', textStart);
  if (textEnd < 0) return;
  String msgTxt = pmJson.substring(textStart, textEnd);

  // Execută acțiunea (folosește handler-ul explicat anterior)
  Serial.printf("[PendingMsg] Received: \"%s\" (ts=%u)\n", msgTxt.c_str(), msgTs);
  eepromSaveUARTMsg(msgTxt.c_str(), msgTs); // Salvează în memorie locală
  handleUARTCommand(msgTxt); // Schimbă starea piniilor

  // ── "Ecou" înapoi pe Firebase, astfel încât utilizatorul web să vadă mesajul ESP-ului in ecranul chat ──
  object_t msgText, msgTimestamp, msgSource, msgObj;
  JsonWriter writer;
  writer.create(msgText,      "text",      string_t(msgTxt.c_str()));
  writer.create(msgTimestamp, "timestamp", (int)msgTs);
  writer.create(msgSource,    "source",    string_t("esp32"));
  writer.join(msgObj, 3, msgText, msgTimestamp, msgSource);

  // Adaugă ecoul în istoricul vizibil de chat din "/messages"
  String msgsPath = fbPath("/messages");
  Database.push<object_t>(aClient1, msgsPath, msgObj);
}