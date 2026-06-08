import React, { useEffect, useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { onAuthStateChanged } from 'firebase/auth';
import { ref, set } from 'firebase/database';
import { auth, db } from './firebase';
import Navbar      from './components/Navbar';
import Dashboard   from './pages/Dashboard';
import LEDControl  from './pages/LEDControl';
import Messages    from './pages/Messages';
import FloodEvents from './pages/FloodEvents';
import Login       from './pages/Login';
import Simulator   from './pages/Simulator';

/** Spinner shown while Firebase resolves the initial auth state. */
function LoadingScreen() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-950">
      <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
    </div>
  );
}

/** Redirect unauthenticated users to /login; show spinner while loading. */
function ProtectedRoute({ user, children }) {
  if (user === undefined) return <LoadingScreen />;
  return user ? children : <Navigate to="/login" replace />;
}

export default function App() {
  // undefined = loading, null = unauthenticated, object = authenticated
  const [user, setUser] = useState(undefined);

  useEffect(() => {
    const unsub = onAuthStateChanged(auth, (u) => {
      setUser(u ?? null);
      // Persist the Google account email under the user's own node so the
      // backend flood listener can send alerts to the right address.
      if (u?.email) {
        set(ref(db, `users/${u.uid}/settings/alertEmail`), u.email).catch(() => {});
      }
    });
    return unsub;
  }, []);

  return (
    <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <Routes>
        {/* Public route */}
        <Route
          path="/login"
          element={user ? <Navigate to="/" replace /> : <Login />}
        />

        {/* Protected routes */}
        <Route
          path="/*"
          element={
            <ProtectedRoute user={user}>
              <div className="min-h-screen bg-gray-950 text-gray-100">
                <Navbar user={user} />
                <main className="max-w-7xl mx-auto px-4 py-6">
                  <Routes>
                    <Route path="/"             element={<Dashboard />} />
                    <Route path="/leds"         element={<LEDControl />} />
                    <Route path="/messages"     element={<Messages />} />
                    <Route path="/flood-events" element={<FloodEvents />} />
                    <Route path="/simulator"    element={<Simulator />} />
                    <Route path="*"             element={<Navigate to="/" replace />} />
                  </Routes>
                </main>
              </div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
