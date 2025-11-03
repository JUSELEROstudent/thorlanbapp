<template>
   <div class=" w-full flex-col space-y-4 p-4 "> 
      <div class="flex flex-row space-x-4 items-center px-4 bg-gray rounded p-1">
        <label class="flex cursor-pointer gap-2 self-center">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/></svg>
            <input type="checkbox" value="synthwave" class="toggle theme-controller"/>
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>
        </label>
      <div class="flex-1">
        <button class="btn btn-success float-end"> GUARDAR</button>
      </div>
    </div>
    <div>
      <div class="w-full flex flex-row space-x-4 p-4">
        <div class="flex-1">
          <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
            <!-- Card Header -->
            <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
              <h3 class="text-lg font-semibold text-gray-900">Config cameras</h3>
              <button class="btn btn-primary btn-sm">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M5 12h14M12 5v14"/>
                </svg>
                Agregar
              </button>
            </div>
            
            <!-- Card Body -->
            <div class="p-6">
              <div class="space-y-3">
                <!-- Lista de elementos -->
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 1</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
                
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 2</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
                
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 3</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>  
          <div class="mx-4 mb-6 bg-white border border-gray-200 rounded-lg shadow-sm">
            <!-- Card Header -->
            <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
              <h3 class="text-lg font-semibold text-gray-900">Config cameras</h3>
              <button class="btn btn-primary btn-sm">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M5 12h14M12 5v14"/>
                </svg>
                Agregar
              </button>
            </div>
            
            <!-- Card Body -->
            <div class="p-6">
              <div class="space-y-3">
                <!-- Lista de elementos -->
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 1</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
                
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 2</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
                
                <div class="flex items-center justify-between p-3 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors">
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900">Elemento 3</span>
                    <p class="text-xs text-gray-500">Descripción del elemento</p>
                  </div>
                  <button class="btn btn-error btn-xs ml-3">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>                  
        </div>        
        <div class="flex-1 p-4 bg-gray-100 rounded">
          <h3 class="font-semibold mb-2">Sección Derecha</h3>
          <div>test1</div>
        </div>
      </div>
      
    </div>
  </div>
</template>
<script setup lang="ts">

function alerta()
{
    alert('dio click')
}

</script>