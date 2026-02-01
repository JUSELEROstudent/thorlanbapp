<template>
    <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
    <!-- Card Header -->
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
      <h3 class="text-lg font-semibold text-gray-900">Config Increase</h3>
      
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
        <h4 class="text-md font-medium text-blue-900 mb-4">Nuevo Registro</h4>
        <div class="grid grid-cols-1 gap-4">
          <!-- Campo Name -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Nombre</label>
            <input 
              v-model="newRecord.name" 
              type="text" 
              class="input input-bordered w-full"
              placeholder="Ingrese el nombre"
              :disabled="isSaving"
            />
          </div>
          
          <!-- Campo Value -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Valor</label>
            <input 
              v-model.number="newRecord.value" 
              type="number" 
              step="0.01"
              class="input input-bordered w-full"
              placeholder="0.00"
              :disabled="isSaving"
            />
          </div>
          
          <!-- Campo AditionalInfo -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Información Adicional</label>
            <textarea 
              v-model="newRecord.aditionalInfo" 
              class="textarea textarea-bordered w-full"
              placeholder="Información adicional"
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
            <path d="M9 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2h-4"/>
            <rect width="6" height="10" x="9" y="1" rx="2"/>
          </svg>
          <p>No hay elementos registrados</p>
        </div>
        
        <div 
          v-for="element in elementList" 
          :key="element.increaseId"
          class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors"
        >
          <div class="flex-1">
            <span class="text-sm font-medium text-gray-900">{{ element.name }}</span>
            <p class="text-xs text-gray-500">Valor: {{ element.value }} - {{ element.aditionalInfo || 'Sin información adicional' }}</p>
          </div>
          <button @click="deleteElement(element.increaseId)" class="btn btn-error btn-xs ml-3">
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
const elementList = ref<IncreaseElement[]>([])

// Interface para el elemento
interface IncreaseElement {
  increaseId: string
  name: string
  value: number
  aditionalInfo: string
}

// Modelo para el nuevo registro
const newRecord = ref({
  name: '',
  value: 0,
  aditionalInfo: ''
})

// Función para obtener la lista de elementos
const fetchElements = async () => {
  try {
    const response = await $fetch(`${config.public.apiUrl}/api/increase`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    }) as IncreaseElement[]
    
    if (response) {
      elementList.value = response
    }
  } catch (error) {
    console.error('Error al obtener elementos:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al cargar los elementos'
    })
  }
}

// Función para eliminar elemento
const deleteElement = async (increaseId: string) => {
  try {
    await $fetch(`${config.public.apiUrl}/api/increase/${increaseId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    // Si llega aquí sin error, la eliminación fue exitosa (204 No Content)
    alertStore.NewAlert({
      type: 'OK',
      tittle: 'Éxito',
      data: 'Elemento eliminado exitosamente'
    })
    
    // Refrescar la lista
    await fetchElements()
  } catch (error) {
    console.error('Error al eliminar elemento:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al eliminar el elemento'
    })
  }
}

// Función para habilitar modo agregar
const enableAddMode = () => {
  isAddingMode.value = true
  // Limpiar el formulario
  newRecord.value = {
    name: '',
    value: 0,
    aditionalInfo: ''
  }
}

// Función para cancelar modo agregar
const cancelAddMode = () => {
  isAddingMode.value = false
  // Limpiar el formulario
  newRecord.value = {
    name: '',
    value: 0,
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

    // Bloquear botón guardar
    isSaving.value = true

    // Preparar datos para enviar
    const recordData = {
      IncreaseId: crypto.randomUUID(), // Generar nuevo GUID
      Name: newRecord.value.name.trim(),
      Value: newRecord.value.value,
      AditionalInfo: newRecord.value.aditionalInfo.trim()
    }

    // Realizar petición al endpoint
    const response = await $fetch(`${config.public.apiUrl}/api/increase`, {
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
        data: 'Registro creado exitosamente'
      })
      
      // Salir del modo agregar
      isAddingMode.value = false
      
      // Limpiar formulario
      newRecord.value = {
        name: '',
        value: 0,
        aditionalInfo: ''
      }
      
      // Refrescar la lista después de crear
      await fetchElements()
    }

  } catch (error) {
    console.error('Error al crear registro:', error)
    alertStore.NewAlert({
      type: 'error',
      tittle: 'Error',
      data: 'Error al crear el registro'
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