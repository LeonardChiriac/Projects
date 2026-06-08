import React, { useState, useRef, useEffect } from 'react';
import { ref, set, push, onValue } from 'firebase/database';
import { auth, db } from '../firebase';
import toast from 'react-hot-toast';

/**
 * ESP32 Simulator Page
 * Mimics everything the physical ESP32 firmware does:
 *  - Push sensor data to /sensors  (temperature, humidity, flood, motion, light)
 *  - Append temperature history to /temperatureHistory
 *  - Trigger a flood event to /floodEvents  (backend picks this up and sends email)
 *  - Send a UART message to /messages  (source = "serial")
 */

export default function Simulator() {
  // ── Sensor controls ────────────────────────────────────────────────────────
  const [temperature, setTemperature] = useState(24.5);
  const [humidity,    setHumidity]    = useState(55);
  const [light,       setLight]       = useState(65);
  const [flood,       setFlood]       = useState(false);
  const [motion,      setMotion]      = useState(false);

  // ── UART message ───────────────────────────────────────────────────────────
  const [uartText, setUartText] = useState('A2');

  // ── Auto-push interval ─────────────────────────────────────────────────────
  const [autoRunning, setAutoRunning] = useState(false);
  const intervalRef = useRef(null);

  // ── Live LEDs from Firebase (same path the ESP32 polls) ───────────────────
  const [leds, setLeds] = useState({}); // { key: { name, state, pin } }

  const uid = auth.currentUser?.uid;

  useEffect(() => {
    if (!uid) return;
    const ledsRef = ref(db, `users/${uid}/leds`);
    const unsub = onValue(ledsRef, (snap) => {
      setLeds(snap.val() ?? {});
    });
    return () => unsub();
  }, [uid]);

  // ── Toggle an LED's state (as if the ESP32 acted on a command) ───────────
  async function toggleLED(key, currentState) {
    try {
      await set(ref(db, `users/${uid}/leds/${key}/state`), !currentState);
    } catch (err) {
      toast.error('Toggle failed: ' + err.message);
    }
  }

  // ── ESP32 firmware constants ────────────────────────────────────────────────
  const TEMP_HIGH_THRESHOLD = 35;   // °C — firmware would alert above this
  const TEMP_LOW_THRESHOLD  = 5;    // °C — firmware would alert below this

  // ── Helper: push a status message as if the ESP32 sent it via serial ──────
  async function esp32Log(text) {
    await push(ref(db, `users/${uid}/messages`), {
      text,
      timestamp: Math.floor(Date.now() / 1000),
      source:    'esp32',
    });
  }

  // ── Push sensors + emit firmware-style status messages ────────────────────
  async function pushSensors(overrides = {}) {
    const ts = Math.floor(Date.now() / 1000);
    const data = {
      temperature: overrides.temperature ?? temperature,
      humidity:    overrides.humidity    ?? humidity,
      flood:       overrides.flood       ?? flood,
      motion:      overrides.motion      ?? motion,
      light:       overrides.light       ?? light,
      lastUpdated: ts,
    };

    const prevFlood  = overrides._prevFlood  ?? false;
    const prevMotion = overrides._prevMotion ?? false;

    try {
      await set(ref(db, `users/${uid}/sensors`), data);
      await push(ref(db, `users/${uid}/temperatureHistory`), {
        value: data.temperature, timestamp: ts,
      });

      // Emit firmware-style messages for notable state changes / thresholds
      if (data.temperature >= TEMP_HIGH_THRESHOLD) {
        await esp32Log(`TEMP_HIGH: ${data.temperature.toFixed(1)}°C`);
      } else if (data.temperature <= TEMP_LOW_THRESHOLD) {
        await esp32Log(`TEMP_LOW: ${data.temperature.toFixed(1)}°C`);
      }
      if (data.flood && !prevFlood)  await esp32Log('FLOOD_DETECTED: water sensor triggered');
      if (data.motion && !prevMotion) await esp32Log('MOTION_DETECTED: PIR sensor triggered');

      toast.success(`Sensors pushed (temp=${data.temperature}°C, hum=${data.humidity}%, flood=${data.flood})`);
    } catch (err) {
      toast.error('Push failed: ' + err.message);
    }
  }

  // ── Trigger a flood event ──────────────────────────────────────────────────
  async function triggerFlood() {
    const ts = Math.floor(Date.now() / 1000);
    try {
      await push(ref(db, `users/${uid}/floodEvents`), {
        timestamp: ts, sensorValue: 1, acknowledged: false,
      });
      await set(ref(db, `users/${uid}/sensors/flood`), true);
      await esp32Log('FLOOD_DETECTED: water sensor triggered');
      toast.success('Flood event triggered! Check Flood Events page & email.');
    } catch (err) {
      toast.error('Failed: ' + err.message);
    }
  }

  // ── Execute a UART command (A<pin> / S<pin>) or send free text ─────────────
  // This mirrors exactly what the ESP32 firmware's handleUARTCommand() does.
  async function sendUARTMessage() {
    const cmd = uartText.trim();
    if (!cmd) return;
    const ts = Math.floor(Date.now() / 1000);

    // Parse A<pin> = Activate, S<pin> = Stop
    const match = cmd.match(/^([AaSs])(\d+)$/);
    if (match) {
      const action  = match[1].toUpperCase(); // 'A' or 'S'
      const pin     = Number(match[2]);
      const newState = action === 'A';

      // Find the LED with this pin number
      const entry = Object.entries(leds).find(([, led]) => led.pin === pin);

      try {
        if (entry) {
          const [key] = entry;
          await set(ref(db, `users/${uid}/leds/${key}/state`), newState);
          // Firmware echoes the command back as a serial message
          await push(ref(db, `users/${uid}/messages`), {
            text:      `${action}${pin}: GPIO${pin} ${newState ? 'ON' : 'OFF'}`,
            timestamp: ts,
            source:    'esp32',
          });
          toast.success(`GPIO ${pin} ${newState ? 'turned ON' : 'turned OFF'}`);
        } else {
          // Pin not registered — firmware would log an error
          await push(ref(db, `users/${uid}/messages`), {
            text:      `ERR: pin ${pin} not found in /leds`,
            timestamp: ts,
            source:    'esp32',
          });
          toast.error(`No LED registered on GPIO ${pin}. Add it in LED Control first.`);
        }
      } catch (err) {
        toast.error('Command failed: ' + err.message);
      }
    } else {
      // Free-text: push as a plain serial message
      try {
        await push(ref(db, `users/${uid}/messages`), {
          text:      cmd.substring(0, 32),
          timestamp: ts,
          source:    'serial',
        });
        toast.success(`Sent: "${cmd}"`);
      } catch (err) {
        toast.error('Failed: ' + err.message);
      }
    }
  }

  // ── Auto-push every 5 s (mimics ESP32 loop) ───────────────────────────────
  function toggleAuto() {
    if (autoRunning) {
      clearInterval(intervalRef.current);
      setAutoRunning(false);
      toast('Auto-push stopped.', { icon: '⏹' });
    } else {
      pushSensors();   // push immediately
      intervalRef.current = setInterval(() => pushSensors(), 5000);
      setAutoRunning(true);
      toast('Auto-push started (every 5 s).', { icon: '▶️' });
    }
  }

  // ── UI ─────────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-6 max-w-2xl">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold text-white">Simulator</h1>
        <p className="text-gray-500 text-sm mt-1">
          Push test data to Firebase without real hardware.
        </p>
      </div>

      {/* Sensor Controls */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 space-y-5">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider">Sensor Values</h2>

        {/* Temperature */}
        <div>
          <div className="flex justify-between mb-2">
            <label className="text-sm text-gray-400">Temperature</label>
            <span className="text-white font-mono text-sm">{temperature.toFixed(1)} °C</span>
          </div>
          <input
            type="range" min="-20" max="60" step="0.5"
            value={temperature}
            onChange={(e) => setTemperature(Number(e.target.value))}
            className="w-full accent-blue-500"
          />
        </div>

        {/* Humidity */}
        <div>
          <div className="flex justify-between mb-2">
            <label className="text-sm text-gray-400">Humidity</label>
            <span className="text-white font-mono text-sm">{humidity} %</span>
          </div>
          <input
            type="range" min="0" max="100" step="1"
            value={humidity}
            onChange={(e) => setHumidity(Number(e.target.value))}
            className="w-full accent-blue-400"
          />
        </div>

        {/* Light */}
        <div>
          <div className="flex justify-between mb-2">
            <label className="text-sm text-gray-400">Ambient Light</label>
            <span className="text-white font-mono text-sm">{light} %</span>
          </div>
          <input
            type="range" min="0" max="100" step="1"
            value={light}
            onChange={(e) => setLight(Number(e.target.value))}
            className="w-full accent-yellow-400"
          />
        </div>

        {/* Flood + Motion toggles */}
        <div className="flex gap-8">
          <label className="flex items-center gap-3 cursor-pointer select-none">
            <button
              type="button"
              onClick={() => setFlood((v) => !v)}
              className={`relative w-11 h-6 rounded-full transition-colors focus:outline-none ${
                flood ? 'bg-red-600' : 'bg-gray-600'
              }`}
            >
              <span className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform duration-200 ${flood ? 'translate-x-5' : ''}`} />
            </button>
            <span className="text-sm text-gray-300">
              Flood sensor — <span className={flood ? 'text-red-400 font-medium' : 'text-gray-500'}>{flood ? 'Wet' : 'Dry'}</span>
            </span>
          </label>

          <label className="flex items-center gap-3 cursor-pointer select-none">
            <button
              type="button"
              onClick={() => setMotion((v) => !v)}
              className={`relative w-11 h-6 rounded-full transition-colors focus:outline-none ${
                motion ? 'bg-orange-500' : 'bg-gray-600'
              }`}
            >
              <span className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform duration-200 ${motion ? 'translate-x-5' : ''}`} />
            </button>
            <span className="text-sm text-gray-300">
              Motion — <span className={motion ? 'text-orange-400 font-medium' : 'text-gray-500'}>{motion ? 'Detected' : 'Clear'}</span>
            </span>
          </label>
        </div>
      </div>

      {/* Push buttons */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <button
          onClick={() => pushSensors()}
          className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2.5 rounded-lg transition-colors"
        >
          Push Sensors Once
        </button>
        <button
          onClick={toggleAuto}
          className={`font-medium py-2.5 rounded-lg transition-colors ${
            autoRunning
              ? 'bg-emerald-700 hover:bg-emerald-800 text-white'
              : 'bg-gray-800 hover:bg-gray-700 text-gray-200'
          }`}
        >
          {autoRunning ? 'Stop Auto-Push' : 'Auto-Push every 5 s'}
        </button>
      </div>

      {/* Flood event trigger */}
      <div className="bg-gray-900 border border-red-900/40 rounded-xl p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-1">Flood Event</h2>
        <p className="text-gray-400 text-sm mb-4">
          Creates a new entry in <code className="bg-gray-800 px-1 rounded text-xs">/floodEvents</code> exactly
          like the ESP32 would. The backend will detect it and send an email alert.
        </p>
        <button
          onClick={triggerFlood}
          className="bg-red-700 hover:bg-red-800 text-white font-medium px-5 py-2.5 rounded-lg transition-colors"
        >
          Trigger Flood Event
        </button>
      </div>

      {/* Live LED board */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-1">LED Board</h2>
        <p className="text-gray-400 text-sm mb-4">
          Live view of all LEDs. Click any to toggle — mirrors what the ESP32 does when it polls{' '}
          <code className="bg-gray-800 px-1 rounded text-xs">/leds</code>.
        </p>

        {Object.keys(leds).length === 0 ? (
          <p className="text-gray-600 text-sm">
            No LEDs added yet. Go to the LED Control page and add one.
          </p>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {Object.entries(leds).map(([key, led]) => {
              const isOn = !!led.state;
              return (
                <button
                  key={key}
                  onClick={() => toggleLED(key, isOn)}
                  className={`flex flex-col items-center justify-center gap-2 p-4 rounded-xl border-2 transition-all select-none
                    ${isOn
                      ? 'bg-yellow-400/10 border-yellow-500/60 text-yellow-300'
                      : 'bg-gray-800 border-gray-700 text-gray-400 hover:border-gray-600'
                    }`}
                >
                  {/* Circle indicator instead of emoji */}
                  <div className={`w-5 h-5 rounded-full transition-all duration-300 ${
                    isOn
                      ? 'bg-yellow-400 shadow-[0_0_12px_4px_rgba(250,204,21,0.4)]'
                      : 'bg-gray-600'
                  }`} />
                  <span className="font-medium text-sm">GPIO {led.pin ?? key}</span>
                  {led.name && (
                    <span className="text-xs text-gray-500 truncate max-w-full px-1">{led.name}</span>
                  )}
                  <span className={`text-xs font-semibold tracking-wide ${isOn ? 'text-yellow-400' : 'text-gray-600'}`}>
                    {isOn ? 'ON' : 'OFF'}
                  </span>
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* UART message */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-1">Send Serial Command</h2>
        <p className="text-gray-400 text-sm mb-4">
          Writes to <code className="bg-gray-800 px-1 rounded text-xs">/messages</code> as if sent from the ESP32 serial monitor.
        </p>
        <div className="flex gap-3">
          <input
            value={uartText}
            onChange={(e) => setUartText(e.target.value)}
            maxLength={32}
            placeholder='e.g. A2 or S18'
            className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-2.5 text-white
                       font-mono placeholder-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
          />
          <button
            onClick={sendUARTMessage}
            disabled={!uartText.trim()}
            className="bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white
                       px-5 py-2.5 rounded-lg font-medium transition-colors whitespace-nowrap text-sm"
          >
            Send
          </button>
        </div>
        <p className="text-gray-600 text-xs mt-2">
          Quick:&nbsp;
          {['A2','S2','A4','S4','A18','S18'].map((cmd) => (
            <button
              key={cmd}
              onClick={() => setUartText(cmd)}
              className="bg-gray-800 hover:bg-gray-700 text-gray-400 px-2 py-0.5 rounded mr-1 font-mono text-xs transition-colors"
            >
              {cmd}
            </button>
          ))}
        </p>
      </div>

      {/* Reference */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-3">What each action tests</h2>
        <ul className="space-y-1.5 text-sm text-gray-400">
          <li><span className="text-gray-200">Push Sensors Once</span> — updates Dashboard cards immediately</li>
          <li><span className="text-gray-200">Auto-Push</span> — runs every 5 s like real firmware; watch the temperature chart fill</li>
          <li><span className="text-gray-200">Trigger Flood Event</span> — shows banner on Dashboard, adds row to Flood Events, triggers email</li>
          <li><span className="text-gray-200">Send Serial Command</span> — adds message to Messages page with "serial" label</li>
        </ul>
      </div>
    </div>
  );
}
