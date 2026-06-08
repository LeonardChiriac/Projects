/**
 * ╔══════════════════════════════════════════════════════════════════════════╗
 * ║          Smart Home Monitor – ESP32 Arduino IDE Firmware                ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  Board   : ESP32 Dev Module  (Tools → Board → ESP32 Arduino → ESP32     ║
 * ║            Dev Module)                                                   ║
 * ║  Libraries: Firebase ESP Client  ← Library Manager → search             ║
 * ║             "Firebase ESP Client" by Mobizt, install v4.x               ║
 * ║             DHT sensor library   ← Library Manager → search             ║
 * ║             "DHT sensor library" by Adafruit, install latest             ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  BEFORE FLASHING – fill in every  ← YOU NEED THIS  comment below       ║
 * ╚══════════════════════════════════════════════════════════════════════════╝
 *
 * Sensors   : DHT22 AM2302 module (temperature + humidity),
 *             Water level sensor analog (flood), PIR HC-SR501 (motion)
 * Actuators : LEDs on GPIO pins you configure in the website's LED Control page
 *
 * UART Commands (Serial Monitor, 115200 baud, newline-terminated):
 *   A<pin>   turn ON  LED on that GPIO   e.g. "A2"  → GPIO 2 HIGH
 *   S<pin>   turn OFF LED on that GPIO   e.g. "S18" → GPIO 18 LOW
 */

#include <WiFi.h>
#include <EEPROM.h>
#include <map>
#include <time.h>
#include <Firebase_ESP_Client.h>
#include "addons/TokenHelper.h"   // Part of the Firebase library – prints token events
#include <DHT.h>                  // Adafruit DHT sensor library

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 1 – WiFi Credentials
//  Put your home WiFi name and password here.
// ════════════════════════════════════════════════════════════════════════════
#define WIFI_SSID      "YOUR_WIFI_NAME"          // ← YOU NEED THIS
#define WIFI_PASSWORD  "YOUR_WIFI_PASSWORD"       // ← YOU NEED THIS

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 2 – Firebase Project Settings
//
//  Where to find each value:
//  ┌─────────────────────┬───────────────────────────────────────────────────┐
//  │ FIREBASE_API_KEY    │ Firebase Console → Project Settings → General     │
//  │                     │ → "Web API Key"                                   │
//  ├─────────────────────┼───────────────────────────────────────────────────┤
//  │ FIREBASE_DB_URL     │ Firebase Console → Realtime Database → Data tab   │
//  │                     │ → the URL at the top (ends in firebasedatabase.app)│
//  ├─────────────────────┼───────────────────────────────────────────────────┤
//  │ FIREBASE_EMAIL      │ A dedicated email/password user you create in      │
//  │ FIREBASE_PASSWORD   │ Firebase Console → Authentication → Users          │
//  │                     │ → Add user  (e.g. esp32@yourproject.com / anything)│
//  └─────────────────────┴───────────────────────────────────────────────────┘
// ════════════════════════════════════════════════════════════════════════════
#define FIREBASE_API_KEY   "YOUR_FIREBASE_WEB_API_KEY"                        // ← YOU NEED THIS
#define FIREBASE_DB_URL    "https://YOUR_PROJECT_ID-default-rtdb.europe-west1.firebasedatabase.app"  // ← YOU NEED THIS
#define FIREBASE_EMAIL     "esp32@yourproject.com"                            // ← YOU NEED THIS
#define FIREBASE_PASSWORD  "esp32_password"                                   // ← YOU NEED THIS

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 3 – Your Firebase User UID
//
//  Because data is stored under  /users/{uid}/...  the ESP32 must know which
//  user it belongs to.
//
//  How to find your UID:
//    Firebase Console → Authentication → Users tab
//    → copy the "User UID" column value for your Google account
//    It looks like:  abc123XYZ789...  (28 random characters)
// ════════════════════════════════════════════════════════════════════════════
#define USER_UID  "PASTE_YOUR_FIREBASE_USER_UID_HERE"  // ← YOU NEED THIS

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 4 – Sensor Pins
//  Change these if your wiring is different.
// ════════════════════════════════════════════════════════════════════════════
#define PIN_DHT22   27   // Digital – DHT22 AM2302 temperature + humidity
#define DHT_TYPE    DHT22
#define PIN_WATER   35   // Analog  – water level sensor (flood detection)
#define PIN_PIR     32   // Digital – PIR HC-SR501 motion sensor
#define PIN_LDR     33   // Analog  – LDR 5537 photoresistor (voltage divider)
//
//  LDR wiring (voltage divider – REQUIRED for a bare photoresistor):
//
//    3.3V ──── LDR 5537 ──── GPIO 33 ──── 10 kΩ resistor ──── GND
//                                  ↑
//                            analog read here
//
//  In bright light  : LDR ≈ 18–50 kΩ  → voltage at GPIO 33 is LOWER
//  In darkness      : LDR ≈ 2 MΩ      → voltage at GPIO 33 is HIGHER
//  ADC output (0-4095): higher value = darker room

