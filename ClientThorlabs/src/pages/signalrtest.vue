<script setup lang="ts">
import * as signalR from "@microsoft/signalr";

const config = useRuntimeConfig()

const currentCamera = ref<number>(0);

const imgRef = ref<HTMLImageElement | null>(null);
const listCameras = ref<{ cameraId: number; cameraName: string }[]>([]);

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
            handleCamera();
        }
    } else {
        console.error(`Error en la solicitud: ${response.status} - ${response.statusText}`);
    }
} catch (error) {
    console.error('Error al realizar la solicitud:', error);
}

</script>

<template>
    <div class="flex flex-col w-full space-y-4 p-4">

        <div class="flex flex-row space-x-4 items-center px-4">

            <label for="cameraSelect" class="space-x-2">
                <span>Camara</span>
                <Icon class="h-8 w-8" name="material-symbols:android-camera"></Icon>
            </label>

            <select id="cameraSelect" v-model="currentCamera" @change="handleCamera" class="select select-bordered w-full max-w-xs">
                <option :value="lc.cameraId" v-for="lc in listCameras">
                    {{ lc.cameraName }}
                </option>
            </select>

        </div>
        <div class="flex justify-center bg-black ">
            <img  ref="imgRef">
        </div>
    </div>
</template>
