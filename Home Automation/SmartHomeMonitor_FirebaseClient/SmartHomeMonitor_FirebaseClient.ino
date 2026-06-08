/**
 * ╔══════════════════════════════════════════════════════════════════════════╗
 * ║          Smart Home Monitor – ESP32 Arduino IDE Firmware                ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  Board   : ESP32 Dev Module  (Tools → Board → ESP32 Arduino → ESP32     ║
 * ║            Dev Module)                                                   ║
 * ║  Libraries: FirebaseClient  ← Library Manager → search "FirebaseClient" ║
 * ║             by Mobizt, install latest (v2.x+)                           ║
 * ║             DHT sensor library  ← Library Manager → search              ║
 * ║             "DHT sensor library" by Adafruit, install latest             ║
 * ╠══════════════════════════════════════════════════════════════════════════╣
 * ║  IMPORTANT: Aceasta este versiunea migrată la noua librărie FirebaseClient
 * ║  (nu mai "Firebase ESP Client" v4.x care este deprecated/EOL).          ║
 * ║  BEFORE FLASHING – fill in every  ← YOU NEED THIS  comment below       ║
 * ╚══════════════════════════════════════════════════════════════════════════╝
 *
 * Sensors   : DHT22 AM2302 module (temperature + humidity),
 *             Water level sensor analog (flood), PIR HC-SR501 (motion),
 *             LDR 5537 photoresistor (light level)
 * Actuators : LEDs on GPIO pins you configure in the website's LED Control page
 *
 * UART Commands (Serial Monitor, 115200 baud, newline-terminated):
 *   A<pin>   turn ON  LED on that GPIO   e.g. "A2"  → GPIO 2 HIGH
 *   S<pin>   turn OFF LED on that GPIO   e.g. "S18" → GPIO 18 LOW
 *
 * Diferențe față de versiunea veche (Firebase_ESP_Client):
 *   - Include: <FirebaseClient.h> + <WiFiClientSecure.h>
 *   - Enable macros: #define ENABLE_USER_AUTH  #define ENABLE_DATABASE
 *   - Autentificare: UserAuth → FirebaseApp (async, gestionat cu app.loop())
 *   - Client SSL: WiFiClientSecure + AsyncClient
 *   - RTDB calls: Database.set() / .get() / .push() / .update()
 *   - JSON: object_t + JsonWriter (nu mai FirebaseJson)
 *   - appReady(): verifică dacă app s-a autentificat înainte de operații DB
 */

// ════════════════════════════════════════════════════════════════════════════
//  Enable only what we use – reduces flash size
// ════════════════════════════════════════════════════════════════════════════
#define ENABLE_USER_AUTH   // Email/password authentication
#define ENABLE_DATABASE    // Realtime Database

