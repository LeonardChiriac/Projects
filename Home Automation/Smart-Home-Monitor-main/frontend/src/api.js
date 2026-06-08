import { auth } from './firebase';

const BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3001';

/**
 * Wrapper around fetch that automatically attaches the Firebase ID token
 * as an Authorization header for every backend API call.
 */
export async function apiFetch(path, options = {}) {
  const token = await auth.currentUser?.getIdToken();
  const headers = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };
  return fetch(`${BASE}${path}`, { ...options, headers });
}
