/**
 * Flood event listener – watches /users for new user nodes, then attaches a
 * per-user child_added listener on /users/{uid}/floodEvents.
 *
 * On startup it also does a one-time scan of all existing user nodes so that
 * users who registered before this backend process started are covered
 * immediately (the child_added on /users fires for existing children too, but
 * only when Firebase delivers the initial sync – the explicit scan ensures we
 * don't miss anyone during a slow connection).
 */
const { getDB }          = require('../services/firebase');
const { sendFloodAlert } = require('../services/mailer');

const COOLDOWN_MS = 5 * 60 * 1000;  // 5 minutes
const cooldowns   = {};              // uid → last email timestamp ms
const watched     = new Set();      // uids we are already listening to

function startFloodListener() {
  const db = getDB();

  // child_added fires for all existing children immediately on attach, plus
  // any new children added later.
  db.ref('users').on('child_added', (snap) => {
    const uid = snap.key;
    if (!uid || watched.has(uid)) return;
    watched.add(uid);
    console.log(`[FloodListener] New user node detected: uid=${uid}`);
    attachUserListener(uid, db);
  }, (err) => {
    console.error('[FloodListener] Error watching /users:', err.message);
  });

  console.log('[FloodListener] Watching /users for accounts.');
}

function attachUserListener(uid, db) {
  const floodRef      = db.ref(`users/${uid}/floodEvents`);
  const listenerStart = Date.now();

  floodRef.on('child_added', async (snapshot) => {
    const event = snapshot.val();
    if (!event) return;

    // Ignore events that already existed before this server process started
    const eventMs = (event.timestamp || 0) * 1000;
    if (eventMs < listenerStart - 10_000) return;

    console.log(`[FloodListener] New flood event for uid=${uid}, ts=${event.timestamp}`);

    const now = Date.now();
    if (now - (cooldowns[uid] || 0) < COOLDOWN_MS) {
      console.log(`[FloodListener] Cooldown active for uid=${uid} – email suppressed.`);
      return;
    }

    cooldowns[uid] = now;

    try {
      // Read the alert email that the frontend saved when the user logged in
      let alertEmail = process.env.ALERT_EMAIL || null;
      const emailSnap = await db.ref(`users/${uid}/settings/alertEmail`).once('value');
      if (emailSnap.val()) {
        alertEmail = emailSnap.val();
      }

      console.log(`[FloodListener] Alert email for uid=${uid}: ${alertEmail || '(not set)'}`);

      if (!alertEmail) {
        console.warn(`[FloodListener] No alert email for uid=${uid} – skipping email.`);
        return;
      }

      await sendFloodAlert({ ...event, id: snapshot.key }, alertEmail);
      console.log(`[FloodListener] Email sent to ${alertEmail} for uid=${uid}`);
    } catch (err) {
      console.error(`[FloodListener] Failed to send email for uid=${uid}:`, err.message);
    }
  }, (err) => {
    console.error(`[FloodListener] Firebase error for uid=${uid}:`, err.message);
  });

  console.log(`[FloodListener] Listening on /users/${uid}/floodEvents`);
}

module.exports = { startFloodListener };
