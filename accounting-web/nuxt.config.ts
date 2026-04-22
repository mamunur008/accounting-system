import tailwindcss from "@tailwindcss/vite";

export default defineNuxtConfig({
  compatibilityDate: "2025-07-15",
  devtools: { enabled: true },
  css: ['./app/assets/css/main.css'],
  vite: {
    plugins: [
      tailwindcss(),
    ],
     server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5263',
          changeOrigin: true
        }
      }
    }
  },

  nitro: {
    devProxy: {
      '/api': {
        target: 'http://localhost:5263/api',
        changeOrigin: true
      }
    }
  },

  routeRules: {
    '/api/**': { proxy: 'http://localhost:5263/api/**' }
  },
});