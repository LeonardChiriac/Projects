import React, { useEffect, useState } from 'react';
import { ref, onValue } from 'firebase/database';
import { auth, db } from '../firebase';
import { apiFetch } from '../api';
import toast from 'react-hot-toast';
import LEDCard     from '../components/LEDCard';
import AddLEDModal from '../components/AddLEDModal';

export default function LEDControl() {
  const [leds,      setLeds]      = useState({});
  const [loading,   setLoading]   = useState(true);
  const [showModal, setShowModal] = useState(false);

  const uid = auth.currentUser?.uid;

  // Live LED list – updates whenever Firebase /users/{uid}/leds changes
  useEffect(() => {
    if (!uid) return;
    const unsub = onValue(ref(db, `users/${uid}/leds`), (snap) => {
      setLeds(snap.val() || {});
      setLoading(false);
    });
    return unsub;
  }, [uid]);

  async function handleToggle(id, newState) {
    try {
      const res = await apiFetch(`/api/leds/${id}/state`, {
        method:  'PATCH',
        body:    JSON.stringify({ state: newState }),
      });
      if (!res.ok) throw new Error((await res.json()).error);
      toast.success(`LED ${newState ? 'turned on' : 'turned off'}.`);
    } catch (err) {
      toast.error(err.message || 'Failed to toggle LED.');
    }
  }

  async function handleDelete(id) {
    try {
      const res = await apiFetch(`/api/leds/${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error((await res.json()).error);
      toast.success('LED deleted.');
    } catch (err) {
      toast.error(err.message || 'Failed to delete LED.');
    }
  }

  async function handleAddLED(pin, name) {
    const res = await apiFetch('/api/leds', {
      method:  'POST',
      body:    JSON.stringify({ pin, name }),
    });
    if (!res.ok) {
      const body = await res.json();
      throw new Error(body.error);
    }
    toast.success(`"${name}" added!`);
    setShowModal(false);
  }

  const usedPins = Object.values(leds).map((l) => l.pin);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">LED Control Panel</h1>
        <button
          onClick={() => setShowModal(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg font-medium transition-colors"
        >
          + Add LED
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-32">
          <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
        </div>
      ) : Object.keys(leds).length === 0 ? (
        <div className="text-center text-gray-500 py-20 bg-gray-900 border border-gray-800 rounded-2xl">
          <p className="text-5xl mb-4">💡</p>
          <p className="text-lg font-medium text-gray-400">No LEDs configured yet</p>
          <p className="text-sm mt-1">Click "+ Add LED" to get started.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Object.entries(leds).map(([id, led]) => (
            <LEDCard
              key={id}
              id={id}
              led={led}
              onToggle={handleToggle}
              onDelete={handleDelete}
            />
          ))}
        </div>
      )}

      {showModal && (
        <AddLEDModal
          usedPins={usedPins}
          onAdd={handleAddLED}
          onClose={() => setShowModal(false)}
        />
      )}
    </div>
  );
}