#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <FirebaseClient.h>
#include <EEPROM.h>
#include <map>
#include <DHT.h>     // Adafruit DHT sensor library

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 1 – Aici bag wifi ul
// ════════════════════════════════════════════════════════════════════════════
#define WIFI_SSID      "DIGI-24-75628D"
#define WIFI_PASSWORD  "628F018206"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 2 – Firebase Project Settings(pt web)
//
//  Where to find each value:
//  ┌─────────────────────┬───────────────────────────────────────────────────┐
//  │ FIREBASE_API_KEY    │ Firebase Console → Project Settings → General     │
//  │                     │ → "Web API Key"                                   │
//  ├─────────────────────┼───────────────────────────────────────────────────┤
//  │ FIREBASE_DB_URL     │ Firebase Console → Realtime Database → Data tab   │
//  │                     │ → URL at the top (ends in firebasedatabase.app)   │
//  ├─────────────────────┼───────────────────────────────────────────────────┤
//  │ FIREBASE_EMAIL      │ A dedicated email/password user you create in      │
//  │ FIREBASE_PASSWORD   │ Firebase Console → Authentication → Users          │
//  │                     │ → Add user  (e.g. esp32@yourproject.com / anything)│
//  └─────────────────────┴───────────────────────────────────────────────────┘
// ════════════════════════════════════════════════════════════════════════════
#define FIREBASE_API_KEY   "AIzaSyBgpeP-MbVxE9v-dgN4I0aDz-xb4LStNI8"
#define FIREBASE_DB_URL    "https://alea-alea-d4226-default-rtdb.europe-west1.firebasedatabase.app/"
#define FIREBASE_EMAIL     "leonard.chiriac@student.upt.ro"
#define FIREBASE_PASSWORD  "QWE-676-LOL"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 3 – Your Firebase User UID
//  Firebase Console → Authentication → Users → "User UID" column
//  Looks like: abc123XYZ789... (28 characters)
// ════════════════════════════════════════════════════════════════════════════
#define USER_UID  "O9anpGoqDYdQCng0llGTxTADTqG3"

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 4 – Sensor Pins
// ════════════════════════════════════════════════════════════════════════════
#define PIN_DHT22   27    // Digital – DHT22 AM2302 temperature + humidity
#define DHT_TYPE    DHT22
#define PIN_WATER   35    // Analog  – water level sensor (flood detection)
#define PIN_PIR     32    // Digital – PIR HC-SR501 motion sensor
#define PIN_LDR     33    // Analog  – LDR 5537 photoresistor (voltage divider)
//
//  LDR wiring (voltage divider):
//    3.3V ──── LDR 5537 ──── GPIO 33 ──── 10 kΩ resistor ──── GND
//                                  ↑ analog read here
//  In bright light : LDR ≈ 18–50 kΩ  → ADC value LOWER
//  In darkness     : LDR ≈ 2 MΩ      → ADC value HIGHER
//  ADC (0-4095): higher = darker room

// Water flood threshold (0–4095, 12-bit ADC)
#define WATER_FLOOD_THRESHOLD  500

// ════════════════════════════════════════════════════════════════════════════
//  ★ STEP 5 – GPIO Pins Available for LEDs
//  Must match the AVAILABLE_PINS list in the React frontend.
//  Do NOT include sensor pins here.
// ════════════════════════════════════════════════════════════════════════════
const int AVAILABLE_PINS[]     = {2, 4, 5, 18, 19, 21, 22, 23, 13, 12, 14};
const int AVAILABLE_PINS_COUNT = sizeof(AVAILABLE_PINS) / sizeof(AVAILABLE_PINS[0]);

// DHT22 sensor instance
DHT dht(PIN_DHT22, DHT_TYPE);

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM Layout  (unchanged from previous version)
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
//  FirebaseClient objects  (replaces FirebaseData / FirebaseAuth / FirebaseConfig)
// ════════════════════════════════════════════════════════════════════════════

// Authentication: email + password (UserAuth replaces the old fbAuth/fbConfig pair)
UserAuth user_auth(FIREBASE_API_KEY, FIREBASE_EMAIL, FIREBASE_PASSWORD);

// FirebaseApp manages token refresh internally – just call app.loop() in loop()
FirebaseApp app;

// SSL clients – the new library uses WiFiClientSecure directly
// We use two clients: one for sync DB calls, one for async token refresh
WiFiClientSecure ssl_client1, ssl_client2;

// AsyncClient wraps the SSL client.  'using' alias is needed by the library.
using AsyncClient = AsyncClientClass;
AsyncClient aClient1(ssl_client1), aClient2(ssl_client2);

// Realtime Database instance (replaces Firebase.RTDB.* calls)
RealtimeDatabase Database;

// ════════════════════════════════════════════════════════════════════════════
//  Runtime state
// ════════════════════════════════════════════════════════════════════════════
unsigned long    lastSensorPushMs = 0;
uint32_t         lastPendingMsgTs = 0;
bool             lastFloodState   = false;
std::map<int,bool> ledPinState;

