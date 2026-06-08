const express           = require('express');
const { getDB }         = require('../services/firebase');
const { requireAuth }   = require('../middleware/auth');

const router = express.Router();

/**
 * GET /api/flood-events
 * Returns all flood events for the authenticated user.
 */
router.get('/', requireAuth, async (req, res) => {
  try {
    const snap = await getDB()
      .ref(`users/${req.uid}/floodEvents`)
      .orderByChild('timestamp')
      .once('value');
    res.json(snap.val() || {});
  } catch (err) {
    console.error('[GET /flood-events]', err.message);
    res.status(500).json({ error: 'Failed to fetch flood events.' });
  }
});

/**
 * DELETE /api/flood-events/:id
 */
router.delete('/:id', requireAuth, async (req, res) => {
  try {
    const snap = await getDB().ref(`users/${req.uid}/floodEvents/${req.params.id}`).once('value');
    if (!snap.exists()) return res.status(404).json({ error: 'Flood event not found.' });

    await getDB().ref(`users/${req.uid}/floodEvents/${req.params.id}`).remove();
    res.json({ message: 'Flood event deleted.' });
  } catch (err) {
    console.error('[DELETE /flood-events/:id]', err.message);
    res.status(500).json({ error: 'Failed to delete flood event.' });
  }
});

module.exports = router;
