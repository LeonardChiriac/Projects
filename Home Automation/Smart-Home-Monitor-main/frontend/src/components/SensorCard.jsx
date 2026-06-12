import React from 'react';

const COLOR_MAP = {
  blue:   { card: 'border-gray-800',  value: 'text-blue-400',   bar: 'bg-blue-500' },
  yellow: { card: 'border-gray-800',  value: 'text-yellow-400', bar: 'bg-yellow-400' },
  green:  { card: 'border-gray-800',  value: 'text-emerald-400',bar: 'bg-emerald-500' },
  orange: { card: 'border-orange-800/60', value: 'text-orange-400', bar: 'bg-orange-400' },
  red:    { card: 'border-red-800/60',    value: 'text-red-400',    bar: 'bg-red-500' },
};

/** Small SVG icons — no emojis */
const ICONS = {
  temperature: (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M14 14.76V3.5a2.5 2.5 0 0 0-5 0v11.26a4.5 4.5 0 1 0 5 0z"/>
    </svg>
  ),
  light: (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>
    </svg>
  ),
  motion: (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M13 2a1 1 0 1 1 2 0 1 1 0 0 1-2 0M6 8l4-1 2 3 3 1M8 21l2-6 3 2 3-5"/>
    </svg>
  ),
  flood: (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M3 12c1.5-3 3-4.5 4.5-4.5S10.5 9 12 9s3-1.5 4.5-1.5S19.5 9 21 12"/><path d="M3 18c1.5-3 3-4.5 4.5-4.5S10.5 15 12 15s3-1.5 4.5-1.5S19.5 15 21 18"/>
    </svg>
  ),
  humidity: (
    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M12 2C6 9 4 13 4 16a8 8 0 0 0 16 0c0-3-2-7-8-14z"/>
    </svg>
  ),
};

/**
 * @param {{ title: string, value: string|number, unit?: string,
 *            iconKey: string, color?: string, progress?: number,
 *            subtitle?: string }} props
 */
export default function SensorCard({ title, value, unit, iconKey, color = 'blue', progress, subtitle }) {
  const c = COLOR_MAP[color] || COLOR_MAP.blue;

  return (
    <div className={`bg-gray-900 rounded-xl border ${c.card} p-5 flex flex-col gap-3`}>
      <div className="flex items-center justify-between">
        <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">{title}</p>
        <span className={`${c.value} opacity-70`}>{ICONS[iconKey]}</span>
      </div>

      <div>
        <p className={`text-2xl font-semibold ${c.value} leading-tight`}>
          {value}
          {unit && <span className="text-sm font-normal text-gray-500 ml-1">{unit}</span>}
        </p>
        {subtitle && <p className="text-xs text-gray-500 mt-0.5">{subtitle}</p>}
      </div>

      {progress !== undefined && (
        <div className="h-1 bg-gray-800 rounded-full overflow-hidden">
          <div
            className={`h-full ${c.bar} rounded-full transition-all duration-700`}
            style={{ width: `${progress}%` }}
          />
        </div>
      )}
    </div>
  );
}
