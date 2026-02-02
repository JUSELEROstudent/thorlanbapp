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
          class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors"
        >
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
          <button @click="deleteElement(element.groupCailbrationId)" class="btn btn-error btn-xs ml-3">
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { alertsClient } from '../../stores/alerts'

const alertStore = alertsClient()
const config = useRuntimeConfig()

// Estados reactivos
const isAddingMode = ref(false)
const isSaving = ref(false)
const isLoadingReferences = ref(false)
const elementList = ref<GroupCalibrationElement[]>([])
const cameraList = ref<CameraElement[]>([])
const microscopeList = ref<MicroscopeElement[]>([])
const increaseList = ref<IncreaseElement[]>([])

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

const newRecord = ref({
  cameraId: '',
  microscopeId: '',
  increaseId: '',
  aditionalInfo: ''
})

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
