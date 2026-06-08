const express           = require('express');
const { getDB }         = require('../services/firebase');
const { requireAuth }   = require('../middleware/auth');

const router = express.Router();

// Must stay in sync with AVAILABLE_PINS in firmware/src/main.cpp
// and AVAILABLE_PINS in the React frontend.
const AVAILABLE_PINS = [2, 4, 5, 18, 19, 21, 22, 23, 12, 13, 14];

/**
 * GET /api/leds
 * Returns all LED entries (id, pin, name, state).
 */
router.get('/', requireAuth, async (req, res) => {
  try {
    const snap = await getDB().ref(`users/${req.uid}/leds`).once('value');
    res.json(snap.val() || {});
  } catch (err) {
    console.error('[GET /leds]', err.message);
    res.status(500).json({ error: 'Failed to fetch LEDs.' });
  }
});

/**
 * POST /api/leds
 * Add a new LED. Body: { pin: number, name: string }
 */
router.post('/', requireAuth, async (req, res) => {
  const { pin, name } = req.body;

  if (!pin || !name || typeof name !== 'string') {
    return res.status(400).json({ error: '`pin` and `name` are required.' });
  }
  if (!AVAILABLE_PINS.includes(Number(pin))) {
    return res.status(400).json({ error: `Pin ${pin} is not in the allowed list.` });
  }

  try {
    // Prevent duplicate pin assignment
    const existing = await getDB().ref(`users/${req.uid}/leds`).once('value');
    const leds     = existing.val() || {};
    for (const led of Object.values(leds)) {
      if (led.pin === Number(pin)) {
        return res.status(409).json({ error: `Pin ${pin} is already assigned to another LED.` });
      }
    }

    const newRef = getDB().ref(`users/${req.uid}/leds`).push();
    await newRef.set({ pin: Number(pin), name: name.trim(), state: false });
    res.status(201).json({ id: newRef.key, pin: Number(pin), name: name.trim(), state: false });
  } catch (err) {
    console.error('[POST /leds]', err.message);
    res.status(500).json({ error: 'Failed to create LED.' });
  }
});

/**
 * DELETE /api/leds/:id
 * Remove a LED entry from Firebase.
 */
router.delete('/:id', requireAuth, async (req, res) => {
  try {
    const snap = await getDB().ref(`users/${req.uid}/leds/${req.params.id}`).once('value');
    if (!snap.exists()) return res.status(404).json({ error: 'LED not found.' });

    await getDB().ref(`users/${req.uid}/leds/${req.params.id}`).remove();
    res.json({ message: 'LED deleted successfully.' });
  } catch (err) {
    console.error('[DELETE /leds/:id]', err.message);
    res.status(500).json({ error: 'Failed to delete LED.' });
  }
});

/**
 * PATCH /api/leds/:id/state
 * Toggle LED state. Body: { state: boolean }
 * The ESP32 polls /leds every 2 s and reacts to the state change automatically.
 */
router.patch('/:id/state', requireAuth, async (req, res) => {
  const { state } = req.body;

  if (typeof state !== 'boolean') {
    return res.status(400).json({ error: '`state` must be a boolean.' });
  }

  try {
    const snap = await getDB().ref(`users/${req.uid}/leds/${req.params.id}`).once('value');
    if (!snap.exists()) return res.status(404).json({ error: 'LED not found.' });

    await getDB().ref(`users/${req.uid}/leds/${req.params.id}/state`).set(state);
    res.json({ id: req.params.id, state });
  } catch (err) {
    console.error('[PATCH /leds/:id/state]', err.message);
    res.status(500).json({ error: 'Failed to update LED state.' });
  }
});

module.exports = router;
