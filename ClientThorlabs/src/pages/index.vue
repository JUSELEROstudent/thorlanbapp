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
        <!-- El visor tiene tres estados. Antes era un <img> fijo apuntando a una imagen
             de ejemplo que no existe (boat.jpg, con host y puerto escritos a mano), así
             que hasta que arrancaba un recorrido se veía el icono de imagen rota. -->
        <div class="flex justify-center bg-black rounded">
            <div v-show="isendrequest" class="flex justify-center items-center bg-black aspect-video max-h-[90vh] w-full">

                <!-- 1. Recorrido en curso: el mosaico en vivo que llega por SignalR. -->
                <img v-show="hasLiveFrame" ref="imgRef" class="max-h-full max-w-full object-contain" />

                <!-- 2. En reposo con un recorrido ya ensamblado: se muestra el último.
                        Va etiquetado porque, sin decirlo, una imagen a pantalla completa
                        en esta vista se lee como si fuera la captura en vivo. -->
                <div v-if="!hasLiveFrame && lastStitchedTour" class="relative w-full h-full flex items-center justify-center">
                    <img
                        :src="lastStitchedUrl!"
                        class="max-h-full max-w-full object-contain opacity-90"
                        @error="lastStitchedTour = null"
                    />
                    <div class="absolute top-2 left-2 bg-black/70 text-white text-xs rounded px-2 py-1">
                        Último recorrido ensamblado · {{ lastStitchedTour.nameFolder }}
                        <span class="text-gray-300">— no es la vista en vivo</span>
                    </div>
                </div>

                <!-- 3. En reposo y sin nada que mostrar. -->
                <div v-else-if="!hasLiveFrame" class="text-center text-gray-400 p-10">
                    <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24"
                         fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"
                         stroke-linejoin="round" class="mx-auto mb-3 text-gray-600">
                        <rect x="3" y="3" width="18" height="18" rx="2" />
                        <path d="M3 9h18M9 3v18" />
                    </svg>
                    <p class="text-sm font-medium text-gray-300">Sin recorrido en curso</p>
                    <p class="text-xs mt-1 max-w-sm mx-auto">
                        Seleccione dispositivo y grupo, indique el área a rastrear y pulse
                        <span class="text-gray-300">Iniciar</span>. El mosaico se irá formando aquí
                        a medida que se capturen las imágenes.
                    </p>
                </div>
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

// Se marca en cuanto llega el primer cuadro del recorrido. No basta con
// statusstreamimg: ese solo cubre el instante de la llamada que abre el stream, y
// entre pulsar Iniciar y recibir la primera imagen pasan varios segundos moviendo
// el motor, durante los cuales el <img> aún no tiene src.
const hasLiveFrame = ref<boolean>(false);

interface StitchedTour {
    idTour: number;
    nameFolder: string;
    stitchingUrl: string | null;
}

// El backend devuelve los recorridos del más reciente al más antiguo e indica cuál
// tiene ya su mosaico generado, así que basta con quedarse con el primero.
const lastStitchedTour = ref<StitchedTour | null>(null);

const lastStitchedUrl = computed(() =>
    lastStitchedTour.value?.stitchingUrl
        ? `${config.public.apiUrl}${lastStitchedTour.value.stitchingUrl}`
        : null
);

const loadLastStitchedTour = async () => {
    try {
        const tours = await $fetch<StitchedTour[]>(`${config.public.apiUrl}/api/Tour`);
        lastStitchedTour.value = (tours || []).find((tour) => !!tour.stitchingUrl) ?? null;
    } catch (error) {
        // Es solo el contenido de reposo del visor: si falla, se cae al mensaje
        // vacío en vez de molestar con una alerta.
        console.error('No se pudo cargar el último recorrido ensamblado:', error);
        lastStitchedTour.value = null;
    }
};

let hubConnection = await new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiUrl}/UpdateStatus`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .build();




onMounted( async () => {

  // No se espera: el visor en reposo es secundario y no debe retrasar el pintado
  // de los selectores de dispositivo y grupo.
  loadLastStitchedTour();

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
                if (imgRef.value) {
                    imgRef.value.src = `${item}`;
                    hasLiveFrame.value = true;
                }
            },
            complete: () => {
                console.log("Stream completed");
                // Al terminar el recorrido, su mosaico pasa a ser el más reciente:
                // se relee para que el visor en reposo muestre el que se acaba de tomar
                // y no el anterior.
                loadLastStitchedTour();
            },
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
