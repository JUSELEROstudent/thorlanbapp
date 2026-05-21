<template>
  <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
    <!-- Card Header -->
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
      <h3 class="text-lg font-semibold text-gray-900">Group Calibration</h3>

      <!-- Botones modo normal -->
      <button v-if="!isAddingMode" @click="enableAddMode" class="btn btn-primary btn-sm">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M5 12h14M12 5v14" />
        </svg>
        Agregar
      </button>

      <!-- Botones modo agregar -->
      <div v-if="isAddingMode" class="flex space-x-2">
        <button @click="saveNewRecord" :disabled="isSaving" class="btn btn-success btn-sm">
          <span v-if="isSaving" class="loading loading-spinner loading-xs"></span>
          <svg v-if="!isSaving" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" />
            <polyline points="17,21 17,13 7,13 7,21" />
            <polyline points="7,3 7,8 15,8" />
          </svg>
          {{ isSaving ? 'Guardando...' : 'Guardar' }}
        </button>
        <button @click="cancelAddMode" :disabled="isSaving" class="btn btn-outline btn-sm">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 6L6 18M6 6l12 12" />
          </svg>
          Cancelar
        </button>
      </div>
    </div>

    <!-- Card Body -->
    <div class="p-6">
      <!-- Formulario para agregar nuevo registro -->
      <div v-if="isAddingMode" class="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <h4 class="text-md font-medium text-blue-900 mb-4">Nuevo Grupo de Calibración</h4>
        <div class="grid grid-cols-1 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Cámara *</label>
            <select
              v-model="newRecord.cameraId"
              class="select select-bordered w-full"
              :disabled="isSaving || isLoadingReferences"
            >
              <option value="" disabled>
                {{ isLoadingReferences ? 'Cargando cámaras...' : 'Seleccione una cámara' }}
              </option>
              <option v-for="camera in cameraList" :key="camera.cameraId" :value="camera.cameraId">
                {{ camera.name }}
              </option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Microscopio *</label>
            <select
              v-model="newRecord.microscopeId"
              class="select select-bordered w-full"
              :disabled="isSaving || isLoadingReferences"
            >
              <option value="" disabled>
                {{ isLoadingReferences ? 'Cargando microscopios...' : 'Seleccione un microscopio' }}
              </option>
              <option v-for="microscope in microscopeList" :key="microscope.microscopeId" :value="microscope.microscopeId">
                {{ microscope.name }}
              </option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Incremento *</label>
            <select
              v-model="newRecord.increaseId"
              class="select select-bordered w-full"
              :disabled="isSaving || isLoadingReferences"
            >
              <option value="" disabled>
                {{ isLoadingReferences ? 'Cargando incrementos...' : 'Seleccione un incremento' }}
              </option>
              <option v-for="increase in increaseList" :key="increase.increaseId" :value="increase.increaseId">
                {{ increase.name }} ({{ increase.value }})
              </option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Información adicional</label>
            <textarea
              v-model="newRecord.aditionalInfo"
              class="textarea textarea-bordered w-full"
              placeholder="Información adicional (opcional)"
              rows="3"
              :disabled="isSaving"
            ></textarea>
          </div>
        </div>
      </div>

      <div class="space-y-3">
        <!-- Lista de elementos existentes -->
        <div v-if="elementList.length === 0" class="text-center p-6 text-gray-500">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" class="mx-auto mb-2 text-gray-300">
            <circle cx="12" cy="12" r="3" />
            <path d="M12 1v6m0 6v6" />
            <path d="m15.5 3.5-2 2" />
            <path d="m10.5 18.5-2 2" />
            <path d="m8.5 8.5-2 2" />
            <path d="m15.5 13.5-2 2" />
          </svg>
          <p>No hay grupos de calibración registrados</p>
        </div>

        <div
          v-for="element in elementList"
          :key="element.groupCailbrationId"
          class="rounded-md border border-gray-200 bg-gray-50"
        >
          <div class="flex items-center justify-between gap-3 p-3 hover:bg-gray-100 transition-colors">
            <div class="flex items-center gap-2">
              <button
                class="btn btn-ghost btn-xs"
                @click="toggleGroup(element.groupCailbrationId)"
                :aria-label="isGroupExpanded(element.groupCailbrationId) ? 'Cerrar' : 'Abrir'"
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  :class="['transition-transform', isGroupExpanded(element.groupCailbrationId) ? 'rotate-90' : '']"
                >
                  <polyline points="9 18 15 12 9 6" />
                </svg>
              </button>
              <div class="flex-1">
                <span class="text-sm font-medium text-gray-900">
                  {{ resolveCameraName(element.cameraId, element.camera) }} ·
                  {{ resolveMicroscopeName(element.microscopeId, element.microscope) }} ·
                  {{ resolveIncreaseName(element.increaseId, element.increase) }}
                </span>
                <p class="text-xs text-gray-500">
                  {{ formatDate(element.date) }}
                  <span v-if="element.aditionalInfo"> - {{ element.aditionalInfo }}</span>
                </p>
              </div>
            </div>
            <button @click="deleteElement(element.groupCailbrationId)" class="btn btn-error btn-xs">
              <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
              </svg>
            </button>
          </div>

          <div v-if="isGroupExpanded(element.groupCailbrationId)" class="border-t border-gray-200 bg-white p-4 space-y-4">
            <div class="flex items-center justify-between">
              <h4 class="text-sm font-semibold text-gray-800">Pics Calibration</h4>
              <button
                v-if="!picsAddingMode[element.groupCailbrationId]"
                class="btn btn-primary btn-xs"
                @click="enablePicsAddMode(element.groupCailbrationId)"
              >
                Agregar
              </button>
            </div>

            <div v-if="picsLoading[element.groupCailbrationId]" class="text-sm text-gray-500">Cargando registros...</div>

            <div v-else class="space-y-3">
              <div v-if="(picsByGroup[element.groupCailbrationId] || []).length === 0" class="text-xs text-gray-400">
                No hay registros de Pics Calibration
              </div>

              <div
                v-for="pic in picsByGroup[element.groupCailbrationId] || []"
                :key="pic.picsCalibrationId"
                class="rounded border border-gray-200 p-3"
              >
                <div class="flex items-center justify-between">
                  <div>
                    <p class="text-sm font-medium text-gray-800">
                      Eje: {{ pic.axeDirectionCalibration }} · Aceptado: {{ pic.acepted ? 'Sí' : 'No' }}
                    </p>
                    <p class="text-xs text-gray-500">
                      dx: {{ pic.dx }} · dy: {{ pic.dy }} · Confianza: {{ pic.confidence }}
                    </p>
                    <p class="text-xs text-gray-500">
                      Unidad: {{ pic.measureUnit }} · Movimiento: {{ pic.movementValue }}
                    </p>
                  </div>
                  <button @click="deletePicCalibration(pic.picsCalibrationId, element.groupCailbrationId)" class="btn btn-error btn-xs">
                    Eliminar
                  </button>
                </div>
                <div v-if="pic.pic1 || pic.pic2" class="mt-2 grid gap-2 text-xs text-gray-500">
                  <div v-if="pic.pic1">
                    <span class="font-semibold">Pic1:</span>
                    <span class="ml-1 break-all">{{ pic.pic1 }}</span>
                  </div>
                  <div v-if="pic.pic2">
                    <span class="font-semibold">Pic2:</span>
                    <span class="ml-1 break-all">{{ pic.pic2 }}</span>
                  </div>
                </div>
              </div>
            </div>

            <div
              v-if="picsAddingMode[element.groupCailbrationId]"
              class="rounded border border-blue-200 bg-blue-50 p-4"
            >
              <div class="flex items-center justify-between mb-3">
                <h5 class="text-sm font-semibold text-blue-900">Nuevo Pics Calibration</h5>
                <button class="btn btn-outline btn-xs" @click="cancelPicsAddMode(element.groupCailbrationId)">Cancelar</button>
              </div>
              <div class="grid grid-cols-1 gap-3">
                <div>
                  <label class="block text-xs font-medium text-gray-700 mb-1">Combo de imágenes *</label>
                  <select
                    v-model="picForms[element.groupCailbrationId].selectedPairId"
                    class="select select-bordered w-full"
                  >
                    <option :value="null" disabled>Seleccione un combo</option>
                    <option v-for="option in captureOptions" :key="option.id" :value="option.id">
                      {{ option.label }}
                    </option>
                  </select>
                  <p class="mt-1 text-xs text-gray-500">Usa las capturas del streaming (2 imágenes por combo).</p>
                </div>
                <div v-if="getSelectedPair(element.groupCailbrationId)" class="grid grid-cols-2 gap-2">
                  <img
                    v-for="(image, index) in getSelectedPair(element.groupCailbrationId)?.images"
                    :key="index"
                    :src="image"
                    class="h-20 w-full rounded object-cover"
                  />
                </div>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Dirección Eje *</label>
                    <input
                      v-model="picForms[element.groupCailbrationId].axeDirectionCalibration"
                      maxlength="2"
                      class="input input-bordered w-full"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Aceptado</label>
                    <select v-model="picForms[element.groupCailbrationId].acepted" class="select select-bordered w-full">
                      <option :value="true">Sí</option>
                      <option :value="false">No</option>
                    </select>
                  </div>
                </div>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">dx</label>
                    <input v-model.number="picForms[element.groupCailbrationId].dx" type="number" step="0.0001" class="input input-bordered w-full" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">dy</label>
                    <input v-model.number="picForms[element.groupCailbrationId].dy" type="number" step="0.0001" class="input input-bordered w-full" />
                  </div>
                </div>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Confianza</label>
                    <input v-model.number="picForms[element.groupCailbrationId].confidence" type="number" step="0.0001" class="input input-bordered w-full" />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Unidad de medida *</label>
                    <input v-model="picForms[element.groupCailbrationId].measureUnit" class="input input-bordered w-full" />
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-700 mb-1">Movimiento</label>
                  <input v-model.number="picForms[element.groupCailbrationId].movementValue" type="number" step="0.0001" class="input input-bordered w-full" />
                </div>
              </div>
              <div class="mt-4">
                <button
                  class="btn btn-info btn-sm mr-2"
                  @click="simulatePicCalibration(element.groupCailbrationId)"
                  :disabled="picsSaving[element.groupCailbrationId]"
                >
                  Simular
                </button>
                <button
                  class="btn btn-success btn-sm"
                  @click="savePicCalibration(element.groupCailbrationId)"
                  :disabled="picsSaving[element.groupCailbrationId]"
                >
                  <span v-if="picsSaving[element.groupCailbrationId]" class="loading loading-spinner loading-xs"></span>
                  Guardar
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, computed } from 'vue'
import { alertsClient } from '../../stores/alerts'
import { useCapturesStore } from '~/stores/captures'

