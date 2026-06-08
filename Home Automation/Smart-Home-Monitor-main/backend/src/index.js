require('dotenv').config();

const express = require('express');
const cors    = require('cors');

const { initFirebase }       = require('./services/firebase');
const { startFloodListener } = require('./listeners/floodListener');

const sensorsRouter     = require('./routes/sensors');
const ledsRouter        = require('./routes/leds');
const messagesRouter    = require('./routes/messages');
const floodEventsRouter = require('./routes/floodEvents');

// ── Validate required environment variables before anything else ─────────────
const REQUIRED_ENV = [
  'FIREBASE_PROJECT_ID',
  'FIREBASE_CLIENT_EMAIL',
  'FIREBASE_PRIVATE_KEY',
  'FIREBASE_DATABASE_URL',
  'GMAIL_USER',
  'GMAIL_APP_PASSWORD',
];
const missing = REQUIRED_ENV.filter((k) => !process.env[k]);
if (missing.length > 0) {
  console.error('[Startup] Missing required environment variables:', missing.join(', '));
  console.error('[Startup] Copy .env to .env and fill in all values.');
  process.exit(1);
}

// ── Initialise Firebase & start flood listener ───────────────────────────────
initFirebase();
startFloodListener();

// ── Express App ──────────────────────────────────────────────────────────────
const app = express();

// Parse allowed origins from env (comma-separated)
const allowedOrigins = (process.env.CORS_ORIGINS || 'http://localhost:5173')
  .split(',')
  .map((o) => o.trim());

app.use(
  cors({
    origin: (origin, callback) => {
      // Allow requests with no Origin header (e.g. curl, Postman, mobile apps)
      if (!origin || allowedOrigins.includes(origin)) return callback(null, true);
      callback(new Error(`CORS: origin "${origin}" is not allowed.`));
    },
    methods: ['GET', 'POST', 'PATCH', 'DELETE'],
  })
);

app.use(express.json());

// ── Routes ───────────────────────────────────────────────────────────────────
app.use('/api/sensors',      sensorsRouter);
app.use('/api/leds',         ledsRouter);
app.use('/api/messages',     messagesRouter);
app.use('/api/flood-events', floodEventsRouter);

// Health-check (useful for Railway / Render uptime monitors)
app.get('/health', (_, res) => res.json({ status: 'ok', ts: Date.now() }));

// 404 handler
app.use((_, res) => res.status(404).json({ error: 'Route not found.' }));

// Global error handler
// eslint-disable-next-line no-unused-vars
app.use((err, req, res, _next) => {
  console.error('[Express]', err.message);
  res.status(500).json({ error: err.message || 'Internal server error.' });
});

// ── Start listening ──────────────────────────────────────────────────────────
const PORT = Number(process.env.PORT) || 3001;
app.listen(PORT, () => console.log(`[Server] Listening on port ${PORT}`));
