<template>
  <CrudCard
    title="Microscopios"
    subtitle="Equipos disponibles en el laboratorio"
    resource-path="microscope"
    id-field="microscopeId"
    :fields="fields"
  >
    <template #item-subtitle="{ item }">
      <p class="text-xs text-gray-500">
        {{ [item.brand, item.site].filter(Boolean).join(' — ') || 'Sin marca ni ubicación' }}
      </p>
    </template>
  </CrudCard>
</template>

<script setup lang="ts">
/**
 * Configuración de microscopios. Toda la mecánica CRUD vive en CrudCard;
 * aquí solo se declaran los campos propios del recurso.
 */
import CrudCard, { type CrudField } from '../ui/CrudCard.vue'

const fields: CrudField[] = [
  {
    key: 'name',
    label: 'Nombre',
    type: 'text',
    placeholder: 'Ingrese el nombre',
    required: true
  },
  {
    key: 'brand',
    label: 'Marca',
    type: 'text',
    placeholder: 'Ingrese la marca'
  },
  {
    key: 'site',
    label: 'Ubicación',
    type: 'text',
    placeholder: 'Ubicación dentro del laboratorio'
  },
  {
    key: 'aditionalInfo',
    label: 'Información Adicional',
    type: 'textarea',
    placeholder: 'Información adicional',
    rows: 3
  }
]
</script>