const alertStore = alertsClient()
const config = useRuntimeConfig()
const capturesStore = useCapturesStore()

// Estados reactivos
const isAddingMode = ref(false)
const isSaving = ref(false)
const isLoadingReferences = ref(false)
const elementList = ref<GroupCalibrationElement[]>([])
const cameraList = ref<CameraElement[]>([])
const microscopeList = ref<MicroscopeElement[]>([])
const increaseList = ref<IncreaseElement[]>([])
const expandedGroups = ref(new Set<string>())
const picsByGroup = reactive<Record<string, PicsCalibrationElement[]>>({})
const picsLoading = reactive<Record<string, boolean>>({})
const picsAddingMode = reactive<Record<string, boolean>>({})
const picsSaving = reactive<Record<string, boolean>>({})
const picForms = reactive<Record<string, PicsCalibrationForm>>({})

interface CameraElement {
  cameraId: string
  name: string
}

interface MicroscopeElement {
  microscopeId: string
  name: string
}

interface IncreaseElement {
  increaseId: string
  name: string
  value: number
}

interface GroupCalibrationElement {
  groupCailbrationId: string
  cameraId: string
  microscopeId: string
  increaseId: string
  date: string
  aditionalInfo: string
  camera?: CameraElement | null
  microscope?: MicroscopeElement | null
  increase?: IncreaseElement | null
}

