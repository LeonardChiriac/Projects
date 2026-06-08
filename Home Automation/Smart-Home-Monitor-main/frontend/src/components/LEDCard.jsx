import React from 'react';

/**
 * A single LED card with name, GPIO pin, ON/OFF toggle and delete button.
 *
 * @param {{ id: string, led: {name,pin,state}, onToggle, onDelete }} props
 */
export default function LEDCard({ id, led, onToggle, onDelete }) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5 flex flex-col gap-4">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-white font-semibold leading-tight">{led.name}</h3>
          <p className="text-gray-500 text-sm mt-0.5">GPIO {led.pin}</p>
        </div>

        {/* Glow dot: yellow when ON, dim gray when OFF */}
        <div
          className={`w-3 h-3 rounded-full mt-1 transition-all duration-300 ${
            led.state
              ? 'bg-yellow-400 shadow-[0_0_10px_3px_rgba(250,204,21,0.5)]'
              : 'bg-gray-600'
          }`}
        />
      </div>

      {/* Toggle + Delete */}
      <div className="flex items-center justify-between">
        {/* Toggle switch */}
        <label className="flex items-center gap-2.5 cursor-pointer select-none">
          <button
            type="button"
            role="switch"
            aria-checked={led.state}
            onClick={() => onToggle(id, !led.state)}
            className={`relative w-12 h-6 rounded-full transition-colors focus:outline-none
                        focus-visible:ring-2 focus-visible:ring-blue-500 ${
                          led.state ? 'bg-blue-600' : 'bg-gray-600'
                        }`}
          >
            <span
              className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow
                          transition-transform duration-200 ${
                            led.state ? 'translate-x-6' : 'translate-x-0'
                          }`}
            />
          </button>
          <span className="text-sm text-gray-400">{led.state ? 'ON' : 'OFF'}</span>
        </label>

        {/* Delete */}
        <button
          onClick={() => onDelete(id)}
          aria-label={`Delete ${led.name}`}
          className="text-gray-600 hover:text-red-400 transition-colors p-1 rounded hover:bg-red-900/20"
        >
          <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6"/>
          </svg>
        </button>
      </div>
    </div>
  );
}
