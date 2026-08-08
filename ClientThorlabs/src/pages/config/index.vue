<script setup lang="ts">
// /config no tiene contenido propio: es solo el contenedor de la sección, así que
// entra directo a la primera pestaña en lugar de mostrar una pantalla vacía.
await navigateTo('/config/equipos', { replace: true })
</script>

<template>
  <div />
</template>