// Water level ADC threshold (0–4095, 12-bit).  Readings above this value
// mean the sensor probes are touching water → flood condition.
#define WATER_FLOOD_THRESHOLD  500

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 5 – GPIO Pins Available for LEDs
//  These are the pins the website's LED Control page can assign LEDs to.
//  Must match the AVAILABLE_PINS list in the React frontend.
//  Do NOT include sensor pins here.
// ════════════════════════════════════════════════════════════════════════════
const int AVAILABLE_PINS[]     = {2, 4, 5, 18, 19, 21, 22, 23};
const int AVAILABLE_PINS_COUNT = sizeof(AVAILABLE_PINS) / sizeof(AVAILABLE_PINS[0]);

// DHT22 sensor instance (single-wire digital protocol)
DHT dht(PIN_DHT22, DHT_TYPE);

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM Layout  (you do not need to change anything below this line)
//
//  Stores the last 10 UART commands and last 10 flood events in flash so
//  they survive a power cycle.
//
//  Offset 0        : UART magic byte  (0xAB)
//  Offset 1        : UART circular-buffer write index
//  Offset 2..371   : 10 × 37-byte UART message records
//  Offset 372      : Flood magic byte (0xCD)
//  Offset 373      : Flood circular-buffer write index
//  Offset 374..423 : 10 × 5-byte flood event records
// ════════════════════════════════════════════════════════════════════════════
#define EEPROM_SIZE      512
#define UART_MAGIC       0xAB
#define FLOOD_MAGIC      0xCD
#define MSG_TEXT_LEN     32
#define MSG_RECORD_LEN   (MSG_TEXT_LEN + 1 + 4)   // 37 bytes
#define MSG_COUNT        10
#define FLOOD_RECORD_LEN 5
#define FLOOD_COUNT      10
#define ADDR_UART_MAGIC  0
#define ADDR_UART_IDX    1
#define ADDR_UART_DATA   2
#define ADDR_FLOOD_MAGIC (ADDR_UART_DATA + MSG_RECORD_LEN * MSG_COUNT)
#define ADDR_FLOOD_IDX   (ADDR_FLOOD_MAGIC + 1)
#define ADDR_FLOOD_DATA  (ADDR_FLOOD_IDX  + 1)

// ════════════════════════════════════════════════════════════════════════════
//  Firebase objects
// ════════════════════════════════════════════════════════════════════════════
FirebaseData   fbdo;
FirebaseData   fbdoHist;   // separate stream for temperatureHistory pushes
FirebaseAuth   fbAuth;
FirebaseConfig fbConfig;

// ════════════════════════════════════════════════════════════════════════════
//  Runtime state
// ════════════════════════════════════════════════════════════════════════════
unsigned long    lastSensorPushMs = 0;
uint32_t         lastPendingMsgTs = 0;
bool             lastFloodState   = false;
std::map<int,bool> ledPinState;

// ════════════════════════════════════════════════════════════════════════════
//  Helpers – build Firebase paths under /users/{uid}/...
// ════════════════════════════════════════════════════════════════════════════
// These save us from repeating String("/users/") + USER_UID + "/..." everywhere
static String fbPath(const char* suffix) {
  return String("/users/") + USER_UID + suffix;
}

