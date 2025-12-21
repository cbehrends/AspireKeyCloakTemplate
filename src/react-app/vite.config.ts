import { defineConfig } from 'vite'
import { devtools } from '@tanstack/devtools-vite'
import { tanstackStart } from '@tanstack/react-start/plugin/vite'
import viteReact from '@vitejs/plugin-react'
import viteTsConfigPaths from 'vite-tsconfig-paths'
import tailwindcss from '@tailwindcss/vite'

const config = defineConfig({
  plugins: [
    devtools(),
    // this is the plugin that enables path aliases
    viteTsConfigPaths({
      projects: ['./tsconfig.json'],
    }),
    tailwindcss(),
    tanstackStart(),
    viteReact(),
  ],
  server: {
    port: process.env.PORT ? Number.parseInt(process.env.PORT) : 3000,
    proxy: {
      '/bff': {
        target: process.env.BFF_URL || 'https://localhost:6001',
        changeOrigin: true,
        secure: false,
      },
      '/api': {
        target: process.env.BFF_URL || 'https://localhost:6001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

export default config
