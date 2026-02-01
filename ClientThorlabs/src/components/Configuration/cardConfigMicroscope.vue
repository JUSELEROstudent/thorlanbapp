<template>
    <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
    <!-- Card Header -->
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
 
        <h3 class="text-lg font-semibold text-gray-900">Config Microscope</h3>

      <!-- Botones modo normal -->
      <button v-if="!isAddingMode" @click="enableAddMode" class="btn btn-primary btn-sm">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M5 12h14M12 5v14"/>
        </svg>
        Agregar
      </button>
      
      <!-- Botones modo agregar -->
      <div v-if="isAddingMode" class="flex space-x-2">
        <button @click="saveNewRecord" :disabled="isSaving" class="btn btn-success btn-sm">
          <span v-if="isSaving" class="loading loading-spinner loading-xs"></span>
          <svg v-if="!isSaving" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17,21 17,13 7,13 7,21"/>
            <polyline points="7,3 7,8 15,8"/>
          </svg>
          {{ isSaving ? 'Guardando...' : 'Guardar' }}
        </button>
        <button @click="cancelAddMode" :disabled="isSaving" class="btn btn-outline btn-sm">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 6L6 18M6 6l12 12"/>
          </svg>
          Cancelar
        </button>
      </div>
    </div>
    
    <!-- Card Body -->
    <div class="p-6">
      <!-- Formulario para agregar nuevo registro -->
      <div v-if="isAddingMode" class="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <h4 class="text-md font-medium text-blue-900 mb-4">Nuevo Microscopio</h4>
        <div class="grid grid-cols-1 gap-4">
          <!-- Campo Name -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Nombre *</label>
            <input 
              v-model="newRecord.name" 
              type="text" 
              class="input input-bordered w-full"
              placeholder="Ingrese el nombre del microscopio"
              :disabled="isSaving"
            />
          </div>
          
          <!-- Campo Brand -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Marca *</label>
            <input 
              v-model="newRecord.brand" 
              type="text" 
              class="input input-bordered w-full"
              placeholder="Ingrese la marca"
              :disabled="isSaving"
            />
          </div>
          
          <!-- Campo Site -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Sitio *</label>
            <input 
              v-model="newRecord.site" 
              type="text" 
              class="input input-bordered w-full"
              placeholder="Ingrese el sitio/ubicación"
              :disabled="isSaving"
            />
          </div>
          
          <!-- Campo AditionalInfo -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Información Adicional</label>
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
            <circle cx="12" cy="12" r="3"/>
            <path d="M12 1v6m0 6v6"/>
            <path d="m15.5 3.5-2 2"/>
            <path d="m10.5 18.5-2 2"/>
            <path d="m8.5 8.5-2 2"/>
            <path d="m15.5 13.5-2 2"/>
          </svg>
          <p>No hay microscopios registrados</p>
        </div>
        
        <div 
          v-for="element in elementList" 
          :key="element.microscopeId"
          class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors"
        >
          <div class="flex-1">
            <span class="text-sm font-medium text-gray-900">{{ element.name }}</span>
            <p class="text-xs text-gray-500">{{ element.brand }} - {{ element.site }} {{ element.aditionalInfo ? '- ' + element.aditionalInfo : '' }}</p>
          </div>
          <button @click="deleteElement(element.microscopeId)" class="btn btn-error btn-xs ml-3">
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
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
const elementList = ref<MicroscopeElement[]>([])

// Interface para el elemento
interface MicroscopeElement {
  microscopeId: string
  name: string
  brand: string
  site: string
  aditionalInfo: string
}

// Modelo para el nuevo registro
const newRecord = ref({
  name: '',
  brand: '',
  site: '',
  aditionalInfo: ''
})

// Función para obtener la lista de elementos
const fetchElements = async () => {
  try {
    const response = await $fetch(`${config.public.apiUrl}/api/microscope`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    }) as MicroscopeElement[]
    
    if (response) {
      elementList.value = response
    }
  } catch (error) {
    console.error('Error al obtener microscopios:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al cargar los microscopios'
    })
  }
}

// Función para eliminar elemento
const deleteElement = async (microscopeId: string) => {
  try {
    await $fetch(`${config.public.apiUrl}/api/microscope/${microscopeId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    // Si llega aquí sin error, la eliminación fue exitosa (204 No Content)
    alertStore.NewAlert({
      type: 'OK',
      tittle: 'Éxito',
      data: 'Microscopio eliminado exitosamente'
    })
    
    // Refrescar la lista
    await fetchElements()
  } catch (error) {
    console.error('Error al eliminar microscopio:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al eliminar el microscopio'
    })
  }
}

// Función para habilitar modo agregar
const enableAddMode = () => {
  isAddingMode.value = true
  // Limpiar el formulario
  newRecord.value = {
    name: '',
    brand: '',
    site: '',
    aditionalInfo: ''
  }
}

// Función para cancelar modo agregar
const cancelAddMode = () => {
  isAddingMode.value = false
  // Limpiar el formulario
  newRecord.value = {
    name: '',
    brand: '',
    site: '',
    aditionalInfo: ''
  }
}

// Función para guardar nuevo registro
const saveNewRecord = async () => {
  try {
    // Validar campos requeridos
    if (!newRecord.value.name.trim()) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'El nombre es requerido'
      })
      return
    }

    if (!newRecord.value.brand.trim()) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'La marca es requerida'
      })
      return
    }

    if (!newRecord.value.site.trim()) {
      alertStore.NewAlert({
        type: 'error',
        tittle: 'Error de validación',
        data: 'El sitio es requerido'
      })
      return
    }

    // Bloquear botón guardar
    isSaving.value = true

    // Preparar datos para enviar
    const recordData = {
      MicroscopeId: crypto.randomUUID(), // Generar nuevo GUID
      Name: newRecord.value.name.trim(),
      Brand: newRecord.value.brand.trim(),
      Site: newRecord.value.site.trim(),
      AditionalInfo: newRecord.value.aditionalInfo.trim()
    }

    // Realizar petición al endpoint
    const response = await $fetch(`${config.public.apiUrl}/api/microscope`, {
      method: 'POST',
      body: recordData,
      headers: {
        'Content-Type': 'application/json'
      }
    })

    // Si la petición fue exitosa
    if (response) {
      alertStore.NewAlert({
        type: 'OK',
        tittle: 'Éxito',
        data: 'Microscopio creado exitosamente'
      })
      
      // Salir del modo agregar
      isAddingMode.value = false
      
      // Limpiar formulario
      newRecord.value = {
        name: '',
        brand: '',
        site: '',
        aditionalInfo: ''
      }
      
      // Refrescar la lista después de crear
      await fetchElements()
    }

  } catch (error) {
    console.error('Error al crear microscopio:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al crear el microscopio'
    })
  } finally {
    // Desbloquear botón guardar
    isSaving.value = false
  }
}

// Cargar elementos al montar el componente
onMounted(() => {
  fetchElements()
})
</script>