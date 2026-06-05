import { defineConfig } from 'vite'
import dts from 'vite-plugin-dts'
import { resolve } from 'node:path'

export default defineConfig(({ mode }) => {
  if (mode === 'lib') {
    return {
      plugins: [dts({ include: ['src'], exclude: ['src/**/*.test.ts'] })],
      build: {
        lib: {
          entry: resolve(import.meta.dirname, 'src/index.ts'),
          name: 'OutletHateoas',
          formats: ['es'],
          fileName: 'index',
        },
        rollupOptions: {
          external: [/^effect(\/|$)/, /^@effect\//],
        },
      },
    }
  }

  return {}
})