// ════════════════════════════════════════════════════════════════════════════
//  SETUP
// ════════════════════════════════════════════════════════════════════════════
void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println("\n\n=== Smart Home Monitor – Booting ===");

  // Set all LED pins as OUTPUT, default LOW (off)
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++) {
    pinMode(AVAILABLE_PINS[i], OUTPUT);
    digitalWrite(AVAILABLE_PINS[i], LOW);
    ledPinState[AVAILABLE_PINS[i]] = false;
  }

  // Sensor pin modes
  pinMode(PIN_PIR, INPUT);   // PIR: digital input
  // PIN_WATER (GPIO35) is analog-only – no pinMode needed
  // PIN_DHT22 (GPIO27) is managed by the DHT library
  dht.begin();

  // Initialise EEPROM and print any stored messages to Serial Monitor
  EEPROM.begin(EEPROM_SIZE);
  eepromInit();
  eepromPrintMessages();

  // Connect to WiFi
  Serial.printf("[WiFi] Connecting to %s", WIFI_SSID);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.printf("\n[WiFi] Connected. IP: %s\n", WiFi.localIP().toString().c_str());

  // Sync real-world clock via NTP (needed for correct chart timestamps)
  configTime(0, 0, "pool.ntp.org", "time.nist.gov");
  Serial.print("[NTP] Waiting for time sync");
  time_t now = 0;
  for (int i = 0; i < 20 && now < 1000000000UL; i++) {
    delay(500);
    Serial.print(".");
    now = time(nullptr);
  }
  if (now >= 1000000000UL) {
    Serial.printf("\n[NTP] Time synced: %lu\n", (unsigned long)now);
  } else {
    Serial.println("\n[NTP] Sync failed – timestamps will be inaccurate.");
  }

  // Connect to Firebase
  fbConfig.api_key               = FIREBASE_API_KEY;
  fbConfig.database_url          = FIREBASE_DB_URL;
  fbConfig.token_status_callback = tokenStatusCallback;
  fbAuth.user.email              = FIREBASE_EMAIL;
  fbAuth.user.password           = FIREBASE_PASSWORD;

  Firebase.begin(&fbConfig, &fbAuth);
  Firebase.reconnectWiFi(true);

  Serial.print("[Firebase] Authenticating");
  unsigned long t0 = millis();
  while (!Firebase.ready()) {
    if (millis() - t0 > 20000) { Serial.println("\n[Firebase] Timeout!"); break; }
    delay(500);
    Serial.print(".");
  }
  Serial.println("\n[Firebase] Ready.\n");
  Serial.printf("[Firebase] Writing to: /users/%s/...\n\n", USER_UID);
}

