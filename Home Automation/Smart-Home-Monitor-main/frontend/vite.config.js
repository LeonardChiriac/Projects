import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Required for Google Sign-In popup to work in development.
    // Without this header the browser blocks the OAuth popup from
    // communicating back to the opener window.
    headers: {
      'Cross-Origin-Opener-Policy': 'unsafe-none',
    },
  },
});
