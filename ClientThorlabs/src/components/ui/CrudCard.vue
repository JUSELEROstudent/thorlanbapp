<template>
  <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
    <!-- Encabezado -->
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
      <div>
        <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
        <p v-if="subtitle" class="text-xs text-gray-500 mt-0.5">{{ subtitle }}</p>
      </div>

      <button v-if="!isAddingMode" @click="enableAddMode" class="btn btn-primary btn-sm">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M5 12h14M12 5v14" />
        </svg>
        Agregar
      </button>

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

    <!-- Cuerpo -->
    <div class="p-6">
      <!-- Formulario de alta -->
      <div v-if="isAddingMode" class="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <h4 class="text-md font-medium text-blue-900 mb-4">Nuevo Registro</h4>

        <!-- Campos propios del recurso, antes de los genéricos -->
        <slot name="form-extra" :record="newRecord" :disabled="isSaving" />

        <div class="grid grid-cols-1 gap-4">
          <div v-for="field in fields" :key="field.key">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              {{ field.label }}<span v-if="field.required" class="text-red-500"> *</span>
            </label>

            <textarea
              v-if="field.type === 'textarea'"
              v-model="newRecord[field.key]"
              class="textarea textarea-bordered w-full"
              :placeholder="field.placeholder"
              :rows="field.rows || 3"
              :disabled="isSaving"
            ></textarea>

            <input
              v-else-if="field.type === 'number'"
              v-model.number="newRecord[field.key]"
              type="number"
              :step="field.step || 'any'"
              class="input input-bordered w-full"
              :placeholder="field.placeholder"
              :disabled="isSaving"
            />

            <input
              v-else
              v-model="newRecord[field.key]"
              type="text"
              class="input input-bordered w-full"
              :placeholder="field.placeholder"
              :disabled="isSaving"
            />

            <p v-if="field.help" class="text-xs text-gray-500 mt-1">{{ field.help }}</p>
          </div>
        </div>
      </div>

      <!-- Listado -->
      <div class="space-y-3">
        <div v-if="isLoading" class="space-y-2">
          <div class="skeleton h-12 w-full"></div>
          <div class="skeleton h-12 w-full"></div>
        </div>

        <div v-else-if="elementList.length === 0" class="text-center p-6 text-gray-500">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" class="mx-auto mb-2 text-gray-300">
            <path d="M9 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2h-4" />
            <rect width="6" height="10" x="9" y="1" rx="2" />
          </svg>
          <p>No hay elementos registrados</p>
        </div>

        <div
          v-for="element in elementList"
          :key="element[idField]"
          class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors"
        >
          <div class="flex-1 min-w-0">
            <span class="text-sm font-medium text-gray-900">{{ element.name }}</span>
            <slot name="item-subtitle" :item="element">
              <p class="text-xs text-gray-500 truncate">{{ element.aditionalInfo || '' }}</p>
            </slot>
          </div>

          <div class="flex items-center gap-2 ml-3 shrink-0">
            <slot name="item-actions" :item="element" />
            <button @click="deleteElement(element[idField])" class="btn btn-error btn-xs">
              <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Tarjeta CRUD genérica.
 *
 * Las tres tarjetas de configuración (cámara, microscopio, objetivo) eran copias
 * casi idénticas: el mismo ciclo cargar / agregar / guardar / eliminar, el mismo
 * marcado y la misma gestión de avisos, repetido tres veces. Cambiaba poco más
 * que la lista de campos. Este componente concentra todo eso y cada tarjeta
 * queda como una envoltura que declara sus campos.
 *
 * Lo específico de cada recurso entra por slots:
 *   - form-extra   → campos propios (recibe el borrador y puede modificarlo)
 *   - item-subtitle→ cómo se describe cada elemento en la lista
 *   - item-actions → botones adicionales por elemento
 */
import { ref, onMounted } from 'vue'
import { alertsClient } from '../../stores/alerts'

export interface CrudField {
  key: string
  label: string
  type?: 'text' | 'number' | 'textarea'
  placeholder?: string
  required?: boolean
  step?: string | number
  rows?: number
  help?: string
  /** Valor inicial al abrir el formulario. Por defecto '' o 0 según el tipo. */
  default?: unknown
}

