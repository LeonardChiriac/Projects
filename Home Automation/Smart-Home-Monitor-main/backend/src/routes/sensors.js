const express           = require('express');
const { getDB }         = require('../services/firebase');
const { requireAuth }   = require('../middleware/auth');

const router = express.Router();

/**
 * GET /api/sensors
 * Returns the latest sensor readings for the authenticated user.
 */
router.get('/', requireAuth, async (req, res) => {
  try {
    const snap = await getDB().ref(`users/${req.uid}/sensors`).once('value');
    if (!snap.exists()) {
      return res.status(404).json({ error: 'No sensor data available yet.' });
    }
    res.json(snap.val());
  } catch (err) {
    console.error('[GET /sensors]', err.message);
    res.status(500).json({ error: 'Failed to fetch sensor data.' });
  }
});

module.exports = router;
