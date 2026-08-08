<template>
  <CreateCalibration />
</template>

<script setup lang="ts">
/**
 * Combos de calibración: la combinación cámara + microscopio + objetivo y la
 * calibración óptica (píxeles por paso) que se mide para cada una.
 *
 * Es la calibración que estima cuánto se desplaza la IMAGEN por cada paso del motor.
 * La que mide cuánto avanza la PLATINA en milímetros vive en la pestaña «Paso del
 * motor»: son magnitudes distintas y se miden con instrumentos distintos.
 */
import CreateCalibration from '~/components/Configuration/CreateCalibration.vue'
</script>
