<template>
  <dialog ref="dialogRef" class="modal" @close="handleClose">
    <div class="modal-box max-w-6xl w-11/12 p-0 overflow-hidden">
      <!-- Encabezado -->
      <div class="flex items-center justify-between px-5 py-3 border-b border-gray-200">
        <div class="min-w-0">
          <h3 class="font-semibold text-gray-900 truncate">{{ title }}</h3>
          <p v-if="subtitle" class="text-xs text-gray-500 truncate">{{ subtitle }}</p>
        </div>
        <button class="btn btn-sm btn-ghost" @click="close">✕</button>
      </div>

      <!-- Pestañas cuando hay más de una imagen -->
      <div v-if="images.length > 1" class="flex gap-1 px-5 pt-3">
        <button
          v-for="(image, index) in images"
          :key="index"
          class="btn btn-xs"
          :class="index === activeIndex ? 'btn-primary' : 'btn-outline'"
          @click="selectImage(index)"
        >
          {{ image.label || `Imagen ${index + 1}` }}
        </button>
      </div>

      <!-- Visor -->
      <div
        class="relative mt-3 h-[65vh] bg-black overflow-hidden select-none flex items-center justify-center"
        @wheel.prevent="onWheel"
        @mousedown="onMouseDown"
        @mousemove="onMouseMove"
        @mouseup="onMouseUp"
        @mouseleave="onMouseUp"
        @dblclick="onDoubleClick"
      >
        <div v-if="isLoading" class="absolute inset-0 flex items-center justify-center bg-black/40 z-10">
          <span class="loading loading-spinner loading-lg text-white"></span>
        </div>

        <p v-if="errorMessage" class="text-red-300 text-sm px-6 text-center">{{ errorMessage }}</p>

        <img
          v-else-if="currentSrc"
          :src="currentSrc"
          class="max-h-full max-w-full object-contain"
          :class="isDragging ? 'cursor-grabbing' : (zoom > minZoom ? 'cursor-grab' : 'cursor-default')"
          :style="{
            transform: `translate(${panX}px, ${panY}px) scale(${zoom})`,
            transition: isDragging ? 'none' : 'transform 0.15s ease-out'
          }"
          draggable="false"
          @error="errorMessage = 'No se pudo mostrar la imagen.'"
        />

        <p v-else-if="!isLoading" class="text-gray-400 text-sm">Sin imagen</p>

        <!-- Controles de zoom -->
        <div v-if="currentSrc && !errorMessage" class="absolute bottom-3 right-3 flex items-center gap-1 bg-black/50 rounded p-1">
          <button class="btn btn-xs" :disabled="zoom <= minZoom" @click.stop="zoomOut">−</button>
          <span class="text-white text-xs px-1 w-12 text-center">{{ Math.round(zoom * 100) }}%</span>
          <button class="btn btn-xs" :disabled="zoom >= maxZoom" @click.stop="zoomIn">+</button>
          <button class="btn btn-xs" :disabled="zoom === minZoom" @click.stop="resetZoom">Reset</button>
        </div>
      </div>

      <!-- Datos asociados a la imagen -->
      <div v-if="$slots.details" class="px-5 py-3 border-t border-gray-200 max-h-40 overflow-y-auto">
        <slot name="details" />
      </div>
    </div>

    <form method="dialog" class="modal-backdrop">
      <button>cerrar</button>
    </form>
  </dialog>
</template>

<script setup lang="ts">
/**
 * Visor de imágenes en ventana modal, con zoom y desplazamiento.
 *
 * La lógica de zoom/pan estaba escrita directamente dentro de stitching.vue y no
 * se podía reutilizar; aquí queda extraída para que la use también la vista de
 * calibraciones. Además es el primer componente modal del proyecto: hasta ahora
 * todo se resolvía con bloques v-if en línea o con confirm() del navegador.
 *
 * Las imágenes se reciben ya resueltas (URL o blob URL): quién las descarga y con
 * qué autenticación es responsabilidad de quien usa el componente.
 */
import { ref, computed, watch } from 'vue'

export interface ViewerImage {
  src: string
  label?: string
}

const props = withDefaults(defineProps<{
  images: ViewerImage[]
  title?: string
  subtitle?: string
  isLoading?: boolean
}>(), {
  title: 'Imagen',
  isLoading: false
})

const dialogRef = ref<HTMLDialogElement | null>(null)
const activeIndex = ref(0)
const errorMessage = ref<string | null>(null)

const currentSrc = computed(() => props.images[activeIndex.value]?.src || '')

// ── Zoom y desplazamiento ────────────────────────────────────────
const minZoom = 1
const maxZoom = 5
const zoom = ref(minZoom)
const panX = ref(0)
const panY = ref(0)
const isDragging = ref(false)

let dragStartX = 0
let dragStartY = 0
let panStartX = 0
let panStartY = 0

const clampZoom = (value: number) => Math.min(maxZoom, Math.max(minZoom, value))

const resetZoom = () => {
  zoom.value = minZoom
  panX.value = 0
  panY.value = 0
}

const zoomIn = () => { zoom.value = clampZoom(zoom.value + 0.5) }

const zoomOut = () => {
  zoom.value = clampZoom(zoom.value - 0.5)
  // Al volver al 100% se recentra: si no, la imagen queda desplazada fuera de
  // la vista sin forma evidente de recuperarla.
  if (zoom.value === minZoom) resetZoom()
}

const onWheel = (event: WheelEvent) => {
  const delta = event.deltaY > 0 ? -0.25 : 0.25
  zoom.value = clampZoom(zoom.value + delta)
  if (zoom.value === minZoom) resetZoom()
}

const onDoubleClick = () => {
  if (zoom.value > minZoom) resetZoom()
  else zoom.value = 2
}

const onMouseDown = (event: MouseEvent) => {
  if (zoom.value <= minZoom) return
  isDragging.value = true
  dragStartX = event.clientX
  dragStartY = event.clientY
  panStartX = panX.value
  panStartY = panY.value
}

const onMouseMove = (event: MouseEvent) => {
  if (!isDragging.value) return
  panX.value = panStartX + (event.clientX - dragStartX)
  panY.value = panStartY + (event.clientY - dragStartY)
}

const onMouseUp = () => { isDragging.value = false }

// ── Control del modal ────────────────────────────────────────────
const selectImage = (index: number) => {
  activeIndex.value = index
  errorMessage.value = null
  resetZoom()
}

const open = (index = 0) => {
  activeIndex.value = index
  errorMessage.value = null
  resetZoom()
  dialogRef.value?.showModal()
}

const close = () => dialogRef.value?.close()

const handleClose = () => resetZoom()

// Si cambia el juego de imágenes con el modal abierto, se vuelve a la primera.
watch(() => props.images, () => {
  if (activeIndex.value >= props.images.length) selectImage(0)
})

defineExpose({ open, close })
</script>
