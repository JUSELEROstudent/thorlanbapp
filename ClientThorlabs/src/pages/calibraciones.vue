<template>
  <div class="w-full p-4 space-y-4">
    <!-- Encabezado -->
    <div class="flex items-center justify-between gap-4 px-4 py-3 bg-white border border-gray-200 rounded-lg shadow-sm">
      <div>
        <h1 class="text-xl font-semibold text-gray-900">Registros de calibración</h1>
        <p class="text-sm text-gray-500">
          Mediciones agrupadas por el montaje óptico con el que se tomaron.
        </p>
      </div>
      <div class="flex items-center gap-3">
        <label class="flex items-center gap-2 text-sm text-gray-600">
          <input v-model="onlyWithMeasurements" type="checkbox" class="checkbox checkbox-sm" />
          Solo con mediciones
        </label>
        <button class="btn btn-outline btn-sm" :disabled="isLoading" @click="fetchGroups">
          <span v-if="isLoading" class="loading loading-spinner loading-xs"></span>
          Actualizar
        </button>
      </div>
    </div>

    <!-- Buscador -->
    <div class="px-4">
      <input
        v-model="search"
        type="text"
        class="input input-bordered input-sm w-full max-w-md"
        placeholder="Filtrar por cámara, microscopio u objetivo..."
      />
    </div>

    <!-- Estados -->
    <div v-if="isLoading" class="px-4 space-y-2">
      <div class="skeleton h-16 w-full"></div>
      <div class="skeleton h-16 w-full"></div>
      <div class="skeleton h-16 w-full"></div>
    </div>

    <div v-else-if="filteredGroups.length === 0" class="px-4">
      <div class="text-center p-10 bg-white border border-gray-200 rounded-lg text-gray-500">
        <p class="font-medium">No hay calibraciones que mostrar</p>
        <p class="text-sm mt-1">
          Las calibraciones se crean desde la vista de Configuración, dentro de un grupo de calibración.
        </p>
      </div>
    </div>

    <!-- Listado maestro-detalle -->
    <div v-else class="px-4 space-y-3">
      <div
        v-for="group in filteredGroups"
        :id="`group-${group.groupCailbrationId}`"
        :key="group.groupCailbrationId"
        class="bg-white border rounded-lg shadow-sm overflow-hidden transition-colors"
        :class="highlightedGroupId === group.groupCailbrationId ? 'border-primary ring-2 ring-primary' : 'border-gray-200'"
      >
        <!-- Fila del combo -->
        <button
          class="w-full flex items-center gap-4 px-5 py-4 hover:bg-gray-50 transition-colors text-left"
          @click="toggleGroup(group.groupCailbrationId)"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
            fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
            class="text-gray-400 transition-transform shrink-0"
            :class="isExpanded(group.groupCailbrationId) ? 'rotate-90' : ''"
          >
            <polyline points="9 18 15 12 9 6" />
          </svg>

          <!-- El combo de equipos es lo que identifica la calibración -->
          <div class="flex-1 min-w-0 grid grid-cols-1 md:grid-cols-3 gap-2">
            <div class="min-w-0">
              <p class="text-xs text-gray-400 uppercase tracking-wide">Cámara</p>
              <p class="text-sm font-medium text-gray-900 truncate">{{ group.camera?.name || '—' }}</p>
            </div>
            <div class="min-w-0">
              <p class="text-xs text-gray-400 uppercase tracking-wide">Microscopio</p>
              <p class="text-sm font-medium text-gray-900 truncate">{{ group.microscope?.name || '—' }}</p>
            </div>
            <div class="min-w-0">
              <p class="text-xs text-gray-400 uppercase tracking-wide">Objetivo</p>
              <p class="text-sm font-medium text-gray-900 truncate">
                {{ group.increase?.name || '—' }}
                <span v-if="group.increase?.value" class="text-gray-500">({{ group.increase.value }})</span>
              </p>
            </div>
          </div>

          <div class="text-right shrink-0">
            <span class="badge badge-outline">
              {{ (group.picsCalibrations || []).length }} medición(es)
            </span>
            <p class="text-xs text-gray-400 mt-1">{{ formatDate(group.date) }}</p>
          </div>
        </button>

        <!-- Detalle: las mediciones -->
        <div v-if="isExpanded(group.groupCailbrationId)" class="border-t border-gray-200 bg-gray-50 px-5 py-4">
          <div v-if="(group.picsCalibrations || []).length === 0" class="text-sm text-gray-500 py-2">
            Este grupo todavía no tiene mediciones de calibración.
          </div>

          <div v-else class="overflow-x-auto">
            <table class="table table-sm bg-white rounded">
              <thead>
                <tr>
                  <th>Eje</th>
                  <th class="text-right">Pasos</th>
                  <th class="text-right">dx</th>
                  <th class="text-right">dy</th>
                  <th class="text-right">Confianza</th>
                  <th>Unidad</th>
                  <th class="text-right">Movimiento</th>
                  <th class="text-center">Aceptada</th>
                  <th class="text-center">Imágenes</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="pic in sortedPics(group)" :key="pic.picsCalibrationId" class="hover">
                  <td class="font-medium uppercase">
                    {{ pic.axisMovementName || pic.axeDirectionCalibration || '—' }}
                  </td>
                  <td class="text-right">{{ pic.numberOfSteps ?? pic.movementValue ?? '—' }}</td>
                  <td class="text-right">{{ formatNumber(pic.dx) }}</td>
                  <td class="text-right">{{ formatNumber(pic.dy) }}</td>
                  <td class="text-right">
                    <span :class="confidenceClass(pic.confidence)">{{ formatNumber(pic.confidence) }}</span>
                  </td>
                  <td>{{ pic.measureUnit || '—' }}</td>
                  <td class="text-right">{{ pic.movementValue || '—' }}</td>
                  <td class="text-center">
                    <span v-if="Number(pic.acepted) === 1" class="badge badge-success badge-xs">Sí</span>
                    <span v-else class="badge badge-ghost badge-xs">No</span>
                  </td>
                  <td class="text-center">
                    <button
                      class="btn btn-xs btn-outline"
                      :disabled="!pic.pic1 && !pic.pic2"
                      @click="openImages(group, pic)"
                    >
                      Ver
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- Visor de imágenes -->
    <ImageViewerModal
      ref="viewerRef"
      :images="viewerImages"
      :is-loading="isLoadingImages"
      :title="viewerTitle"
      :subtitle="viewerSubtitle"
    >
      <template #details>
        <div v-if="selectedPic" class="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
          <div>
            <p class="text-xs text-gray-400">Eje</p>
            <p class="font-medium uppercase">
              {{ selectedPic.axisMovementName || selectedPic.axeDirectionCalibration || '—' }}
            </p>
          </div>
          <div>
            <p class="text-xs text-gray-400">Pasos</p>
            <p class="font-medium">{{ selectedPic.numberOfSteps ?? '—' }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-400">Desplazamiento (dx, dy)</p>
            <p class="font-medium">{{ formatNumber(selectedPic.dx) }}, {{ formatNumber(selectedPic.dy) }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-400">Confianza</p>
            <p class="font-medium">{{ formatNumber(selectedPic.confidence) }}</p>
          </div>
        </div>
      </template>
    </ImageViewerModal>
  </div>
</template>

<script setup lang="ts">
/**
 * Vista de registros de calibración.
 *
 * Cada calibración solo tiene sentido junto al montaje óptico con el que se tomó
 * (cámara + microscopio + objetivo): la relación píxeles/paso cambia con el
 * aumento, así que una medición sin su combo no dice nada. Por eso la vista es
 * maestro-detalle, con el combo siempre visible en la fila principal.
 *
 * Las imágenes no se muestran en línea sino en un visor bajo demanda: lo que se
 * consulta habitualmente son los datos extraídos (dx, dy, confianza), y cargar
 * dos imágenes por cada medición haría la lista muy pesada.
 */
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { alertsClient } from '../stores/alerts'
import ImageViewerModal, { type ViewerImage } from '../components/ui/ImageViewerModal.vue'

const alertStore = alertsClient()
const api = useApi()
const route = useRoute()

const groups = ref<any[]>([])
const isLoading = ref(false)
const expanded = ref<Set<string>>(new Set())
const search = ref('')
const onlyWithMeasurements = ref(false)

// Cuando se llega desde el botón "Detalle" de Group Calibration (?group=id),
// se abre y resalta ese grupo en concreto en vez de dejar que el usuario lo
// busque en la lista completa.
const highlightedGroupId = ref<string | null>(null)

const viewerRef = ref<InstanceType<typeof ImageViewerModal> | null>(null)
const viewerImages = ref<ViewerImage[]>([])
const viewerTitle = ref('Imágenes de calibración')
const viewerSubtitle = ref('')
const isLoadingImages = ref(false)
const selectedPic = ref<any>(null)

// URLs de blob creadas para el visor; hay que liberarlas para no filtrar memoria.
let objectUrls: string[] = []

const releaseObjectUrls = () => {
  for (const url of objectUrls) URL.revokeObjectURL(url)
  objectUrls = []
}

const fetchGroups = async () => {
  isLoading.value = true
  try {
    groups.value = (await api.get<any[]>('/api/GroupCalibration/with-details')) || []
  } catch (error: any) {
    console.error('Error al cargar las calibraciones:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: error?.data || 'No se pudieron cargar las calibraciones'
    })
  } finally {
    isLoading.value = false
  }
}

const filteredGroups = computed(() => {
  let result = groups.value

  if (onlyWithMeasurements.value) {
    result = result.filter((group) => (group.picsCalibrations || []).length > 0)
  }

  const term = search.value.trim().toLowerCase()
  if (term) {
    result = result.filter((group) => {
      const haystack = [
        group.camera?.name,
        group.microscope?.name,
        group.increase?.name,
        group.aditionalInfo
      ].filter(Boolean).join(' ').toLowerCase()
      return haystack.includes(term)
    })
  }

  return result
})

const isExpanded = (id: string) => expanded.value.has(id)

const toggleGroup = (id: string) => {
  // Se reasigna el Set para que Vue detecte el cambio.
  const next = new Set(expanded.value)
  next.has(id) ? next.delete(id) : next.add(id)
  expanded.value = next
}

// Se ordenan por número de pasos: así la progresión de la calibración se lee de
// corrido, en vez de en el orden en que quedaron guardadas.
const sortedPics = (group: any) =>
  [...(group.picsCalibrations || [])].sort((a, b) => {
    const stepA = Number(a.numberOfSteps ?? a.movementValue ?? 0)
    const stepB = Number(b.numberOfSteps ?? b.movementValue ?? 0)
    return stepA - stepB
  })

const formatNumber = (value: any) => {
  if (value === null || value === undefined || value === '') return '—'
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed.toFixed(3) : String(value)
}

const confidenceClass = (value: any) => {
  const parsed = Number(value)
  if (!Number.isFinite(parsed)) return ''
  // La correlación de fase devuelve una confianza entre 0 y 1; por debajo de 0,3
  // la medición suele ser ruido.
  if (parsed >= 0.6) return 'text-green-600 font-medium'
  if (parsed >= 0.3) return 'text-amber-600'
  return 'text-red-600'
}

const formatDate = (value: string) => {
  if (!value) return ''
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString()
}

const loadImage = async (filePath: string, label: string): Promise<ViewerImage | null> => {
  try {
    const response = await api.raw(`/api/PicsCalibration/image?filePath=${encodeURIComponent(filePath)}`)
    if (!response.ok) return null
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    objectUrls.push(url)
    return { src: url, label }
  } catch (error) {
    console.error('Error al cargar la imagen de calibración:', error)
    return null
  }
}

const openImages = async (group: any, pic: any) => {
  releaseObjectUrls()
  viewerImages.value = []
  selectedPic.value = pic
  viewerTitle.value = `Calibración ${(pic.axisMovementName || pic.axeDirectionCalibration || '').toUpperCase()} — ${pic.numberOfSteps ?? ''} pasos`
  viewerSubtitle.value = [group.camera?.name, group.microscope?.name, group.increase?.name]
    .filter(Boolean).join(' · ')

  isLoadingImages.value = true
  viewerRef.value?.open(0)

  try {
    const loaded: ViewerImage[] = []
    if (pic.pic1) {
      const image = await loadImage(pic.pic1, 'Posición inicial')
      if (image) loaded.push(image)
    }
    if (pic.pic2) {
      const image = await loadImage(pic.pic2, 'Tras el movimiento')
      if (image) loaded.push(image)
    }

    viewerImages.value = loaded

    if (loaded.length === 0) {
      alertStore.NewAlert({
        type: 'warning',
        tittle: 'Sin imágenes',
        data: 'No se pudieron cargar las imágenes de esta medición.'
      })
    }
  } finally {
    isLoadingImages.value = false
  }
}

onMounted(async () => {
  await fetchGroups()

  const targetGroupId = typeof route.query.group === 'string' ? route.query.group : null
  if (targetGroupId && groups.value.some((group) => group.groupCailbrationId === targetGroupId)) {
    highlightedGroupId.value = targetGroupId
    toggleGroup(targetGroupId)
    await nextTick()
    document.getElementById(`group-${targetGroupId}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  }
})

onBeforeUnmount(() => {
  releaseObjectUrls()
})
</script>
