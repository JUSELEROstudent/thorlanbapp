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
        <div class="flex-1 space-y-4">
        <CardConfigMicroscope />
                  <!-- Componente cardConfigElement -->
        <cardConfigCamera />
                  <!-- Componente cardConfigElement -->
        <CardConfigIncrease />              
        </div>        
        

        
        <div class="flex-1 space-y-4">
          <CameraStreamCapture />
          <CreateCalibration />
        </div>
      </div>
      
    </div>
  </div>
</template>
<script setup lang="ts">
import CardConfigIncrease from '~/components/Configuration/cardConfigIncrease.vue';
import cardConfigCamera from '../components/Configuration/cardConfigCamera.vue'
import CardConfigMicroscope from '~/components/Configuration/cardConfigMicroscope.vue';
import CameraStreamCapture from '~/components/Configuration/CameraStreamCapture.vue';
import CreateCalibration from '~/components/Configuration/CreateCalibration.vue';

function alerta()
{
    alert('dio click')
}

</script>