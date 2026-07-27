<template>
  <CrudCard
    title="Objetivos / Aumentos"
    subtitle="Aumento óptico usado en cada montaje"
    resource-path="increase"
    id-field="increaseId"
    :fields="fields"
  >
    <template #item-subtitle="{ item }">
      <p class="text-xs text-gray-500">
        Valor: {{ item.value }} — {{ item.aditionalInfo || 'Sin información adicional' }}
      </p>
    </template>
  </CrudCard>
</template>

<script setup lang="ts">
/**
 * Configuración de objetivos (aumentos). Toda la mecánica CRUD vive en CrudCard;
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
    key: 'value',
    label: 'Valor',
    type: 'number',
    step: '0.01',
    placeholder: '0.00',
    default: 0,
    help: 'Aumento del objetivo, por ejemplo 10 para 10x.'
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
