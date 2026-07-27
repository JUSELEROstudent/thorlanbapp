<template>
    <div class=" w-full flex-col w-full space-y-4 p-4 ">
        <div class="flex flex-row space-x-4 items-center px-4 bg-gray rounded p-1 w-full">

            <label style="color: white;" for="deviceSelect" class="space-x-2">
                <span>Dispositivo</span>
            </label>
            <select id="deviceSelect" v-model="currentDevice" class="select select-bordered w-full max-w-xs ">
                <option :value="ld" v-for="ld in listDevices">
                    {{ ld }}
                </option>
            </select>
            <label style="color: white;" for="groupSelect" class="space-x-2">
                <span>Grupo</span>
            </label>
            <select id="groupSelect" v-model="currentGroup" class="select select-bordered w-full max-w-xs ">
                <option :value="lg.groupCailbrationId" v-for="lg in listGroups">
                    {{ lg.aditionalInfo }}
                </option>
            </select>
            <span class="text-black pl-3" title="defina el tamaño del área a rastrear en milímetros"> Area X * Y (mm)</span>
            <label>
                <div class="flex items-center w-32">
                <input type="number" step="0.1" v-model="areaX" class="input input-bordered w-full ml-2" name="fname" placeholder="X mm" title="Tamaño X en milímetros" >
                </div>
            </label>
            <label>
                <div class="flex items-center w-32">
                <input type="number" step="0.1" v-model="areaY" class="input input-bordered w-full ml-2" name="fname" placeholder="Y mm" title="Tamaño Y en milímetros"  >
                </div>
            </label>
            <div class="flex-1 " > 
                 <button @click="InitStreamImg()" class="btn btn-success bg-blue-900 float-end " :class="{'btn-disabled': statusstreamimg || !canStartStream }">
                Iniciar 
                 </button>
            </div> 
            
        </div>
        <div class="flex justify-center bg-black">
            <div v-show="isendrequest" class=" flex justify-center bg-black aspect-video max-h-[90vh]" >
            <img  ref="imgRef" src="https://localhost:7166/SouerceStaticFiles/boat.jpg">
        </div>    
        </div>
    </div>
</template>
<script setup lang="ts">
import * as signalR from "@microsoft/signalr";
import { alertsClient }  from './../stores/alerts'

const config = useRuntimeConfig();
const alertStore = alertsClient()

const listDevices = ref<string[]>([]);
const currentDevice = ref<string>("");
const listGroups = ref<{ groupCailbrationId: string; aditionalInfo: string }[]>([]);
const currentGroup = ref<string>("");
const isendrequest = ref<boolean>(false);
const imgRef = ref<HTMLImageElement | null>(null);
const statusstreamimg = ref<boolean>(false);
const areaX = ref<number>(10.0);
const areaY = ref<number>(8.0);
const canStartStream = computed(() => {
    const hasDevice = `${currentDevice.value ?? ""}`.trim().length > 0;
    const hasGroup = `${currentGroup.value ?? ""}`.trim().length > 0;
    return hasDevice && hasGroup;
});

let hubConnection = await new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiUrl}/UpdateStatus`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .build();




onMounted( async () => {
  
  try {
      const myHeaders = new Headers()
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
  
      const requestOptions = {
        method: 'GET',
        headers: myHeaders
        // , redirect: 'follow'
      }
      const [devicesResponse, groupsResponse] = await Promise.all([
          fetch(`${config.public.apiUrl}/home/devices`, requestOptions),
          fetch(`${config.public.apiUrl}/api/GroupCalibration`, requestOptions)
      ]);

      if (devicesResponse.ok) {
          const data = await devicesResponse.json();
          listDevices.value = data;
          if (listDevices.value.length > 0) {
              currentDevice.value = data[0];
          }
      } else {
          console.error(`Error en la solicitud: ${devicesResponse.status} - ${devicesResponse.statusText}`);
      }

      if (groupsResponse.ok) {
          const data = await groupsResponse.json();
          listGroups.value = data;
          if (listGroups.value.length > 0) {
              currentGroup.value = data[0].groupCailbrationId;
          }
      } else {
          console.error(`Error en la solicitud: ${groupsResponse.status} - ${groupsResponse.statusText}`);
      }

      isendrequest.value = listDevices.value.length > 0 && listGroups.value.length > 0;
  } catch (error) {
      console.error('Error al realizar la solicitud:', error);
  }
  })

  const handleCamera = async () => {
    try {
        // debugger;
        await hubConnection.stop();
        await hubConnection.start();
        // El hub Imgupdate resuelve la cámara a usar a partir del grupo de
        // calibración (groupCalibrationId -> Camera), no de este primer
        // parámetro: por eso ya no hay selector de cámara en esta vista, y aquí
        // solo se manda un valor cualquiera para cumplir la firma del método.
        hubConnection.stream("Imgupdate", 0, areaX.value, areaY.value, currentGroup.value, currentDevice.value).subscribe({
            next: (item: string) => {
                if (imgRef.value) { imgRef.value.src = `${item}` }
            },
            complete: () => { console.log("Stream completed"); },
            error: (err: Error) => { alertStore.NewAlert({type: 'error',data: err.message, tittle:'Revisar conexiones '}) },
        });
    } catch (err) {  alertStore.NewAlert({type: 'error',data: 'error al establecer coneccion signalr [ES]', tittle:'Revisar conexiones '}) }
}
async function InitStreamImg()
{
    if (!canStartStream.value) {
        alertStore.NewAlert({type: 'error',data: 'Debe seleccionar dispositivo y grupo.', tittle:'Validacion'})
        return;
    }
    statusstreamimg.value= true;
    const response = await handleCamera()
    statusstreamimg.value= false;


}

</script>
