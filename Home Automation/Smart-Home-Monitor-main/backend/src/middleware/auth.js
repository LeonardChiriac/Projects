const admin = require('firebase-admin');

/**
 * Express middleware that verifies the Firebase ID token sent by the frontend.
 * Sets req.uid to the authenticated user's UID on success.
 * Returns 401 if the token is missing or invalid.
 */
async function requireAuth(req, res, next) {
  const header = req.headers.authorization;
  if (!header || !header.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'Missing Authorization header.' });
  }

  const token = header.slice(7);
  try {
    const decoded = await admin.auth().verifyIdToken(token);
    req.uid = decoded.uid;
    next();
  } catch (err) {
    return res.status(401).json({ error: 'Invalid or expired token.' });
  }
}

module.exports = { requireAuth };
