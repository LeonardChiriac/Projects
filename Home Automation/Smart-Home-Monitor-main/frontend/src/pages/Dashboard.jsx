import React, { useEffect, useState } from 'react';
import { ref, onValue, get, set, remove } from 'firebase/database';
import { auth, db } from '../firebase';
import SensorCard       from '../components/SensorCard';
import TemperatureChart from '../components/TemperatureChart';

export default function Dashboard() {
  const [sensors,      setSensors]     = useState(null);
  const [tempHistory,  setTempHistory] = useState([]);
  const [histVersion,  setHistVersion] = useState(0);
  const [loading,      setLoading]     = useState(true);

  const uid = auth.currentUser?.uid;

  // Live sensor data – updates every time ESP32 pushes (every 5 s)
  useEffect(() => {
    if (!uid) return;
    const unsub = onValue(ref(db, `users/${uid}/sensors`), (snap) => {
      setSensors(snap.val());
      setLoading(false);
    });
    return unsub;
  }, [uid]);

  // Trim to 100 entries on mount – sorted by push key (insertion order), no timestamp filter
  useEffect(() => {
    if (!uid) return;
    const histRef = ref(db, `users/${uid}/temperatureHistory`);
    get(histRef).then((snap) => {
      if (!snap.exists()) return;
      const keys = Object.keys(snap.val()).sort(); // push keys sort chronologically
      if (keys.length > 100) {
        keys.slice(0, keys.length - 100).forEach((key) =>
          remove(ref(db, `users/${uid}/temperatureHistory/${key}`))
        );
      }
    });
  }, [uid]);

  // Live temperature history – sorted by Firebase push key (always chronological)
  useEffect(() => {
    if (!uid) return;
    const unsub = onValue(ref(db, `users/${uid}/temperatureHistory`), (snap) => {
      const data = snap.val();
      if (!data) return;
      // Firebase push keys sort lexicographically in insertion order.
      // We use the key timestamp as fallback when the stored timestamp is invalid.
      const PUSH_ALPHABET = '-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz';
      const keyToTs = (k) => {
        let t = 0;
        for (let i = 0; i < 8; i++) t = t * 64 + PUSH_ALPHABET.indexOf(k[i]);
        return Math.floor(t / 1000);
      };
      const arr = Object.entries(data)
        .sort(([a], [b]) => (a < b ? -1 : 1))          // chronological by push key
        .map(([key, val]) => ({
          value: val.value,
          timestamp: val.timestamp >= 1_000_000_000 ? val.timestamp : keyToTs(key),
        }));
      setTempHistory(arr.slice(-20));
      setHistVersion((v) => v + 1);
    });
    return unsub;
  }, [uid]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-white">Dashboard</h1>

      {/* Flood Alert Banner */}
      {sensors?.flood && (
        <div className="bg-red-950 border border-red-700 text-white px-5 py-4 rounded-xl flex items-center gap-4">
          <div className="shrink-0 w-8 h-8 rounded-full bg-red-600 flex items-center justify-center">
            <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M3 12c1.5-3 3-4.5 4.5-4.5S10.5 9 12 9s3-1.5 4.5-1.5S19.5 9 21 12"/>
              <path d="M3 18c1.5-3 3-4.5 4.5-4.5S10.5 15 12 15s3-1.5 4.5-1.5S19.5 15 21 18"/>
            </svg>
          </div>
          <div className="flex-1">
            <p className="font-semibold leading-tight">Flood Detected</p>
            <p className="text-sm text-red-300 mt-0.5">Water sensor is active. Check your property.</p>
          </div>
          <button
            onClick={() => set(ref(db, `users/${uid}/sensors/flood`), false).catch(() => {})}
            className="shrink-0 text-red-300 hover:text-white text-sm px-3 py-1.5 rounded-lg
                       border border-red-700 hover:bg-red-800 transition-colors"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Sensor Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
        <SensorCard
          title="Temperature"
          value={sensors?.temperature != null && sensors.temperature !== -99 ? sensors.temperature.toFixed(1) : '—'}
          unit="°C"
          iconKey="temperature"
          color="blue"
        />
        <SensorCard
          title="Humidity"
          value={sensors?.humidity != null ? sensors.humidity.toFixed(0) : '—'}
          unit="%"
          iconKey="humidity"
          color="blue"
          progress={sensors?.humidity != null ? Math.min(sensors.humidity, 100) : 0}
        />
        <SensorCard
          title="Ambient Light"
          value={sensors?.light ?? '—'}
          unit="%"
          iconKey="light"
          color="yellow"
          progress={sensors?.light != null ? Math.min(sensors.light, 100) : 0}
        />
        <SensorCard
          title="Motion"
          value={sensors?.motion ? 'Detected' : 'Clear'}
          iconKey="motion"
          color={sensors?.motion ? 'orange' : 'green'}
        />
        <SensorCard
          title="Flood Sensor"
          value={sensors?.flood ? 'Triggered' : 'Normal'}
          iconKey="flood"
          color={sensors?.flood ? 'red' : 'green'}
        />
      </div>

      {/* Temperature History Chart */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4">Temperature History</h2>
        <TemperatureChart key={histVersion} data={tempHistory} />
      </div>

      {sensors?.lastUpdated && (
        <p className="text-gray-600 text-xs">
          Last updated: {new Date(sensors.lastUpdated * 1000).toLocaleString()}
        </p>
      )}
    </div>
  );
}
