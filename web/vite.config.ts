import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The API URL can be overridden with API_URL=... npm run dev
const apiUrl = process.env.API_URL ?? 'http://localhost:5193'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    // Same-origin requests from the browser: no CORS and no hard-coded API host in the client code.
    proxy: {
      '/api': { target: apiUrl, changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
