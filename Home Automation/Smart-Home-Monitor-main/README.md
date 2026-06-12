# Smart Home Monitor & Control System

A full-stack IoT project combining an **ESP32** microcontroller, **Node.js + Express** backend,
**Firebase Realtime Database**, and a **React + TailwindCSS** frontend. Each user's data is fully
isolated — all Firebase paths live under `/users/{uid}/...`.

---

## What it does

| Feature | Description |
|---|---|
| **Live sensor dashboard** | Temperature, humidity, ambient light, motion, flood — updated every 5 s |
| **Temperature chart** | Last 20 readings plotted over time |
| **Flood alerts** | Banner on dashboard + automatic email when water is detected |
| **LED control** | Add GPIO-mapped LEDs, toggle them from the web or via UART commands |
| **Messages / UART chat** | Send `A<pin>` / `S<pin>` commands to the ESP32 from the browser |
| **Flood event log** | Full history of every flood event with delete |
| **Simulator** | Push fake sensor data without real hardware (for development / testing) |
| **Google Sign-In** | Each user gets their own isolated data space |

---

## Architecture

```
┌──────────────┐     WiFi → Firebase RTDB      ┌────────────────────────┐
│    ESP32     │ ──────────────────────────────► │  /users/{uid}/         │
│  (firmware)  │ ◄────────────────────────────── │    sensors             │
└──────────────┘   push sensors / poll leds      │    leds                │
                                                 │    messages            │
┌──────────────┐     Admin SDK                   │    pendingMessage      │
│  Node.js /   │ ◄──────────────────────────────►│    floodEvents         │
│  Express     │   REST API + flood listener      │    temperatureHistory  │
└──────────────┘                                 └────────────────────────┘
        ▲                                                   ▲
        │  REST (auth'd)                          JS SDK (onValue live)
        │                                                   │
        └───────────────────────────────────────────────────┘
                              React Frontend
                          (Vite + TailwindCSS)
```

---

## Hardware components