// ════════════════════════════════════════════════════════════════════════════
//  Async callback – required by FirebaseApp / Database for async operations.
//  We use sync calls for simplicity, but the callback is still needed.
// ════════════════════════════════════════════════════════════════════════════
void asyncCB(AsyncResult &aResult) {
  if (aResult.isError()) {
    Serial.printf("[Firebase async] error: %s (%d)\n",
                  aResult.error().message().c_str(),
                  aResult.error().code());
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Helpers – build Firebase paths under /users/{uid}/...
// ════════════════════════════════════════════════════════════════════════════
static String fbPath(const char* suffix) {
  return String("/users/") + USER_UID + suffix;
}

// Convenience: is the FirebaseApp authenticated and ready?
static bool appReady() {
  return app.ready();
}

// ════════════════════════════════════════════════════════════════════════════
//  SETUP
// ════════════════════════════════════════════════════════════════════════════
void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println("\n\n=== Smart Home Monitor – Booting (FirebaseClient) ===");

  // Set all LED pins as OUTPUT, default LOW (off)
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++) {
    pinMode(AVAILABLE_PINS[i], OUTPUT);
    digitalWrite(AVAILABLE_PINS[i], LOW);
    ledPinState[AVAILABLE_PINS[i]] = false;
  }

  // Sensor pin modes
  pinMode(PIN_PIR, INPUT);
  // PIN_WATER (GPIO35) is analog-only – no pinMode needed
  dht.begin();

  // Initialise EEPROM and print stored messages
  EEPROM.begin(EEPROM_SIZE);
  eepromInit();
  eepromPrintMessages();

  // ── Connect to WiFi ────────────────────────────────────────────────────
  Serial.printf("[WiFi] Connecting to %s", WIFI_SSID);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.printf("\n[WiFi] Connected. IP: %s\n", WiFi.localIP().toString().c_str());

  // ── Firebase: skip certificate verification (simplest for ESP32) ───────
  //  For production you should load the root CA instead:
  //  ssl_client1.setCACert(rootCACert);
  ssl_client1.setInsecure();
  ssl_client2.setInsecure();

  // ── Initialise FirebaseApp with UserAuth (email/password) ──────────────
  //  initializeApp() starts the async token request.
  //  app.loop() must be called repeatedly in loop() until app.ready().
  initializeApp(aClient2, app, getAuth(user_auth), asyncCB, "authTask");

  // Bind the Database instance to the authenticated app
  app.getApp<RealtimeDatabase>(Database);
  Database.url(FIREBASE_DB_URL);

  // Wait for authentication (up to 20 s)
  Serial.print("[Firebase] Authenticating");
  unsigned long t0 = millis();
  while (!app.ready()) {
    app.loop();   // MUST be called while waiting
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
//  LOOP
// ════════════════════════════════════════════════════════════════════════════
void loop() {
  // REQUIRED: keep token refresh and async operations alive
  app.loop();

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
//  EEPROM – initialise  (unchanged)
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
//  EEPROM – save a UART message  (unchanged)
// ════════════════════════════════════════════════════════════════════════════
void eepromSaveUARTMsg(const char* text, uint32_t ts) {
  uint8_t idx  = EEPROM.read(ADDR_UART_IDX) % MSG_COUNT;
  int     base = ADDR_UART_DATA + idx * MSG_RECORD_LEN;
  int     len  = strlen(text);
  if (len > MSG_TEXT_LEN) len = MSG_TEXT_LEN;
  for (int i = 0; i <= MSG_TEXT_LEN; i++)
    EEPROM.write(base + i, (i < len) ? (uint8_t)text[i] : 0);
  EEPROM.write(base + MSG_TEXT_LEN + 1, (uint8_t)(ts >> 24));
  EEPROM.write(base + MSG_TEXT_LEN + 2, (uint8_t)(ts >> 16));
  EEPROM.write(base + MSG_TEXT_LEN + 3, (uint8_t)(ts >>  8));
  EEPROM.write(base + MSG_TEXT_LEN + 4, (uint8_t)(ts));
  EEPROM.write(ADDR_UART_IDX, (idx + 1) % MSG_COUNT);
  EEPROM.commit();
  Serial.printf("[EEPROM] Saved UART msg in slot %d\n", idx);
}

// ════════════════════════════════════════════════════════════════════════════
//  EEPROM – print stored messages to Serial Monitor on boot  (unchanged)
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
//  EEPROM – save a flood event  (unchanged)
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
//  (logic unchanged, hardware stays the same)
// ════════════════════════════════════════════════════════════════════════════
void setLEDPin(int pin, bool state) {
  bool allowed = false;
  for (int i = 0; i < AVAILABLE_PINS_COUNT; i++)
    if (AVAILABLE_PINS[i] == pin) { allowed = true; break; }
  if (!allowed) {
    Serial.printf("[LED] Rejected pin %d – not in AVAILABLE_PINS!\n", pin);
    return;
  }
  if (ledPinState.count(pin) && ledPinState[pin] == state) return;
  digitalWrite(pin, state ? HIGH : LOW);
  ledPinState[pin] = state;
  Serial.printf("[LED] GPIO %-2d → %s\n", pin, state ? "ON" : "OFF");
}

// ════════════════════════════════════════════════════════════════════════════
//  UART Command Handler
//  A<pin>  → turn ON   (e.g. "A2" or "A18")
//  S<pin>  → turn OFF  (e.g. "S2" or "S18")
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

  uint32_t ts = (uint32_t)(millis() / 1000UL);
  eepromSaveUARTMsg(cmd.c_str(), ts);

  // Sync the new LED state back to Firebase so the web UI updates
  if (!appReady()) return;

  // Read /leds as a raw JSON string and find which led_N owns this pin
  // ─────────────────────────────────────────────────────────────────
  // In FirebaseClient, Database.get<String>() returns the JSON payload as a
  // raw String when the path points to a JSON node.
  String ledsPath = fbPath("/leds");
  String ledsJson = Database.get<String>(aClient1, ledsPath);

  if (aClient1.lastError().code() != 0) {
    Serial.printf("[UART] Failed to read /leds: %s\n",
                  aClient1.lastError().message().c_str());
    return;
  }

  // Parse: look for "pin":<pin> inside each "led_N" object
  // We do a simple string scan – avoids a full JSON parser dependency.
  // Pattern: search for the pin value, then backtrack to find the led ID.
  String pinStr = "\"pin\":" + String(pin);
  int pinIdx    = ledsJson.indexOf(pinStr);
  if (pinIdx < 0) {
    Serial.printf("[UART] Pin %d not found in /leds – adding anyway.\n", pin);
    return;
  }

  // Find the led key (format: "led_N": { ... "pin":<pin> ... } )
  int keyStart = ledsJson.lastIndexOf('"', pinIdx - 3);
  int keyEnd   = ledsJson.indexOf('"', keyStart + 1);
  if (keyStart < 0 || keyEnd < 0) return;
  String ledId     = ledsJson.substring(keyStart + 1, keyEnd);
  String statePath = fbPath("/leds/") + ledId + "/state";

  bool ok = Database.set<bool>(aClient1, statePath, state);
  if (!ok) {
    Serial.printf("[UART] Failed to update LED state: %s\n",
                  aClient1.lastError().message().c_str());
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Sensor Push  (called every 5 s from loop)
//
//  Migration notes:
//    OLD: FirebaseJson + Firebase.RTDB.setJSON(&fbdo, path, &json)
//    NEW: object_t + JsonWriter + Database.set<object_t>(aClient, path, obj)
//         Database.push<object_t>(aClient, path, obj)  for appending history
// ════════════════════════════════════════════════════════════════════════════
void pushSensorData() {
  if (!appReady()) {
    Serial.println("[Sensor] Firebase not ready – skipping.");
    return;
  }

  // Read sensors
  float tempC    = dht.readTemperature();
  float humidity = dht.readHumidity();
  if (isnan(tempC))    tempC    = -99.0f;
  if (isnan(humidity)) humidity =   0.0f;

  int  waterRaw = analogRead(PIN_WATER);
  bool flood    = (waterRaw > WATER_FLOOD_THRESHOLD);
  bool motion   = (digitalRead(PIN_PIR) == HIGH);
  int  ldrRaw   = analogRead(PIN_LDR);
  int  lightPct = map(ldrRaw, 0, 4095, 100, 0);

  uint32_t ts = (uint32_t)(millis() / 1000UL);

  Serial.printf("[Sensor] temp=%.1f°C  hum=%.1f%%  flood=%d (raw=%d)  motion=%d  light=%d%%\n",
                tempC, humidity, (int)flood, waterRaw, (int)motion, lightPct);

  // ── Build sensor JSON with JsonWriter ───────────────────────────────────
  //  JsonWriter creates nested object_t values.
  //  writer.create(obj, "key", value)  →  { "key": value }
  //  writer.join(target, N, obj1, obj2, ...)  →  merges N objects into target
  object_t obj_temp, obj_hum, obj_flood, obj_motion, obj_light, obj_ts;
  object_t sensorObj;
  JsonWriter writer;

  writer.create(obj_temp,   "temperature", number_t(tempC, 1));
  writer.create(obj_hum,    "humidity",    number_t(humidity, 1));
  writer.create(obj_flood,  "flood",       boolean_t(flood));
  writer.create(obj_motion, "motion",      boolean_t(motion));
  writer.create(obj_light,  "light",       lightPct);
  writer.create(obj_ts,     "lastUpdated", (int)ts);
  writer.join(sensorObj, 6, obj_temp, obj_hum, obj_flood, obj_motion, obj_light, obj_ts);

  String sensorsPath = fbPath("/sensors");
  bool ok = Database.set<object_t>(aClient1, sensorsPath, sensorObj);
  if (!ok) {
    Serial.printf("[Sensor] Push failed: %s\n",
                  aClient1.lastError().message().c_str());
    return;
  }

  // ── Append temperature history entry ────────────────────────────────────
  //  Database.push() creates a new child with an auto-generated key (like
  //  the old Firebase.RTDB.pushJSON).
  object_t histVal, histTs, histObj;
  writer.create(histVal, "value",     number_t(tempC, 1));
  writer.create(histTs,  "timestamp", (int)ts);
  writer.join(histObj, 2, histVal, histTs);

  String histPath = fbPath("/temperatureHistory");
  Database.push<object_t>(aClient1, histPath, histObj);

  // ── Flood event – rising-edge only ──────────────────────────────────────
  if (flood && !lastFloodState) {
    Serial.println("[Flood] FLOOD DETECTED!");
    eepromSaveFloodEvent(ts, 1);

    object_t evtTs, evtRaw, evtAck, evtObj;
    writer.create(evtTs,  "timestamp",    (int)ts);
    writer.create(evtRaw, "sensorValue",  waterRaw);
    writer.create(evtAck, "acknowledged", boolean_t(false));
    writer.join(evtObj, 3, evtTs, evtRaw, evtAck);

    String evtPath = fbPath("/floodEvents");
    Database.push<object_t>(aClient1, evtPath, evtObj);
  }
  lastFloodState = flood;
}

// ════════════════════════════════════════════════════════════════════════════
//  LED State Sync  (called every 2 s from loop)
//  Downloads /users/{uid}/leds as a JSON String, parses pin/state pairs,
//  and applies each LED's state to its GPIO pin.
//
//  Migration notes:
//    OLD: Firebase.RTDB.getJSON(&ledFbdo, path) then FirebaseJson iterator
//    NEW: Database.get<String>(aClient, path)  → raw JSON string
//         Simple String parsing for "pin":<N> and "state":true/false
// ════════════════════════════════════════════════════════════════════════════
void syncLEDStates() {
  static unsigned long lastSync = 0;
  if (millis() - lastSync < 2000) return;
  lastSync = millis();
  if (!appReady()) return;

  String ledsPath = fbPath("/leds");
  String ledsJson = Database.get<String>(aClient1, ledsPath);

  // Debug: printează JSON-ul brut primit de la Firebase
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

  // Parsare robustă: pentru fiecare "pin":<N> găsim blocul { } care îl conține
  // și căutăm "state" DOAR în acel bloc – indiferent de ordinea cheilor în JSON.
  // Acceptă "state":true / "state":false / "state":1 / "state":0
  int searchFrom = 0;
  while (true) {
    int pinKeyIdx = ledsJson.indexOf("\"pin\":", searchFrom);
    if (pinKeyIdx < 0) break;

    // Citește valoarea pin-ului (sărim peste spații posibile)
    int pinValStart = pinKeyIdx + 6;
    while (pinValStart < (int)ledsJson.length() && ledsJson[pinValStart] == ' ')
      pinValStart++;
    int pinValEnd = pinValStart;
    while (pinValEnd < (int)ledsJson.length() &&
           (isdigit(ledsJson[pinValEnd]) || ledsJson[pinValEnd] == '-'))
      pinValEnd++;
    int pin = ledsJson.substring(pinValStart, pinValEnd).toInt();

    // Găsim { } blocul led-ului care conține acest "pin"
    int blockStart = ledsJson.lastIndexOf('{', pinKeyIdx);
    int blockEnd   = ledsJson.indexOf('}', pinKeyIdx);
    if (blockStart < 0 || blockEnd < 0) { searchFrom = pinValEnd; continue; }

    String block = ledsJson.substring(blockStart, blockEnd + 1);
    Serial.printf("[LED Sync] Pin %d | block: %s\n", pin, block.c_str());

    // Caută "state" în bloc – acceptă true/1 ca ON, false/0 ca OFF
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
    setLEDPin(pin, state);
    searchFrom = blockEnd + 1;
  }
}

// ════════════════════════════════════════════════════════════════════════════
//  Pending Message Check  (called every 3 s from loop)
//  The website writes { text, timestamp } to /pendingMessage.
//  The ESP32 reads it, executes it as a UART command, and echoes back
//  to /messages so the chat page updates in real time.
//
//  Migration notes:
//    OLD: Firebase.RTDB.getJSON(&pmFbdo, path) then FirebaseJsonData
//    NEW: Database.get<String>(aClient, path) → raw JSON
//         Simple String extraction for "timestamp" and "text"
// ════════════════════════════════════════════════════════════════════════════
void checkPendingMessage() {
  static unsigned long lastCheck = 0;
  if (millis() - lastCheck < 3000) return;
  lastCheck = millis();
  if (!appReady()) return;

  String pmPath = fbPath("/pendingMessage");
  String pmJson = Database.get<String>(aClient1, pmPath);

  if (aClient1.lastError().code() != 0) return;
  if (pmJson.length() == 0) return;

  // Extract timestamp
  int tsIdx = pmJson.indexOf("\"timestamp\":");
  if (tsIdx < 0) return;
  int tsValStart = tsIdx + 12;
  int tsValEnd   = pmJson.indexOf(',', tsValStart);
  if (tsValEnd < 0) tsValEnd = pmJson.indexOf('}', tsValStart);
  if (tsValEnd < 0) return;
  uint32_t msgTs = (uint32_t)pmJson.substring(tsValStart, tsValEnd).toInt();

  if (msgTs <= lastPendingMsgTs) return;  // Already handled
  lastPendingMsgTs = msgTs;

  // Extract text
  int textIdx = pmJson.indexOf("\"text\":\"");
  if (textIdx < 0) return;
  int textStart = textIdx + 8;
  int textEnd   = pmJson.indexOf('"', textStart);
  if (textEnd < 0) return;
  String msgTxt = pmJson.substring(textStart, textEnd);

  Serial.printf("[PendingMsg] Received: \"%s\" (ts=%u)\n", msgTxt.c_str(), msgTs);
  eepromSaveUARTMsg(msgTxt.c_str(), msgTs);
  handleUARTCommand(msgTxt);

  // Echo back to /messages for the website chat
  object_t msgText, msgTimestamp, msgSource, msgObj;
  JsonWriter writer;
  writer.create(msgText,      "text",      string_t(msgTxt.c_str()));
  writer.create(msgTimestamp, "timestamp", (int)msgTs);
  writer.create(msgSource,    "source",    string_t("esp32"));
  writer.join(msgObj, 3, msgText, msgTimestamp, msgSource);

  String msgsPath = fbPath("/messages");
  Database.push<object_t>(aClient1, msgsPath, msgObj);
}