// ════════════════════════════════════════════════════════════════════════════
//  LOOP
// ════════════════════════════════════════════════════════════════════════════
void loop() {
  // Push sensor readings every 5 seconds
  if (millis() - lastSensorPushMs >= 5000) {
    lastSensorPushMs = millis();
    pushSensorData();
  }

  syncLEDStates();       // Poll Firebase /leds every 2 s and apply to GPIOs
  checkPendingMessage(); // Poll /pendingMessage every 3 s for web commands

  // Execute UART commands typed in Serial Monitor
  while (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() > 0) handleUARTCommand(cmd);
  }

  delay(50);
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – initialise
// ════════════════════════════════════════════════════════════════════════════
void eepromInit() {
  bool uartOK  = (EEPROM.read(ADDR_UART_MAGIC)  == UART_MAGIC);
  bool floodOK = (EEPROM.read(ADDR_FLOOD_MAGIC) == FLOOD_MAGIC);

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
  EEPROM.commit();
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – save a UART message (circular buffer, oldest gets overwritten)
// ════════════════════════════════════════════════════════════════════════════
void eepromSaveUARTMsg(const char* text, uint32_t ts) {
  uint8_t idx  = EEPROM.read(ADDR_UART_IDX) % MSG_COUNT;
  int     base = ADDR_UART_DATA + idx * MSG_RECORD_LEN;

  int len = strlen(text);
  if (len > MSG_TEXT_LEN) len = MSG_TEXT_LEN;

  for (int i = 0; i <= MSG_TEXT_LEN; i++)
    EEPROM.write(base + i, (i < len) ? (uint8_t)text[i] : 0);

  // Timestamp stored big-endian (4 bytes)
  EEPROM.write(base + MSG_TEXT_LEN + 1, (uint8_t)(ts >> 24));
  EEPROM.write(base + MSG_TEXT_LEN + 2, (uint8_t)(ts >> 16));
  EEPROM.write(base + MSG_TEXT_LEN + 3, (uint8_t)(ts >>  8));
  EEPROM.write(base + MSG_TEXT_LEN + 4, (uint8_t)(ts));

  EEPROM.write(ADDR_UART_IDX, (idx + 1) % MSG_COUNT);
  EEPROM.commit();
  Serial.printf("[EEPROM] Saved UART msg in slot %d\n", idx);
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – print all stored messages to Serial Monitor on boot
// ════════════════════════════════════════════════════════════════════════════
void eepromPrintMessages() {
  Serial.println("─── Stored UART Messages (EEPROM) ──────────────");
  bool found = false;
  for (int i = 0; i < MSG_COUNT; i++) {
    int  base = ADDR_UART_DATA + i * MSG_RECORD_LEN;
    char text[MSG_TEXT_LEN + 1];
    for (int j = 0; j <= MSG_TEXT_LEN; j++)
      text[j] = (char)EEPROM.read(base + j);
    text[MSG_TEXT_LEN] = '\0';
    if (text[0] == '\0') continue;

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
//  EEPROM – save a flood event
// ════════════════════════════════════════════════════════════════════════════
void eepromSaveFloodEvent(uint32_t ts, uint8_t sensorVal) {
  uint8_t idx  = EEPROM.read(ADDR_FLOOD_IDX) % FLOOD_COUNT;
  int     base = ADDR_FLOOD_DATA + idx * FLOOD_RECORD_LEN;
  EEPROM.write(base,     (uint8_t)(ts >> 24));
  EEPROM.write(base + 1, (uint8_t)(ts >> 16));
  EEPROM.write(base + 2, (uint8_t)(ts >>  8));
  EEPROM.write(base + 3, (uint8_t)(ts));
  EEPROM.write(base + 4, sensorVal);
  EEPROM.write(ADDR_FLOOD_IDX, (idx + 1) % FLOOD_COUNT);
  EEPROM.commit();
  Serial.printf("[EEPROM] Flood event saved in slot %d\n", idx);
}

// ════════════════════════════════════════════════════════════════════════════
//  LED Helper – set a GPIO pin, skip if not in AVAILABLE_PINS list
// ════════════════════════════════════════════════════════════════════════════
void setLEDPin(int pin, bool state) {
  bool allowed = false;
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++)
    if (AVAILABLE_PINS[i] == pin) { allowed = true; break; }
  if (!allowed) {
    Serial.printf("[LED] Rejected pin %d – not in AVAILABLE_PINS!\n", pin);
    return;
  }
  // Skip if already in requested state (avoids unnecessary digitalWrite)
  if (ledPinState.count(pin) && ledPinState[pin] == state) return;
  digitalWrite(pin, state ? HIGH : LOW);
  ledPinState[pin] = state;
  Serial.printf("[LED] GPIO %-2d → %s\n", pin, state ? "ON" : "OFF");
}

// ════════════════════════════════════════════════════════════════════════════
//  UART Command Handler
//  A<pin>  → turn ON   (e.g. "A2"  or "A18")
//  S<pin>  → turn OFF  (e.g. "S2"  or "S18")
// ════════════════════════════════════════════════════════════════════════════
void handleUARTCommand(const String& cmd) {
  if (cmd.length() < 2) return;
  char action = cmd.charAt(0);
  if (action != 'A' && action != 'S') {
    Serial.printf("[UART] Unknown command: %s\n", cmd.c_str());
    return;
  }
  int  pin   = cmd.substring(1).toInt();
  bool state = (action == 'A');
  setLEDPin(pin, state);

  uint32_t ts = (uint32_t)time(nullptr);
  eepromSaveUARTMsg(cmd.c_str(), ts);

  // Sync the new state back to Firebase so the website LED Control page updates
  if (Firebase.ready()) {
    FirebaseData scanFbdo;
    String ledsPath = fbPath("/leds");
    if (Firebase.RTDB.getJSON(&scanFbdo, ledsPath.c_str())) {
      FirebaseJson& json = scanFbdo.jsonObject();
      size_t count = json.iteratorBegin();
      for (size_t i = 0; i < count; i++) {
        int type; String key, value;
        json.iteratorGet(i, type, key, value);
        // Iterator returns flattened paths like "led_1/pin", "led_1/state"
        if (key.endsWith("/pin") && value.toInt() == pin) {
          String ledId     = key.substring(0, key.indexOf('/'));
          String statePath = fbPath("/leds/") + ledId + "/state";
          Firebase.RTDB.setBool(&fbdo, statePath.c_str(), state);
          break;
        }
      }
      json.iteratorEnd();
    }
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Sensor Push  (called every 5 s from loop)
// ════════════════════════════════════════════════════════════════════════════
void pushSensorData() {
  if (!Firebase.ready()) {
    Serial.println("[Sensor] Firebase not ready – skipping.");
    return;
  }

  // Temperature & humidity from DHT22 (single-wire digital protocol)
  float tempC    = dht.readTemperature();  // degrees Celsius
  float humidity = dht.readHumidity();     // percent
  if (isnan(tempC))    tempC    = -99.0f;  // -99 signals a read failure
  if (isnan(humidity)) humidity =   0.0f;

  // Water level sensor: analog output (0–4095).
  // Dry probes ≈ 0; submerged probes rise above WATER_FLOOD_THRESHOLD.
  int  waterRaw = analogRead(PIN_WATER);
  bool flood    = (waterRaw > WATER_FLOOD_THRESHOLD);

  bool motion = (digitalRead(PIN_PIR) == HIGH);

  // LDR 5537 (voltage divider): higher ADC value = darker room.
  // Map 0–4095 to a 0–100 light-level percentage (100 = brightest).
  int ldrRaw   = analogRead(PIN_LDR);
  int lightPct = map(ldrRaw, 0, 4095, 100, 0);  // invert: bright → high %

  uint32_t ts = (uint32_t)time(nullptr);

  // Write sensor values
  FirebaseJson sensorJson;
  sensorJson.set("temperature", tempC);
  sensorJson.set("humidity",    humidity);
  sensorJson.set("flood",       flood);
  sensorJson.set("motion",      motion);
  sensorJson.set("light",       lightPct);
  sensorJson.set("lastUpdated", (int)ts);

  String sensorsPath = fbPath("/sensors");
  if (!Firebase.RTDB.setJSON(&fbdo, sensorsPath.c_str(), &sensorJson)) {
    Serial.printf("[Sensor] Push failed: %s\n", fbdo.errorReason().c_str());
    return;
  }
  Serial.printf("[Sensor] temp=%.1f C  hum=%.1f%%  flood=%d (raw=%d)  motion=%d  light=%d%%\n",
                tempC, humidity, (int)flood, waterRaw, (int)motion, lightPct);

  // Append temperature history (cleanup handled by the web dashboard)
  if (tempC != -99.0f) {
    FirebaseJson histJson;
    histJson.set("value",     tempC);
    histJson.set("timestamp", (int)ts);
    String histPath = fbPath("/temperatureHistory");
    if (!Firebase.RTDB.pushJSON(&fbdoHist, histPath.c_str(), &histJson)) {
      Serial.printf("[History] Push failed: %s\n", fbdoHist.errorReason().c_str());
    }
  }

  // Flood event – trigger only on the rising edge (dry → wet)
  if (flood && !lastFloodState) {
    Serial.println("[Flood] FLOOD DETECTED!");
    eepromSaveFloodEvent(ts, 1);

    // Push to /floodEvents – the Node.js backend listener detects this
    // new child and sends an email alert to your account
    FirebaseJson evtJson;
    evtJson.set("timestamp",    (int)ts);
    evtJson.set("sensorValue",  waterRaw);
    evtJson.set("acknowledged", false);
    String evtPath = fbPath("/floodEvents");
    Firebase.RTDB.pushJSON(&fbdo, evtPath.c_str(), &evtJson);
  }
  lastFloodState = flood;
}

// ════════════════════════════════════════════════════════════════════════════
//  LED State Sync  (called every 2 s from loop)
//  Downloads /users/{uid}/leds and applies each LED's state to its GPIO pin.
// ════════════════════════════════════════════════════════════════════════════
void syncLEDStates() {
  static unsigned long lastSync = 0;
  if (millis() - lastSync < 2000) return;
  lastSync = millis();
  if (!Firebase.ready()) return;

  FirebaseData ledFbdo;
  String ledsPath = fbPath("/leds");
  if (!Firebase.RTDB.getJSON(&ledFbdo, ledsPath.c_str())) return;

  // Collect up to 16 LEDs; iterator gives flattened keys like "led_1/pin"
  const int MAX_LEDS = 16;
  struct Entry { int pin = 0; bool state = false; };
  struct IDMap { String id; int idx; };
  Entry entries[MAX_LEDS];
  IDMap idMap[MAX_LEDS];
  int   numEntries = 0, idCount = 0;

  auto getIdx = [&](const String& id) -> int {
    for (int i = 0; i < idCount; i++)
      if (idMap[i].id == id) return idMap[i].idx;
    if (numEntries >= MAX_LEDS) return -1;
    idMap[idCount++] = { id, numEntries };
    return numEntries++;
  };

  FirebaseJson& json = ledFbdo.jsonObject();
  size_t count = json.iteratorBegin();
  for (size_t i = 0; i < count; i++) {
    int type; String key, value;
    json.iteratorGet(i, type, key, value);
    int slash = key.indexOf('/');
    if (slash < 0) continue;
    String ledId = key.substring(0, slash);
    String field = key.substring(slash + 1);
    int eIdx = getIdx(ledId);
    if (eIdx < 0) continue;
    if      (field == "pin")   entries[eIdx].pin   = value.toInt();
    else if (field == "state") entries[eIdx].state = (value == "true" || value == "1");
  }
  json.iteratorEnd();

  for (int i = 0; i < numEntries; i++)
    if (entries[i].pin > 0) setLEDPin(entries[i].pin, entries[i].state);
}

// ════════════════════════════════════════════════════════════════════════════
//  Pending Message Check  (called every 3 s from loop)
//  The website writes { text, timestamp } to /pendingMessage.
//  The ESP32 reads it, executes it as a UART command, and echoes it back
//  to /messages so the Messages page chat updates in real time.
// ════════════════════════════════════════════════════════════════════════════
void checkPendingMessage() {
  static unsigned long lastCheck = 0;
  if (millis() - lastCheck < 3000) return;
  lastCheck = millis();
  if (!Firebase.ready()) return;

  FirebaseData pmFbdo;
  String pmPath = fbPath("/pendingMessage");
  if (!Firebase.RTDB.getJSON(&pmFbdo, pmPath.c_str())) return;

  FirebaseJson&    json = pmFbdo.jsonObject();
  FirebaseJsonData tsData, textData;
  json.get(tsData,   "timestamp");
  json.get(textData, "text");
  if (!tsData.success || !textData.success) return;

  uint32_t msgTs  = (uint32_t)tsData.intValue;
  String   msgTxt = textData.stringValue;
  if (msgTs <= lastPendingMsgTs) return;   // Already handled
  lastPendingMsgTs = msgTs;

  Serial.printf("[PendingMsg] Received: \"%s\" (ts=%u)\n", msgTxt.c_str(), msgTs);
  eepromSaveUARTMsg(msgTxt.c_str(), msgTs);
  handleUARTCommand(msgTxt);

  // Echo back to /messages so the website chat shows the confirmation
  FirebaseJson msgJson;
  msgJson.set("text",      msgTxt);
  msgJson.set("timestamp", (int)msgTs);
  msgJson.set("source",    "esp32");
  String msgsPath = fbPath("/messages");
  Firebase.RTDB.pushJSON(&fbdo, msgsPath.c_str(), &msgJson);
}
