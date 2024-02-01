// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  ssr: false,
  devtools: { enabled: true },
  runtimeConfig: {
    public: {
      apiUrl: process.env.API_URL
    }
  },
  srcDir: 'src',
  modules: ['nuxt-icon', '@nuxtjs/tailwindcss']
})