interface PicsCalibrationElement {
  picsCalibrationId: string
  groupCailbrationId: string
  pic1?: string
  pic2?: string
  axeDirectionCalibration: string
  acepted: boolean
  dx: number
  dy: number
  confidence: number
  measureUnit: string
  movementValue: number
}

interface PicsCalibrationForm {
  selectedPairId: number | null
  axeDirectionCalibration: string
  acepted: boolean
  dx: number
  dy: number
  confidence: number
  measureUnit: string
  movementValue: number
  simulated: boolean
}

const newRecord = ref({
  cameraId: '',
  microscopeId: '',
  increaseId: '',
  aditionalInfo: ''
})

const createDefaultPicForm = (): PicsCalibrationForm => ({
  selectedPairId: null,
  axeDirectionCalibration: '',
  acepted: false,
  dx: 0,
  dy: 0,
  confidence: 0,
  measureUnit: '',
  movementValue: 0,
  simulated: false
})

const ensurePicForm = (groupId: string) => {
  if (!picForms[groupId]) {
    picForms[groupId] = createDefaultPicForm()
  }
}

const captureOptions = computed(() =>
  capturesStore.pairs.map((pair, index) => ({
    id: pair.id,
    label: `Combo ${index + 1} (${pair.images.length}/${capturesStore.maxPerPair})`
  }))
)

