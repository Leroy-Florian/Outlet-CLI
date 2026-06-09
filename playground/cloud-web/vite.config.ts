import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// The Outlet Cloud login UI. In dev it proxies /api to the Cloud Web host
// (stripping the /api prefix so /api/auth/login -> {host}/auth/login).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      // Use the workspace bridge from source (no separate lib build step).
      '@outlet/effect-react': new URL('../../packages/outlet-effect-react/src/index.ts', import.meta.url).pathname,
    },
  },
  server: {
    port: 5273,
    proxy: {
      '/api': {
        target: 'http://localhost:5280',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
