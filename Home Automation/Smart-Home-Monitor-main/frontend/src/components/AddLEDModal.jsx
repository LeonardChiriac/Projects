import React, { useState } from 'react';
import toast from 'react-hot-toast';

// Must stay in sync with AVAILABLE_PINS in firmware and backend
const ALL_PINS = [2, 4, 5, 18, 19, 21, 22, 23, 13, 12, 14];

/**
 * Modal dialog for adding a new LED.
 *
 * @param {{ usedPins: number[], onAdd: (pin,name)=>Promise, onClose: ()=>void }} props
 */
export default function AddLEDModal({ usedPins, onAdd, onClose }) {
  const [name,    setName]    = useState('');
  const [pin,     setPin]     = useState('');
  const [loading, setLoading] = useState(false);

  const availablePins = ALL_PINS.filter((p) => !usedPins.includes(p));

  async function handleSubmit(e) {
    e.preventDefault();
    if (!name.trim() || !pin) return;
    setLoading(true);
    try {
      await onAdd(Number(pin), name.trim());
    } catch (err) {
      toast.error(err.message || 'Failed to add LED.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 px-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="bg-gray-900 border border-gray-800 rounded-2xl w-full max-w-sm p-6 shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-white">Add New LED</h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-white text-2xl leading-none w-8 h-8 flex items-center justify-center rounded-lg hover:bg-gray-800 transition-colors"
          >
            ×
          </button>
        </div>

        {availablePins.length === 0 ? (
          <p className="text-yellow-400 text-sm text-center py-6">
            All available GPIO pins are already in use.
          </p>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Name */}
            <div>
              <label className="block text-sm text-gray-400 mb-1.5">Name</label>
              <input
                type="text"
                required
                maxLength={40}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Kitchen Light"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-2.5 text-white
                           placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            {/* Pin selector */}
            <div>
              <label className="block text-sm text-gray-400 mb-1.5">GPIO Pin</label>
              <select
                required
                value={pin}
                onChange={(e) => setPin(e.target.value)}
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-2.5 text-white
                           focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">Select a pin…</option>
                {availablePins.map((p) => (
                  <option key={p} value={p}>
                    GPIO {p}
                  </option>
                ))}
              </select>
            </div>

            {/* Buttons */}
            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 bg-gray-800 hover:bg-gray-700 text-gray-300 py-2.5 rounded-lg
                           text-sm font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={loading}
                className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white
                           py-2.5 rounded-lg text-sm font-medium transition-colors"
              >
                {loading ? 'Adding…' : 'Add LED'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