const getSelectedPair = (groupId: string) => {
  const pairId = picForms[groupId]?.selectedPairId
  if (!pairId) {
    return null
  }
  return capturesStore.pairs.find((pair) => pair.id === pairId) ?? null
}

const dataUrlToFile = (dataUrl: string, filename: string) => {
  const [header, data] = dataUrl.split(',')
  const mimeMatch = /data:(.*?);base64/.exec(header)
  const mime = mimeMatch?.[1] ?? 'image/png'
  const binary = atob(data)
  const array = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i += 1) {
    array[i] = binary.charCodeAt(i)
  }
  return new File([array], filename, { type: mime })
}

const setGroupExpanded = (groupId: string, expanded: boolean) => {
  const next = new Set(expandedGroups.value)
  if (expanded) {
    next.add(groupId)
  } else {
    next.delete(groupId)
  }
  expandedGroups.value = next
}

const isGroupExpanded = (groupId: string) => expandedGroups.value.has(groupId)

const fetchPicsCalibration = async (groupId: string) => {
  try {
    picsLoading[groupId] = true
    const response = await $fetch(`${config.public.apiUrl}/api/PicsCalibration`, {
      method: 'GET',
      query: {
        groupCailbrationId: groupId
      },
      headers: {
        'Content-Type': 'application/json'
      }
    }) as PicsCalibrationElement[]

    picsByGroup[groupId] = response ?? []
  } catch (error) {
    console.error('Error al obtener pics calibration:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al cargar Pics Calibration'
    })
    picsByGroup[groupId] = []
  } finally {
    picsLoading[groupId] = false
  }
}

const toggleGroup = async (groupId: string) => {
  const isExpanded = isGroupExpanded(groupId)
  if (isExpanded) {
    setGroupExpanded(groupId, false)
    return
  }

  setGroupExpanded(groupId, true)
  ensurePicForm(groupId)
  if (!picsByGroup[groupId]) {
    await fetchPicsCalibration(groupId)
  }
}

const enablePicsAddMode = (groupId: string) => {
  ensurePicForm(groupId)
  picsAddingMode[groupId] = true
}

const cancelPicsAddMode = (groupId: string) => {
  picsAddingMode[groupId] = false
  picForms[groupId] = createDefaultPicForm()
}

