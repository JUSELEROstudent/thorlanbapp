<template>
  <div class="w-full flex flex-col gap-4 p-4">
    <!-- Cabecera de la sección con su navegación propia.
         Antes esta página apilaba en una sola pantalla el streaming, los parámetros de
         cámara, el alta de equipos y la creación de combos. Se separó en rutas hijas
         porque ya no cabía nada más y porque cada bloque se usa en un momento distinto
         del trabajo, no a la vez. -->
    <div class="rounded bg-gray p-3 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-lg font-semibold text-white">Configuración</h1>
        <p class="text-xs text-gray-300">Equipos, cámara y calibración del montaje.</p>
      </div>

      <label class="flex cursor-pointer gap-2 items-center" title="Tema claro / oscuro">
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5" /><path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" /></svg>
        <input type="checkbox" value="synthwave" class="toggle theme-controller" />
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" /></svg>
      </label>
    </div>

    <div role="tablist" class="tabs tabs-boxed bg-base-200 overflow-x-auto flex-nowrap">
      <NuxtLink
        v-for="tab in tabs"
        :key="tab.to"
        role="tab"
        class="tab whitespace-nowrap"
        active-class="tab-active"
        :to="tab.to"
      >
        {{ tab.label }}
      </NuxtLink>
      <NuxtLink role="tab" class="tab whitespace-nowrap" to="/calibraciones">
        Registros
        <span class="badge badge-ghost badge-xs ml-2">otra vista</span>
      </NuxtLink>
    </div>

    <NuxtPage />
  </div>
</template>

<script setup lang="ts">
const tabs = [
  { to: '/config/equipos', label: 'Equipos' },
  { to: '/config/camara', label: 'Cámara' },
  { to: '/config/combos', label: 'Combos de calibración' },
  { to: '/config/pasos', label: 'Paso del motor' }
]
</script>
