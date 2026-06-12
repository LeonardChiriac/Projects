import { initializeApp }  from 'firebase/app';
import { getAuth }        from 'firebase/auth';
import { getDatabase }    from 'firebase/database';
import { getAnalytics, isSupported } from 'firebase/analytics';

const firebaseConfig = {
  apiKey:            import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain:        import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  // databaseURL is NOT shown in the Firebase web-app config snippet.
  // You must copy it manually from: Firebase Console → Realtime Database
  databaseURL:       import.meta.env.VITE_FIREBASE_DATABASE_URL,
  projectId:         import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket:     import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId:             import.meta.env.VITE_FIREBASE_APP_ID,
  measurementId:     import.meta.env.VITE_FIREBASE_MEASUREMENT_ID,
};

const app = initializeApp(firebaseConfig);

// Analytics only runs in real browsers (not SSR / Node test environments)
isSupported().then((yes) => yes && getAnalytics(app));

export const auth = getAuth(app);
export const db   = getDatabase(app);
