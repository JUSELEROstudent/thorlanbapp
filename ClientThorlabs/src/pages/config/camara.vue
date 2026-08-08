<template>
  <div class="grid grid-cols-1 xl:grid-cols-2 gap-4 items-start">
    <CameraStreamCapture />
    <CameraParameters />
  </div>
</template>

<script setup lang="ts">
/**
 * Vista en vivo y parámetros de captura de la cámara.
 *
 * Van en la misma pantalla y ya no en un acordeón porque se usan a la vez: se ajusta
 * la exposición o el enfoque mirando el resultado en el streaming, y el umbral de
 * nitidez se calibra con la muestra ya enfocada delante.
 */
import CameraStreamCapture from '~/components/Configuration/CameraStreamCapture.vue'
import CameraParameters from '~/components/Configuration/CameraParameters.vue'
</script>