const props = defineProps<{
  title: string
  subtitle?: string
  /** Ruta del recurso en la API, por ejemplo 'increase' para /api/increase */
  resourcePath: string
  /** Nombre del campo identificador, por ejemplo 'increaseId' */
  idField: string
  fields: CrudField[]
  /** Campos extra que deben incluirse en el POST aunque no estén en `fields`. */
  extraKeys?: string[]
}>()

const emit = defineEmits<{
  (e: 'created', record: any): void
  (e: 'deleted', id: string): void
  (e: 'loaded', items: any[]): void
  (e: 'add-mode', active: boolean): void
}>()

const alertStore = alertsClient()
const api = useApi()

const isAddingMode = ref(false)
const isSaving = ref(false)
const isLoading = ref(false)
const elementList = ref<any[]>([])
const newRecord = ref<Record<string, any>>({})

const blankRecord = (): Record<string, any> => {
  const record: Record<string, any> = {}
  for (const field of props.fields) {
    record[field.key] = field.default !== undefined
      ? field.default
      : (field.type === 'number' ? 0 : '')
  }
  for (const key of props.extraKeys || []) {
    if (!(key in record)) record[key] = ''
  }
  return record
}

const toPascalCase = (key: string) => key.charAt(0).toUpperCase() + key.slice(1)

const fetchElements = async () => {
  isLoading.value = true
  try {
    const response = await api.get<any[]>(`/api/${props.resourcePath}`)
    elementList.value = response || []
    emit('loaded', elementList.value)
  } catch (error) {
    console.error(`Error al obtener ${props.resourcePath}:`, error)
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: 'Error al cargar los elementos' })
  } finally {
    isLoading.value = false
  }
}

const enableAddMode = () => {
  newRecord.value = blankRecord()
  isAddingMode.value = true
  emit('add-mode', true)
}

const cancelAddMode = () => {
  isAddingMode.value = false
  newRecord.value = blankRecord()
  emit('add-mode', false)
}

const saveNewRecord = async () => {
  // Validación de obligatorios declarada en los campos, en vez de repetir un
  // bloque de ifs por cada tarjeta.
  for (const field of props.fields) {
    if (!field.required) continue
    const value = newRecord.value[field.key]
    const isEmpty = value === null || value === undefined ||
      (typeof value === 'string' && !value.trim())
    if (isEmpty) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: `${field.label} es requerido`
      })
      return
    }
  }

  isSaving.value = true
  try {
    // El backend espera las claves en PascalCase (CameraId, LocalIdentifier...).
    const payload: Record<string, any> = {
      [toPascalCase(props.idField)]: crypto.randomUUID()
    }
    for (const [key, value] of Object.entries(newRecord.value)) {
      payload[toPascalCase(key)] = typeof value === 'string' ? value.trim() : value
    }

    const response = await api.post(`/api/${props.resourcePath}`, payload)

    alertStore.NewAlert({ type: 'OK', tittle: 'Éxito', data: 'Registro creado exitosamente' })
    isAddingMode.value = false
    newRecord.value = blankRecord()
    emit('add-mode', false)
    emit('created', response)
    await fetchElements()
  } catch (error: any) {
    console.error(`Error al crear ${props.resourcePath}:`, error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      // El backend devuelve mensajes útiles (por ejemplo nombre duplicado);
      // mostrarlos evita que el usuario tenga que adivinar qué pasó.
      data: error?.data || error?.message || 'Error al crear el registro'
    })
  } finally {
    isSaving.value = false
  }
}

const deleteElement = async (id: string) => {
  try {
    await api.del(`/api/${props.resourcePath}/${id}`)
    alertStore.NewAlert({ type: 'OK', tittle: 'Éxito', data: 'Elemento eliminado exitosamente' })
    emit('deleted', id)
    await fetchElements()
  } catch (error: any) {
    console.error(`Error al eliminar ${props.resourcePath}:`, error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: error?.data || 'Error al eliminar el elemento'
    })
  }
}

onMounted(() => {
  newRecord.value = blankRecord()
  fetchElements()
})

// Permite que la tarjeta contenedora refresque la lista tras un cambio externo.
defineExpose({ refresh: fetchElements, elementList })
</script>