All purchased from [sigmanortec.ro](https://sigmanortec.ro).

| Component | Link | Purpose |
|---|---|---|
| ESP32 DevKit v1 | [sigmanortec.ro](https://sigmanortec.ro/placa-dezvoltare-esp32-cu-wifi-si-bluetooth) | Main microcontroller |
| DHT22 AM2302 module | [sigmanortec.ro](https://sigmanortec.ro/senzor-temperatura-si-umiditate-dht22-am2302-original-modul) | Temperature + humidity |
| PIR HC-SR501 | [sigmanortec.ro](https://sigmanortec.ro/Senzor-PIR-miscare-p126182136) | Motion detection |
| Water level sensor | [sigmanortec.ro](https://sigmanortec.ro/Senzor-nivel-apa-lichid-p125423486) | Flood / water detection |
| LDR 5537 (5 mm photoresistor) | [sigmanortec.ro](https://sigmanortec.ro) | Ambient light level |
| Resistor kit (30 values × 20 pcs) | [sigmanortec.ro](https://sigmanortec.ro/kit-rezistori-30-valori-20-bucati) | LED current limiting (220 Ω) + LDR divider (10 kΩ) |
| LEDs — white, yellow, green, blue | sigmanortec.ro | Visual output indicators |
| Dupont wires 20 cm M-F (×40) | [sigmanortec.ro](https://sigmanortec.ro/40-Fire-Dupont-20cm-Tata-Mama-p210854317) | Connections |

---

## Project structure

```
/
├── firmware/
│   └── SmartHomeMonitor/
│       └── SmartHomeMonitor.ino   ← Arduino IDE sketch (open this file)
│
├── backend/                       ← Node.js + Express
│   ├── src/
│   │   ├── index.js
│   │   ├── middleware/auth.js
│   │   ├── routes/                (sensors, leds, messages, floodEvents)
│   │   ├── services/              (firebase.js, mailer.js)
│   │   └── listeners/             (floodListener.js)
│   ├── .env.example
│   └── package.json
│
└── frontend/                      ← React + Vite + TailwindCSS
    ├── src/
    │   ├── pages/                 (Dashboard, LEDControl, Messages, FloodEvents, Simulator, Login)
    │   ├── components/            (Navbar, SensorCard, TemperatureChart, LEDCard, AddLEDModal)
    │   ├── firebase.js
    │   ├── api.js
    │   ├── App.jsx
    │   └── main.jsx
    ├── .env.example
    └── package.json
```

---

## Part 1 — Firebase project setup

> Do this first. Both the backend and frontend need these values.

1. Go to [console.firebase.google.com](https://console.firebase.google.com) and create a project.

2. **Enable Google Sign-In**  
   Authentication → Sign-in method → Google → Enable.

3. **Create a Realtime Database**  
   Realtime Database → Create database → choose a region → start in **locked mode**.

4. **Set database rules** (Realtime Database → Rules tab):
   ```json
   {
     "rules": {
       "users": {
         "$uid": {
           ".read":  "$uid === auth.uid",
           ".write": "$uid === auth.uid"
         }
       }
     }
   }
   ```

5. **Add a Web app** (Project Settings → Your apps → Add app → Web).  
   Copy the `firebaseConfig` object — you'll need it for the frontend `.env`.

6. **Generate a service account key** (Project Settings → Service Accounts → Generate new private key).  
   Save the JSON file — you'll need it for the backend `.env`.

7. **Create an email/password user for the ESP32**  
   Authentication → Users → Add user → e.g. `esp32@yourproject.com` / any password.  
   Copy the **User UID** shown in the table — you'll paste it into the firmware.

---

## Part 2 — Node.js backend

### Requirements
- [Node.js](https://nodejs.org) v18 or newer

### Install & run

```bash
cd backend
cp .env .env
# → open .env and fill in every value (see table below)
npm install
npm run dev     # development — auto-restarts on file changes (nodemon)
npm start       # production
```

The server starts on **port 3001** by default.

### Environment variables (`backend/.env`)

| Variable | Where to find it |
|---|---|
| `FIREBASE_PROJECT_ID` | Firebase Console → Project Settings → General → Project ID |
| `FIREBASE_CLIENT_EMAIL` | Service account JSON → `client_email` field |
| `FIREBASE_PRIVATE_KEY` | Service account JSON → `private_key` field (keep the `\n` characters) |
| `FIREBASE_DB_URL` | Realtime Database → Data tab → URL at the top |
| `GMAIL_USER` | Your Gmail address |
| `GMAIL_APP_PASSWORD` | Google Account → Security → 2-Step Verification → App passwords ([guide](https://support.google.com/accounts/answer/185833)) |
| `CORS_ORIGINS` | Frontend URL, e.g. `http://localhost:5173` (comma-separated for multiple) |

### API endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/health` | Health check |
| GET | `/api/sensors` | Latest sensor readings |
| GET | `/api/leds` | All LEDs |
| POST | `/api/leds` | Add LED `{ pin, name }` |
| DELETE | `/api/leds/:id` | Remove LED |
| PATCH | `/api/leds/:id/state` | Set state `{ state: true/false }` |
| GET | `/api/messages` | Last 50 messages |
| POST | `/api/messages` | Send UART command `{ text }` |
| GET | `/api/flood-events` | All flood events |
| DELETE | `/api/flood-events/:id` | Delete a flood event |

All routes except `/health` require a Firebase ID token in the `Authorization: Bearer <token>` header.

---

## Part 3 — React frontend

### Requirements
- [Node.js](https://nodejs.org) v18 or newer

### Install & run

```bash
cd frontend
cp .env .env
# → open .env and fill in every value (see table below)
npm install
npm run dev     # development server → http://localhost:5173
npm run build   # production build → dist/
```

### Environment variables (`frontend/.env`)

All variables must be prefixed with `VITE_`.

| Variable | Where to find it |
|---|---|
| `VITE_FIREBASE_API_KEY` | Firebase Console → Project Settings → Your apps → Web app → `apiKey` |
| `VITE_FIREBASE_AUTH_DOMAIN` | same → `authDomain` |
| `VITE_FIREBASE_PROJECT_ID` | same → `projectId` |
| `VITE_FIREBASE_DATABASE_URL` | Realtime Database → Data tab → URL at the top |
| `VITE_FIREBASE_STORAGE_BUCKET` | same web app config → `storageBucket` |
| `VITE_FIREBASE_MESSAGING_SENDER_ID` | same → `messagingSenderId` |
| `VITE_FIREBASE_APP_ID` | same → `appId` |
| `VITE_API_BASE_URL` | Backend URL — `http://localhost:3001` |

### Pages

| Route | Page | Description |
|---|---|---|
| `/` | Dashboard | Live sensor cards + temperature chart + flood banner |
| `/leds` | LED Control | Add, delete, toggle GPIO-mapped LEDs |
| `/messages` | Messages | UART chat — type `A<pin>` / `S<pin>` to control LEDs |
| `/flood-events` | Flood Events | Full flood event log with delete |
| `/simulator` | Simulator | Push fake sensor data without hardware |
| `/login` | Login | Google Sign-In |

All routes except `/login` require authentication. Unauthenticated visitors are redirected to `/login`.

---

## Part 4 — ESP32 firmware (Arduino IDE)

### Requirements

- [Arduino IDE 2.x](https://www.arduino.cc/en/software)
- **ESP32 board package** — install via Arduino IDE:  
  File → Preferences → Additional boards manager URLs → add:  
  `https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json`  
  Then: Tools → Board → Boards Manager → search `esp32` by Espressif → Install.
- **Firebase ESP Client** library by Mobizt — Library Manager → search `Firebase ESP Client` → install v4.x
- **DHT sensor library** by Adafruit — Library Manager → search `DHT sensor library` → install latest  
  (it will also prompt to install **Adafruit Unified Sensor** — install that too)

### Hardware wiring

| Component | ESP32 GPIO | Notes |
|---|---|---|
| DHT22 module — DO | GPIO 27 | Single-wire digital |
| DHT22 module — VCC | 3.3 V | |
| Water level sensor — S | GPIO 35 | Analog (input-only pin) |
| Water level sensor — VCC | 3.3 V | |
| PIR HC-SR501 — OUT | GPIO 32 | Digital |
| PIR HC-SR501 — VCC | VIN (5 V) | PIR requires 5 V |
| LDR 5537 — one leg | 3.3 V | |
| LDR 5537 — other leg | GPIO 33 **and** 10 kΩ to GND | Voltage divider |
| LEDs (each) | GPIO 2 / 4 / 5 / 18 / 19 / 21 / 22 / 23 | 220 Ω resistor in series to GND |

**LDR voltage divider diagram:**
```
3.3V ── LDR 5537 ──┬── GPIO 33
                   └── 10 kΩ ── GND
```

### Flash steps

1. Open `firmware/SmartHomeMonitor/SmartHomeMonitor.ino` in Arduino IDE.
2. Select board: Tools → Board → ESP32 Arduino → **ESP32 Dev Module**.
3. Select the correct port: Tools → Port → `COM_` (Windows) or `/dev/ttyUSB_` (Linux/Mac).
4. Fill in the 5 credential blocks at the top of the file (each marked `← YOU NEED THIS`):

   ```cpp
   #define WIFI_SSID         "your-wifi-name"
   #define WIFI_PASSWORD     "your-wifi-password"

   #define FIREBASE_API_KEY  "your-web-api-key"        // Project Settings → Web API Key
   #define FIREBASE_DB_URL   "https://your-project-default-rtdb.europe-west1.firebasedatabase.app"
   #define FIREBASE_EMAIL    "esp32@yourproject.com"   // the user you created in step 7 above
   #define FIREBASE_PASSWORD "esp32_password"

   #define USER_UID          "paste-your-uid-here"     // Authentication → Users → your UID
   ```

5. Click **Upload** (→). When done, open **Serial Monitor** at **115200 baud** to see status logs.

### UART commands (Serial Monitor or Messages page)

| Command | Action |
|---|---|
| `A2` | Turn GPIO 2 ON |
| `S2` | Turn GPIO 2 OFF |
| `A18` | Turn GPIO 18 ON |
| `S18` | Turn GPIO 18 OFF |

The last 10 commands are saved to EEPROM and printed on every boot.

---

## Firebase Realtime Database structure

All data lives under `/users/{uid}/` so multiple users never see each other's data.

```json
{
  "users": {
    "{uid}": {
      "sensors": {
        "temperature": 24.5,
        "humidity":    58.0,
        "flood":       false,
        "motion":      true,
        "light":       72,
        "lastUpdated": 1748476800
      },
      "leds": {
        "led_1": { "pin": 2,  "name": "Desk Light", "state": false },
        "led_2": { "pin": 18, "name": "Hall Light",  "state": true  }
      },
      "messages": {
        "-NxABC": { "text": "A2", "timestamp": 1748476700, "source": "web" }
      },
      "pendingMessage": {
        "text": "A2",
        "timestamp": 1748476800
      },
      "floodEvents": {
        "-NxDEF": { "timestamp": 1748470000, "sensorValue": 312, "acknowledged": false }
      },
      "temperatureHistory": {
        "-NxGHI": { "value": 24.1, "timestamp": 1748476500 }
      },
      "settings": {
        "alertEmail": "your@email.com"
      }
    }
  }
}
```

---

## Quick-start checklist (fresh machine)

```
[ ] Install Node.js v18+       https://nodejs.org
[ ] Install Arduino IDE 2.x    https://arduino.cc/en/software
[ ] Create Firebase project    https://console.firebase.google.com
[ ] Enable Google Auth         Firebase Console → Authentication → Google
[ ] Create Realtime Database   Firebase Console → Realtime Database
[ ] Set DB rules               (see Part 1 step 4 above)
[ ] Add Web app + copy config  (for frontend .env)
[ ] Generate service account   (for backend .env)
[ ] Create ESP32 email user    (for firmware credentials)
[ ] cd backend  && cp .env.example .env  (fill in values)  && npm install && npm run dev
[ ] cd frontend && cp .env.example .env  (fill in values)  && npm install && npm run dev
[ ] Open http://localhost:5173 and sign in with Google
[ ] Open Arduino IDE, install ESP32 board + Firebase + DHT libraries
[ ] Fill firmware credentials, flash ESP32
```

---

## Key design decisions

| Decision | Rationale |
|---|---|
| Per-user data isolation (`/users/{uid}/`) | Multiple users on one Firebase project, zero data leakage |
| ESP32 polls `/leds` every 2 s | Simpler than streams; tolerates WiFi reconnects |
| Rising-edge flood detection | Prevents duplicate events/emails during prolonged wetness |
| `pendingMessage` pattern | Decouples browser → ESP32 without a persistent socket |
| EEPROM circular buffers | Last 10 UART commands + 10 flood events survive power cycles |
| GPIO whitelist in firmware & UI | Prevents unsafe pin assignments |
| DHT22 module (not bare sensor) | Built-in pull-up resistor; no extra wiring needed |
| Water level sensor analog read | Gives actual water level value, not just dry/wet boolean |
