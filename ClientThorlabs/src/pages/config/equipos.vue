<template>
  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
    <CardConfigMicroscope />
    <cardConfigCamera />
    <CardConfigIncrease />
  </div>
</template>

<script setup lang="ts">
/**
 * Alta y edición de los equipos del laboratorio.
 *
 * Los tres van juntos porque son el catálogo del que se compone un combo de
 * calibración: sin cámara, microscopio y objetivo dados de alta no hay nada que
 * combinar. Antes estaban plegados dentro de un acordeón en la página de
 * configuración; aquí se muestran en paralelo, que es como se consultan.
 */
import CardConfigIncrease from '~/components/Configuration/cardConfigIncrease.vue'
import cardConfigCamera from '~/components/Configuration/cardConfigCamera.vue'
import CardConfigMicroscope from '~/components/Configuration/cardConfigMicroscope.vue'
</script>
