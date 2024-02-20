<template>
    <div class=" w-full flex-col w-full space-y-4 p-4 ">
        <div class="flex flex-row space-x-4 items-center px-4 bg-gray rounded p-1 w-full">

            <label style="color: white;" for="cameraSelect" class="space-x-2">
                <span>Select Camara</span>
                <Icon   name="material-symbols:android-camera" ></Icon>
            </label>

            <select id="cameraSelect" v-model="currentCamera" @change="handleCamera" class="select select-bordered w-full max-w-xs ">
                <option :value="lc.cameraId" v-for="lc in listCameras">
                    {{ lc.cameraName }}
                </option>
            </select>
             <div class="flex-1 " > 
                <button @click="InitStreamImg()" class="btn btn-primary float-end " :class="{'btn-disabled': statusstreamimg }">
                Iniciar 
                 </button>
            </div> 
            
        </div>
        <div>
            <div v-show="isendrequest" class=" flex justify-center bg-black max-h-screen" >
            <img  ref="imgRef">
        </div>    
        </div>
    </div>
</template>
<script setup lang="ts">
import * as signalR from "@microsoft/signalr";

const config = useRuntimeConfig();
const listCameras = ref<{ cameraId: number; cameraName: string }[]>([]);
const currentCamera = ref<number>(0);
const isendrequest = ref<boolean>(false);
const imgRef = ref<HTMLImageElement | null>(null);
const statusstreamimg = ref<boolean>(false);

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
      const response = await fetch(`${config.public.apiUrl}/Home/cameras`, requestOptions);
      if (response.ok) {
          const data = await response.json();
          listCameras.value = data;
          if (listCameras.value.length > 0) {
              currentCamera.value = data[0].cameraId;
              isendrequest.value= true;
              //handleCamera();
          }
      } else {
          console.error(`Error en la solicitud: ${response.status} - ${response.statusText}`);
      }
  } catch (error) {
      console.error('Error al realizar la solicitud:', error);
  }
  })

  const handleCamera = async () => {
    try {
        // debugger;
        await hubConnection.stop();
        await hubConnection.start();
        hubConnection.stream("Imgupdate", currentCamera.value).subscribe({
            next: (item: string) => {
                if (imgRef.value) { imgRef.value.src = `${item}` }
            },
            complete: () => { console.log("Stream completed"); },
            error: (err: Error) => { console.error(err); },
        });
    } catch (err) { console.error(err); }
}
async function InitStreamImg()
{
    statusstreamimg.value= true;
    const response = await handleCamera()
    statusstreamimg.value= false;


}

</script>
