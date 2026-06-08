import React from 'react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';

function fmt(ts) {
  return new Date(ts * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function CustomTooltip({ active, payload }) {
  if (!active || !payload?.length) return null;
  const { value, timestamp } = payload[0].payload;
  return (
    <div className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm shadow-lg">
      <p className="text-gray-400 text-xs mb-0.5">{fmt(timestamp)}</p>
      <p className="text-blue-400 font-bold">{value.toFixed(1)} °C</p>
    </div>
  );
}

/**
 * Area chart showing temperature history.
 * @param {{ data: Array<{value: number, timestamp: number}> }} props
 */
export default function TemperatureChart({ data }) {
  if (!data || data.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 text-gray-500 text-sm">
        No temperature history yet.
      </div>
    );
  }

  const values  = data.map((d) => d.value);
  const minVal  = Math.min(...values);
  const maxVal  = Math.max(...values);
  // Add 2° padding on each side so the line never hugs the edge
  const domainMin = Math.floor(minVal - 2);
  const domainMax = Math.ceil(maxVal  + 2);

  return (
    <ResponsiveContainer width="100%" height={220}>
      <AreaChart data={data} margin={{ top: 5, right: 10, left: -15, bottom: 0 }}>
        <defs>
          <linearGradient id="tempGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%"  stopColor="#3b82f6" stopOpacity={0.35} />
            <stop offset="95%" stopColor="#3b82f6" stopOpacity={0}    />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
        <XAxis
          dataKey="timestamp"
          tickFormatter={fmt}
          tick={{ fill: '#6b7280', fontSize: 11 }}
          axisLine={false}
          tickLine={false}
          minTickGap={40}
        />
        <YAxis
          domain={[domainMin, domainMax]}
          tick={{ fill: '#6b7280', fontSize: 11 }}
          axisLine={false}
          tickLine={false}
          unit="°"
        />
        <Tooltip content={<CustomTooltip />} />
        {/* baseValue must match the axis floor so the filled area renders
            correctly for both positive and negative temperatures */}
        <Area
          type="monotone"
          dataKey="value"
          stroke="#3b82f6"
          strokeWidth={2}
          fill="url(#tempGrad)"
          dot={false}
          activeDot={{ r: 4, fill: '#3b82f6', strokeWidth: 0 }}
          baseValue={domainMin}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
