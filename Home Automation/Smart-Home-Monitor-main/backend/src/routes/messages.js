const express           = require('express');
const { getDB }         = require('../services/firebase');
const { requireAuth }   = require('../middleware/auth');

const router = express.Router();

/**
 * GET /api/messages
 * Returns the last 10 UART messages for the authenticated user.
 */
router.get('/', requireAuth, async (req, res) => {
  try {
    const snap = await getDB()
      .ref(`users/${req.uid}/messages`)
      .orderByChild('timestamp')
      .limitToLast(10)
      .once('value');
    res.json(snap.val() || {});
  } catch (err) {
    console.error('[GET /messages]', err.message);
    res.status(500).json({ error: 'Failed to fetch messages.' });
  }
});

/**
 * POST /api/messages
 * Send a command/message to the ESP32 via Firebase /pendingMessage.
 * Body: { text: string }  (max 32 chars – EEPROM buffer limit)
 *
 * The ESP32 polls /pendingMessage every 3 s, writes it to EEPROM, executes
 * it as a UART command, and mirrors it back to /messages for history display.
 */
router.post('/', requireAuth, async (req, res) => {
  const { text } = req.body;

  if (!text || typeof text !== 'string' || text.trim().length === 0) {
    return res.status(400).json({ error: '`text` is required.' });
  }

  // Truncate to EEPROM limit
  const sanitized = text.trim().substring(0, 32);
  const ts        = Math.floor(Date.now() / 1000);

  try {
    // Write to pendingMessage so the ESP32 picks it up on its next poll
    await getDB().ref(`users/${req.uid}/pendingMessage`).set({ text: sanitized, timestamp: ts });

    // Also push to messages history so it appears in the chat immediately.
    // On real hardware the ESP32 mirrors the command back here itself;
    // we do it from the backend so the UI works without hardware too.
    await getDB().ref(`users/${req.uid}/messages`).push({ text: sanitized, timestamp: ts, source: 'web' });

    res.status(201).json({ message: 'Message queued for ESP32.', text: sanitized, timestamp: ts });
  } catch (err) {
    console.error('[POST /messages]', err.message);
    res.status(500).json({ error: 'Failed to send message.' });
  }
});

module.exports = router;
