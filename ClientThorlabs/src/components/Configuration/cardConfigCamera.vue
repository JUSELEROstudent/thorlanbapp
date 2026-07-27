<template>
  <CrudCard
    ref="crudRef"
    title="Cámaras"
    subtitle="Cámaras registradas y su driver de captura"
    resource-path="camera"
    id-field="cameraId"
    :fields="fields"
    :extra-keys="['name', 'localIdentifier', 'driverType']"
    @add-mode="onAddMode"
    @created="notifyCameraConfigUpdated"
    @deleted="notifyCameraConfigUpdated"
    @loaded="onLoaded"
  >
    <!-- Campos propios de la cámara: se eligen antes de los genéricos porque
         el nombre se deduce del dispositivo seleccionado. -->
    <template #form-extra="{ record, disabled }">
      <div class="grid grid-cols-1 gap-4 mb-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Dispositivo detectado<span class="text-red-500"> *</span>
          </label>
          <select
            v-model="record.localIdentifier"
            class="select select-bordered w-full"
            :disabled="disabled || isLoadingCameras"
            @change="syncNameFromIdentifier(record)"
          >
            <option value="">
              {{ isLoadingCameras ? 'Buscando cámaras...' : 'Seleccione una cámara conectada' }}
            </option>
            <option
              v-for="camera in filteredAvailableCameras"
              :key="camera.uniqueId"
              :value="camera.uniqueId"
            >
              {{ camera.cameraName }} ({{ formatIdentifier(camera.uniqueId) }})
            </option>
          </select>
          <p class="text-xs text-gray-500 mt-1">
            Solo se listan las cámaras conectadas que todavía no están registradas.
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Nombre</label>
          <input
            :value="record.name"
            type="text"
            class="input input-bordered w-full bg-gray-100"
            placeholder="Se toma del dispositivo seleccionado"
            readonly
            disabled
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Driver<span class="text-red-500"> *</span>
          </label>
          <select v-model="record.driverType" class="select select-bordered w-full" :disabled="disabled">
            <option value="">Seleccione el driver</option>
            <option value="generic">generic — webcam / DirectShow</option>
            <option value="ids_peak_dotnet">ids_peak_dotnet — IDS Peak (GigE / USB3)</option>
            <option value="ids_ueye">ids_ueye — IDS uEye clásica</option>
          </select>
          <p class="text-xs text-gray-500 mt-1">
            Determina cómo se captura y qué parámetros se pueden configurar después.
          </p>
        </div>
      </div>
    </template>

    <template #item-subtitle="{ item }">
      <p class="text-xs text-gray-500 truncate">
        <span class="badge badge-outline badge-xs mr-1">{{ item.driverType || 'generic' }}</span>
        {{ item.features || 'Sin características' }}
      </p>
    </template>
  </CrudCard>
</template>

<script setup lang="ts">
/**
 * Configuración de cámaras.
 *
 * A diferencia de las otras dos tarjetas, esta no es solo una lista de campos:
 * el nombre no se escribe, se deduce del dispositivo físico que el usuario
 * selecciona, y hay que avisar al resto de la aplicación cuando la lista cambia
 * (CameraStreamCapture escucha el evento 'camera-config-updated' para refrescar
 * su selector sin recargar la página).
 */
import { ref, computed, onMounted } from 'vue'
import CrudCard, { type CrudField } from '../ui/CrudCard.vue'

interface AvailableCamera {
  cameraId: number
  cameraName: string
  uniqueId: string
}

const api = useApi()

const crudRef = ref<InstanceType<typeof CrudCard> | null>(null)
const availableCameras = ref<AvailableCamera[]>([])
const registeredCameras = ref<any[]>([])
const isLoadingCameras = ref(false)

const fields: CrudField[] = [
  {
    key: 'features',
    label: 'Características',
    type: 'textarea',
    placeholder: 'Describa las características de la cámara',
    rows: 3,
    required: true,
    help: 'Texto libre descriptivo (por ejemplo: USB3, 5MP, color). No son los parámetros de captura.'
  }
]

// No tiene sentido ofrecer cámaras que ya están registradas.
const filteredAvailableCameras = computed(() => {
  const used = new Set(registeredCameras.value.map((item) => item.localIdentifier))
  return availableCameras.value.filter((camera) => !used.has(camera.uniqueId))
})

const syncNameFromIdentifier = (record: Record<string, any>) => {
  const match = availableCameras.value.find((camera) => camera.uniqueId === record.localIdentifier)
  record.name = match ? match.cameraName : ''
}

const formatIdentifier = (value: string, visibleChars = 8) => {
  if (!value) return ''
  return value.length <= visibleChars ? value : `${value.slice(0, visibleChars)}...`
}

const fetchAvailableCameras = async () => {
  isLoadingCameras.value = true
  try {
    const response = await api.get<AvailableCamera[]>('/Home/cameras')
    availableCameras.value = response || []
  } catch (error) {
    console.error('Error al obtener las cámaras conectadas:', error)
    availableCameras.value = []
  } finally {
    isLoadingCameras.value = false
  }
}

const onAddMode = (active: boolean) => {
  // Se consultan los dispositivos al abrir el formulario, no al montar: así se
  // detecta una cámara que el usuario acaba de conectar.
  if (active) fetchAvailableCameras()
}

const onLoaded = (items: any[]) => {
  registeredCameras.value = items || []
}

const notifyCameraConfigUpdated = () => {
  if (typeof window === 'undefined') return
  window.dispatchEvent(new CustomEvent('camera-config-updated', {
    detail: registeredCameras.value
  }))
}

onMounted(() => {
  fetchAvailableCameras()
})
</script>
