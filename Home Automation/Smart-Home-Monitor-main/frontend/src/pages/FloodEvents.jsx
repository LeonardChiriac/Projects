import React, { useEffect, useState } from 'react';
import { ref, onValue } from 'firebase/database';
import { auth, db } from '../firebase';
import { apiFetch } from '../api';
import toast from 'react-hot-toast';

export default function FloodEvents() {
  const [events,  setEvents]  = useState([]);
  const [loading, setLoading] = useState(true);

  const uid = auth.currentUser?.uid;

  // Live flood events list
  useEffect(() => {
    if (!uid) return;
    const unsub = onValue(ref(db, `users/${uid}/floodEvents`), (snap) => {
      const data = snap.val();
      if (data) {
        const arr = Object.entries(data)
          .map(([id, ev]) => ({ id, ...ev }))
          .sort((a, b) => b.timestamp - a.timestamp);  // newest first
        setEvents(arr);
      } else {
        setEvents([]);
      }
      setLoading(false);
    });
    return unsub;
  }, [uid]);

  async function handleDelete(id) {
    if (!window.confirm('Delete this flood event? This action cannot be undone.')) return;
    try {
      const res = await apiFetch(`/api/flood-events/${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error((await res.json()).error);
      toast.success('Event deleted.');
    } catch (err) {
      toast.error(err.message || 'Failed to delete event.');
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Flood Events Log</h1>
        {events.length > 0 && (
          <span className="text-sm text-gray-400">{events.length} event{events.length !== 1 ? 's' : ''}</span>
        )}
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-32">
          <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : events.length === 0 ? (
        <div className="text-center py-20 bg-gray-900 border border-gray-800 rounded-2xl">
          <p className="text-5xl mb-4">✅</p>
          <p className="text-lg font-medium text-gray-300">No flood events recorded</p>
          <p className="text-sm text-gray-500 mt-1">Your home is safe!</p>
        </div>
      ) : (
        <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 text-gray-400 text-left">
                <th className="px-4 py-3 font-medium">#</th>
                <th className="px-4 py-3 font-medium">Timestamp</th>
                <th className="px-4 py-3 font-medium">Sensor Value</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {events.map((ev, idx) => (
                <tr
                  key={ev.id}
                  className="border-b border-gray-800/50 hover:bg-gray-800/30 transition-colors"
                >
                  <td className="px-4 py-3 text-gray-500 font-mono">{events.length - idx}</td>
                  <td className="px-4 py-3 text-gray-200 font-mono text-xs">
                    {new Date(ev.timestamp * 1000).toLocaleString()}
                  </td>
                  <td className="px-4 py-3">
                    <span className="bg-red-900/40 text-red-400 px-2 py-0.5 rounded-full text-xs font-mono">
                      {ev.sensorValue}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        ev.acknowledged
                          ? 'bg-green-900/40 text-green-400'
                          : 'bg-yellow-900/40 text-yellow-400'
                      }`}
                    >
                      {ev.acknowledged ? 'Acknowledged' : 'Unread'}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => handleDelete(ev.id)}
                      className="text-red-400 hover:text-red-300 hover:bg-red-900/20 px-3 py-1
                                 rounded-lg transition-colors text-xs font-medium"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
