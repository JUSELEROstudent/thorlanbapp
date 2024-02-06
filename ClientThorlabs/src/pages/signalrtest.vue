<script setup lang="ts">
import * as signalR from "@microsoft/signalr";
//import type drivemanualVue from "~/components/drivemanual.vue";
import DriveManual from './../components/driveManual.vue'

const config = useRuntimeConfig()

const currentCamera = ref<number>(0);

const imgRef = ref<HTMLImageElement | null>(null);
const listCameras = ref<{ cameraId: number; cameraName: string }[]>([]);
const isendrequest = ref<boolean>(false);

let hubConnection = await new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiUrl}/StreamingHub`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .build();


const handleCamera = async () => {
    try {
        // debugger;
        await hubConnection.stop();
        await hubConnection.start();
        hubConnection.stream("Counter", currentCamera.value, 10).subscribe({
            next: (item: string) => {
                if (imgRef.value) { imgRef.value.src = `data:image/png;base64,${item}` }
            },
            complete: () => { console.log("Stream completed"); },
            error: (err: Error) => { console.error(err); },
        });
    } catch (err) { console.error(err); }
}

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
            handleCamera();
        }
    } else {
        console.error(`Error en la solicitud: ${response.status} - ${response.statusText}`);
    }
} catch (error) {
    console.error('Error al realizar la solicitud:', error);
}
})


</script>

<template>
    <div class="flex flex-col w-full space-y-4 p-4">

        <div class="flex flex-row space-x-4 items-center px-4 bg-gray rounded p-1">

            <label style="color: white;" for="cameraSelect" class="space-x-2">
                <span>Select Camara</span>
                <Icon   name="material-symbols:android-camera" ></Icon>
            </label>

            <select id="cameraSelect" v-model="currentCamera" @change="handleCamera" class="select select-bordered w-full max-w-xs">
                <option :value="lc.cameraId" v-for="lc in listCameras">
                    {{ lc.cameraName }}
                </option>
            </select>

        </div>
        <div v-show="isendrequest" class=" flex justify-center bg-black " >
            <img  ref="imgRef">
        </div>
        <div v-show="!isendrequest" class="skeleton w-full h-48 space-y-4 p-4 bg-lightgray"> </div>
        <DriveManual />
    </div>
</template>