const simulatePicCalibration = async (groupId: string) => {
  try {
    ensurePicForm(groupId)
    const form = picForms[groupId]
    const selectedPair = getSelectedPair(groupId)

    if (!selectedPair || selectedPair.images.length < capturesStore.maxPerPair) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'Seleccione un combo con 2 imágenes'
      })
      return
    }

    picsSaving[groupId] = true

  const formData = new FormData()
  formData.append('img1', dataUrlToFile(selectedPair.images[0], 'pic1.png'))
  formData.append('img2', dataUrlToFile(selectedPair.images[1], 'pic2.png'))

    const response = await $fetch(`${config.public.apiUrl}/api/PhaseCorrelationCalibration/file-data`, {
      method: 'POST',
      body: formData
    }) as { dx: number; dy: number; confidence: number }

    if (response) {
      form.dx = response.dx
      form.dy = response.dy
      form.confidence = response.confidence
      form.simulated = true
      alertStore.NewAlert({
        type: 'OK',
        tittle: 'Simulación',
        data: 'Valores calculados correctamente'
      })
    }
  } catch (error) {
    console.error('Error al simular pics calibration:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al simular los valores'
    })
  } finally {
    picsSaving[groupId] = false
  }
}

const savePicCalibration = async (groupId: string) => {
  try {
    ensurePicForm(groupId)
    const form = picForms[groupId]

    const selectedPair = getSelectedPair(groupId)
    if (!selectedPair || selectedPair.images.length < capturesStore.maxPerPair) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'Seleccione un combo con 2 imágenes'
      })
      return
    }

    if (!form.axeDirectionCalibration.trim()) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'La dirección del eje es requerida'
      })
      return
    }

    if (!form.measureUnit.trim()) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'La unidad de medida es requerida'
      })
      return
    }

    if (!form.simulated) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'Debe simular antes de guardar'
      })
      return
    }

    picsSaving[groupId] = true

    const formData = new FormData()
    formData.append('GroupCailbrationId', groupId)
    formData.append('AxeDirectionCalibration', form.axeDirectionCalibration.trim())
    formData.append('Acepted', String(form.acepted))
    formData.append('dx', String(form.dx))
    formData.append('dy', String(form.dy))
    formData.append('Confidence', String(form.confidence))
    formData.append('MeasureUnit', form.measureUnit.trim())
    formData.append('MovementValue', String(form.movementValue))
    formData.append('Pic1File', dataUrlToFile(selectedPair.images[0], 'pic1.png'))
    formData.append('Pic2File', dataUrlToFile(selectedPair.images[1], 'pic2.png'))

    const response = await $fetch(`${config.public.apiUrl}/api/PicsCalibration`, {
      method: 'POST',
      body: formData
    })

    if (response) {
      alertStore.NewAlert({
        type: 'OK',
        tittle: 'Éxito',
        data: 'Pics Calibration creado exitosamente'
      })

      picsAddingMode[groupId] = false
      picForms[groupId] = createDefaultPicForm()
      await fetchPicsCalibration(groupId)
    }
  } catch (error) {
    console.error('Error al crear pics calibration:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al crear Pics Calibration'
    })
  } finally {
    picsSaving[groupId] = false
  }
}

