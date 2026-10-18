import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In development the React app runs on http://localhost:5173 and the API on https://localhost:7130.
// The proxy forwards every request that starts with /api to the API, so the browser sees
// one origin and no CORS settings are needed on the server.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7130',
        secure: false // the API uses the local development certificate
      }
    }
  }
});
