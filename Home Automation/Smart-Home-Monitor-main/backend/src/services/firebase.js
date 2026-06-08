/**
 * Firebase Admin SDK initialisation.
 * Call initFirebase() once at startup, then use getDB() anywhere.
 */
const admin = require('firebase-admin');

let db = null;

function initFirebase() {
  if (admin.apps.length > 0) return admin.apps[0];

  // The private key arrives with literal "\n" – convert to real newlines.
  const privateKey = process.env.FIREBASE_PRIVATE_KEY
    ? process.env.FIREBASE_PRIVATE_KEY.replace(/\\n/g, '\n')
    : undefined;

  admin.initializeApp({
    credential: admin.credential.cert({
      projectId:   process.env.FIREBASE_PROJECT_ID,
      clientEmail: process.env.FIREBASE_CLIENT_EMAIL,
      privateKey,
    }),
    databaseURL: process.env.FIREBASE_DATABASE_URL,
  });

  db = admin.database();
  console.log('[Firebase] Admin SDK initialised.');
  return admin.app();
}

function getDB() {
  if (!db) throw new Error('Firebase not initialised. Call initFirebase() first.');
  return db;
}

module.exports = { initFirebase, getDB };
