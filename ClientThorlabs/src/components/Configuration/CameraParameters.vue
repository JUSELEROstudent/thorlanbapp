<template>
  <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
    <!-- Encabezado -->
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center gap-3">
      <div class="min-w-0">
        <h3 class="text-lg font-semibold text-gray-900">Parámetros de la cámara</h3>
        <p class="text-xs text-gray-500">
          Se aplican al streaming, a la calibración automática y a los recorridos.
        </p>
      </div>
      <div class="flex gap-2 shrink-0">
        <button
          class="btn btn-outline btn-sm"
          :disabled="!selectedCameraId || isLoading"
          @click="loadParameters"
        >
          <span v-if="isLoading" class="loading loading-spinner loading-xs"></span>
          Releer
        </button>
        <button
          class="btn btn-success btn-sm"
          :disabled="!canSave"
          @click="saveParameters"
        >
          <span v-if="isSaving" class="loading loading-spinner loading-xs"></span>
          {{ isSaving ? 'Guardando...' : 'Guardar' }}
        </button>
      </div>
    </div>

    <div class="p-6 space-y-4">
      <!-- Selección de cámara -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Cámara</label>
        <select v-model="selectedCameraId" class="select select-bordered w-full" @change="loadParameters">
          <option value="">Seleccione una cámara registrada</option>
          <option v-for="camera in cameraList" :key="camera.cameraId" :value="camera.cameraId">
            {{ camera.name }} ({{ camera.driverType || 'generic' }})
          </option>
        </select>
        <p class="text-xs text-gray-500 mt-1">
          Solo se pueden configurar cámaras ya registradas. Consultar el dispositivo puede
          tardar unos segundos porque hay que abrirlo.
        </p>
      </div>

      <div v-if="isLoading" class="space-y-2">
        <div class="skeleton h-8 w-full"></div>
        <div class="skeleton h-8 w-full"></div>
        <div class="skeleton h-8 w-3/4"></div>
      </div>

      <div v-else-if="!selectedCameraId" class="text-center p-6 text-gray-500 text-sm">
        Seleccione una cámara para ver sus parámetros.
      </div>

      <template v-else>
        <!-- Aviso del backend (driver sin soporte o dispositivo no disponible) -->
        <div v-if="response?.message" class="alert alert-warning py-2 text-sm">
          <span>{{ response.message }}</span>
        </div>

        <div v-if="descriptors.length === 0 && !response?.message" class="text-center p-6 text-gray-500 text-sm">
          Este dispositivo no expone parámetros configurables.
        </div>

        <!-- Formulario generado a partir de los descriptores del backend -->
        <div v-for="group in groupedDescriptors" :key="group.name" class="border border-gray-200 rounded-lg">
          <div class="px-4 py-2 bg-gray-50 border-b border-gray-200 rounded-t-lg">
            <h4 class="text-sm font-semibold text-gray-800">{{ group.name }}</h4>
          </div>

          <div class="p-4 space-y-4">
            <div v-for="descriptor in group.items" :key="descriptor.name">
              <div class="flex items-start justify-between gap-4">
                <div class="min-w-0 flex-1">
                  <label class="block text-sm font-medium text-gray-700">{{ descriptor.label }}</label>
                  <p v-if="descriptor.description" class="text-xs text-gray-500 mt-0.5">
                    {{ descriptor.description }}
                  </p>
                  <p v-if="descriptor.current !== null && descriptor.current !== undefined" class="text-xs text-gray-400 mt-0.5">
                    Valor actual en el dispositivo: {{ descriptor.current }}{{ descriptor.unit ? ' ' + descriptor.unit : '' }}
                  </p>
                </div>

                <div class="shrink-0 w-56">
                  <!-- Booleano -->
                  <input
                    v-if="descriptor.type === 'bool'"
                    type="checkbox"
                    class="toggle toggle-primary"
                    :checked="isTruthy(values[descriptor.name])"
                    @change="setValue(descriptor.name, ($event.target as HTMLInputElement).checked ? 'true' : 'false')"
                  />

                  <!-- Enumerado -->
                  <select
                    v-else-if="descriptor.type === 'enum'"
                    class="select select-bordered select-sm w-full"
                    :value="values[descriptor.name] || ''"
                    @change="setValue(descriptor.name, ($event.target as HTMLSelectElement).value)"
                  >
                    <option value="">(sin definir)</option>
                    <option v-for="option in descriptor.options || []" :key="option" :value="option">
                      {{ option }}
                    </option>
                  </select>

                  <!-- Numérico -->
                  <div v-else class="flex items-center gap-2">
                    <input
                      v-if="hasRange(descriptor)"
                      type="range"
                      class="range range-xs range-primary flex-1"
                      :min="descriptor.min ?? undefined"
                      :max="descriptor.max ?? undefined"
                      :step="descriptor.step || 'any'"
                      :value="values[descriptor.name] || descriptor.min || 0"
                      @input="setValue(descriptor.name, ($event.target as HTMLInputElement).value)"
                    />
                    <input
                      type="number"
                      class="input input-bordered input-sm w-24"
                      :min="descriptor.min ?? undefined"
                      :max="descriptor.max ?? undefined"
                      :step="descriptor.step || 'any'"
                      :value="values[descriptor.name] ?? ''"
                      :placeholder="descriptor.current || ''"
                      @input="setValue(descriptor.name, ($event.target as HTMLInputElement).value)"
                    />
                    <span v-if="descriptor.unit" class="text-xs text-gray-500 w-8">{{ descriptor.unit }}</span>
                  </div>

                  <p v-if="hasRange(descriptor)" class="text-xs text-gray-400 mt-1 text-right">
                    {{ descriptor.min }} – {{ descriptor.max }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Umbral de enfoque -->
        <div v-if="response?.supportsParameters" class="border border-gray-200 rounded-lg">
          <div class="px-4 py-2 bg-gray-50 border-b border-gray-200 rounded-t-lg">
            <h4 class="text-sm font-semibold text-gray-800">Enfoque</h4>
          </div>
          <div class="p-4">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1">
                <label class="block text-sm font-medium text-gray-700">Umbral de nitidez</label>
                <p class="text-xs text-gray-500 mt-0.5">
                  Por debajo de este valor se advierte antes de lanzar una calibración. La escala
                  depende del montaje óptico: obsérvela en el streaming con la muestra bien
                  enfocada y anote un valor algo menor al que vea.
                </p>
              </div>
              <input
                v-model="focusThreshold"
                type="number"
                step="any"
                class="input input-bordered input-sm w-32 shrink-0"
                placeholder="sin definir"
              />
            </div>
          </div>
        </div>

        <div v-if="descriptors.length > 0" class="flex justify-end">
          <button class="btn btn-ghost btn-xs" @click="clearAll">
            Restaurar valores por defecto del driver
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Configuración de parámetros de captura de una cámara.
 *
 * El formulario NO está escrito para una cámara concreta: se genera a partir de
 * los descriptores que devuelve el backend (nombre, tipo, rango, unidad), que a
 * su vez los obtiene consultando el dispositivo real. Por eso la misma vista
 * sirve para la cámara genérica y para la uEye, y servirá para la IDS Peak
 * cuando se implemente su proveedor, sin tocar este archivo.
 */
import { ref, computed, onMounted } from 'vue'
import { alertsClient } from '../../stores/alerts'

interface CameraParameterDescriptor {
  name: string
  label: string
  type: 'number' | 'bool' | 'enum'
  min?: number | null
  max?: number | null
  step?: number | null
  unit?: string | null
  options?: string[] | null
  current?: string | null
  group?: string | null
  description?: string | null
}

interface ParametersResponse {
  cameraId: string
  cameraName: string
  driverType: string
  supportsParameters: boolean
  message?: string | null
  descriptors: CameraParameterDescriptor[]
  saved: Record<string, string>
  focusThreshold?: string | null
}

const alertStore = alertsClient()
const api = useApi()

const cameraList = ref<any[]>([])
const selectedCameraId = ref('')
const response = ref<ParametersResponse | null>(null)
const values = ref<Record<string, string>>({})
const focusThreshold = ref<string>('')
const isLoading = ref(false)
const isSaving = ref(false)

const descriptors = computed(() => response.value?.descriptors || [])

const canSave = computed(() =>
  !!selectedCameraId.value && !isSaving.value && !isLoading.value && !!response.value?.supportsParameters
)

const groupedDescriptors = computed(() => {
  const groups = new Map<string, CameraParameterDescriptor[]>()
  for (const descriptor of descriptors.value) {
    const key = descriptor.group || 'General'
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(descriptor)
  }
  return Array.from(groups.entries()).map(([name, items]) => ({ name, items }))
})

const isTruthy = (value: string | undefined) =>
  value === 'true' || value === '1' || value === 'True'

const hasRange = (descriptor: CameraParameterDescriptor) =>
  descriptor.min !== null && descriptor.min !== undefined &&
  descriptor.max !== null && descriptor.max !== undefined

const setValue = (name: string, value: string) => {
  if (value === '' || value === null || value === undefined) {
    delete values.value[name]
  } else {
    values.value[name] = value
  }
}

const fetchCameras = async () => {
  try {
    cameraList.value = (await api.get<any[]>('/api/camera')) || []
  } catch (error) {
    console.error('Error al cargar las cámaras:', error)
    cameraList.value = []
  }
}

const loadParameters = async () => {
  if (!selectedCameraId.value) {
    response.value = null
    values.value = {}
    focusThreshold.value = ''
    return
  }

  isLoading.value = true
  try {
    const result = await api.get<ParametersResponse>(`/api/camera/${selectedCameraId.value}/parameters`)
    response.value = result

    // Se parte de lo guardado; si un parámetro no tiene valor guardado, se deja
    // vacío en vez de precargar el actual del dispositivo, para distinguir
    // "el usuario no configuró esto" de "el usuario eligió justo este valor".
    values.value = { ...(result?.saved || {}) }
    focusThreshold.value = result?.focusThreshold || ''
  } catch (error: any) {
    console.error('Error al cargar los parámetros:', error)
    response.value = null
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: error?.data || 'No se pudieron cargar los parámetros de la cámara'
    })
  } finally {
    isLoading.value = false
  }
}

const saveParameters = async () => {
  if (!selectedCameraId.value) return

  isSaving.value = true
  try {
    await api.put(`/api/camera/${selectedCameraId.value}/parameters`, {
      values: values.value,
      focusThreshold: focusThreshold.value === '' ? null : focusThreshold.value
    })

    alertStore.NewAlert({
      type: 'OK',
      tittle: 'Éxito',
      data: 'Parámetros guardados. Se aplicarán en la próxima captura.'
    })

    // Se relee para mostrar los valores que el dispositivo aceptó realmente,
    // que pueden diferir de los pedidos (redondeo al incremento soportado).
    await loadParameters()
  } catch (error: any) {
    console.error('Error al guardar los parámetros:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: error?.data || 'No se pudieron guardar los parámetros'
    })
  } finally {
    isSaving.value = false
  }
}

const clearAll = () => {
  values.value = {}
  alertStore.NewAlert({
    type: 'warning',
    tittle: 'Valores limpiados',
    data: 'Pulse Guardar para volver al comportamiento por defecto del driver.'
  })
}

onMounted(() => {
  fetchCameras()
})
</script>