const deletePicCalibration = async (picsCalibrationId: string, groupId: string) => {
  try {
    await $fetch(`${config.public.apiUrl}/api/PicsCalibration/${picsCalibrationId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    alertStore.NewAlert({
      type: 'OK',
      tittle: 'Éxito',
      data: 'Pics Calibration eliminado exitosamente'
    })

    await fetchPicsCalibration(groupId)
  } catch (error) {
    console.error('Error al eliminar pics calibration:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al eliminar Pics Calibration'
    })
  }
}

const fetchElements = async () => {
  try {
    const response = await $fetch(`${config.public.apiUrl}/api/GroupCalibration`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    }) as GroupCalibrationElement[]

    if (response) {
      elementList.value = response
    }
  } catch (error) {
    console.error('Error al obtener grupos de calibración:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al cargar los grupos de calibración'
    })
  }
}

const fetchReferences = async () => {
  try {
    isLoadingReferences.value = true

    const [cameraResponse, microscopeResponse, increaseResponse] = await Promise.all([
      $fetch(`${config.public.apiUrl}/api/camera`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        }
      }) as Promise<CameraElement[]>,
      $fetch(`${config.public.apiUrl}/api/microscope`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        }
      }) as Promise<MicroscopeElement[]>,
      $fetch(`${config.public.apiUrl}/api/increase`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json'
        }
      }) as Promise<IncreaseElement[]>
    ])

    cameraList.value = cameraResponse ?? []
    microscopeList.value = microscopeResponse ?? []
    increaseList.value = increaseResponse ?? []
  } catch (error) {
    console.error('Error al obtener referencias:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al cargar listas de referencia'
    })
  } finally {
    isLoadingReferences.value = false
  }
}

const deleteElement = async (groupCailbrationId: string) => {
  try {
    await $fetch(`${config.public.apiUrl}/api/GroupCalibration/${groupCailbrationId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    alertStore.NewAlert({
      type: 'OK',
      tittle: 'Éxito',
      data: 'Grupo de calibración eliminado exitosamente'
    })

    await fetchElements()
  } catch (error) {
    console.error('Error al eliminar grupo de calibración:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al eliminar el grupo de calibración'
    })
  }
}

const enableAddMode = async () => {
  isAddingMode.value = true
  newRecord.value = {
    cameraId: '',
    microscopeId: '',
    increaseId: '',
    aditionalInfo: ''
  }
  await fetchReferences()
}

const cancelAddMode = () => {
  isAddingMode.value = false
  newRecord.value = {
    cameraId: '',
    microscopeId: '',
    increaseId: '',
    aditionalInfo: ''
  }
}

const saveNewRecord = async () => {
  try {
    if (!newRecord.value.cameraId) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'La cámara es requerida'
      })
      return
    }

    if (!newRecord.value.microscopeId) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'El microscopio es requerido'
      })
      return
    }

    if (!newRecord.value.increaseId) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'El incremento es requerido'
      })
      return
    }

    isSaving.value = true

    const recordData = {
      GroupCailbrationId: crypto.randomUUID(),
      CameraId: newRecord.value.cameraId,
      MicroscopeId: newRecord.value.microscopeId,
      IncreaseId: newRecord.value.increaseId,
      Date: new Date().toISOString(),
      AditionalInfo: newRecord.value.aditionalInfo.trim()
    }

    const response = await $fetch(`${config.public.apiUrl}/api/GroupCalibration`, {
      method: 'POST',
      body: recordData,
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (response) {
      alertStore.NewAlert({
        type: 'OK',
        tittle: 'Éxito',
        data: 'Grupo de calibración creado exitosamente'
      })

      isAddingMode.value = false
      newRecord.value = {
        cameraId: '',
        microscopeId: '',
        increaseId: '',
        aditionalInfo: ''
      }

      await fetchElements()
    }
  } catch (error) {
    console.error('Error al crear grupo de calibración:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al crear el grupo de calibración'
    })
  } finally {
    isSaving.value = false
  }
}

const resolveCameraName = (cameraId: string, camera?: CameraElement | null) => {
  if (camera?.name) {
    return camera.name
  }
  return cameraList.value.find((item) => item.cameraId === cameraId)?.name ?? 'Cámara no encontrada'
}

const resolveMicroscopeName = (microscopeId: string, microscope?: MicroscopeElement | null) => {
  if (microscope?.name) {
    return microscope.name
  }
  return microscopeList.value.find((item) => item.microscopeId === microscopeId)?.name ?? 'Microscopio no encontrado'
}

const resolveIncreaseName = (increaseId: string, increase?: IncreaseElement | null) => {
  if (increase?.name) {
    return `${increase.name} (${increase.value})`
  }
  const fallback = increaseList.value.find((item) => item.increaseId === increaseId)
  return fallback ? `${fallback.name} (${fallback.value})` : 'Incremento no encontrado'
}

const formatDate = (dateValue?: string) => {
  if (!dateValue) {
    return 'Fecha no disponible'
  }
  const parsedDate = new Date(dateValue)
  if (Number.isNaN(parsedDate.getTime())) {
    return dateValue
  }
  return parsedDate.toLocaleString()
}

onMounted(() => {
  fetchElements()
  fetchReferences()
})
</script>
