import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  css: {
    preprocessorOptions: {
      scss: {
        // Bootstrap 5.3 still uses the legacy Sass colour API; these warnings come
        // from the library, not from our own stylesheets.
        silenceDeprecations: [
          'color-functions',
          'global-builtin',
          'if-function',
          'import',
        ],
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'https://localhost:7001', changeOrigin: true, secure: false },
      '/connect': { target: 'https://localhost:7001', changeOrigin: true, secure: false },
    },
  },
})
