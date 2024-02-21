<template>
  <div class=" z-10 pt-10 md:pt-1 sm:pt-96 2xl:pt-96">
    <div class="xl:w-3/12 md:w-6/12 bg-white rounded shadow m-auto p-10"
      style="box-shadow: rgba(255, 255, 255, 0.25) 10px 50px 100px 20px, rgba(0, 0, 0, 0.3) 0px 30px 60px -30px, rgba(255, 255, 255, 0.35) 0px -2px 6px 0px inset;">
      <h2 class="text-black">INICIAR SESION</h2>
      <label class="input input-bordered input-success flex items-center m-4">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" class="w-4 h-4 opacity-70">
          <path
            d="M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM12.735 14c.618 0 1.093-.561.872-1.139a6.002 6.002 0 0 0-11.215 0c-.22.578.254 1.139.872 1.139h9.47Z" />
        </svg>
        <input type="text" class="grow" placeholder="Username" v-model="username" />
      </label>
      <label class="input input-bordered input-success  flex items-center m-4">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" class="w-4 h-4 opacity-70">
          <path fill-rule="evenodd"
            d="M14 6a4 4 0 0 1-4.899 3.899l-1.955 1.955a.5.5 0 0 1-.353.146H5v1.5a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1-.5-.5v-2.293a.5.5 0 0 1 .146-.353l3.955-3.955A4 4 0 1 1 14 6Zm-4-2a.75.75 0 0 0 0 1.5.5.5 0 0 1 .5.5.75.75 0 0 0 1.5 0 2 2 0 0 0-2-2Z"
            clip-rule="evenodd" />
        </svg>
        <input type="password" class="grow" value="password" v-model="password" />
      </label>
      <div class="flex justify-between">
        <button @click="SetPageForAll()" class="btn btn-neutral w-4/12 m-2 " href="~/config">Sin Usuario</button>
        <button @click="CheckCredencials()" class="btn btn-success bg-greenuis w-4/12 m-2 ">Iniciar </button>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ConsoleLogger } from '@microsoft/signalr/dist/esm/Utils';
import { alertsClient } from './../stores/alerts'

definePageMeta({
  layout: 'login'
})
const alertStore = alertsClient();
const config = useRuntimeConfig();

const password = ref<string>();
const username = ref<string>();

async function CheckCredencials() {
  const myHeaders = new Headers();
  myHeaders.append('Content-Type', 'application/json')
  const raw = JSON.stringify({ 'User': username.value, 'Password': password.value })
  const requestOptions = {
    method: 'POST',
    headers: myHeaders,
    body: raw
  }
  try {
    const petition = await fetch(`${config.public.apiUrl}/Innerlogin`, requestOptions)
    if (petition.ok) {
      const datafetch = await petition.text();
      localStorage.setItem("stringjwt",datafetch)
      window.location.href='/signalrtest';

      alertStore.NewAlert({ type: 'OK', data: 'Sesion iniciada', tittle: 'none' })

    }
    else{
      throw {error:'se salio'};
    }
  
  }
  catch (error) {
    SetAlert()
    
    console.error("error de login")
  }
}


 function SetAlert() {
  alertStore.NewAlert({ type: 'error', data: 'Fallo en la peticion al servidor', tittle: 'Revisar conexiones ' });

}
function SetPageForAll(){
  window.location.href='/config';
}
</script>