import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  base: './',
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    target: 'es2022',
    sourcemap: false,
    rollupOptions: { input: { manor: 'index.html', lobby: 'lobby.html', empire: 'empire.html', blackjack: 'blackjack.html', reels: 'reels.html' } },
  },
  server: { host: '127.0.0.1', port: 5173, strictPort: true },
});
