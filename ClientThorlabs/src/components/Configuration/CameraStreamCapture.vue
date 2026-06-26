<script setup lang="ts">
import * as signalR from "@microsoft/signalr";
import { useCapturesStore } from "~/stores/captures";

const config = useRuntimeConfig();

const currentCamera = ref<number>(0);
const listCameras = ref<{ cameraId: number; cameraName: string }[]>([]);
const savedCameraNames = ref<string[]>([]);
const isStreaming = ref<boolean>(false);
const lastFrameUrl = ref<string | null>(null);
const isLoadingCameras = ref<boolean>(false);
const capturesStore = useCapturesStore();
const maxPerPair = capturesStore.maxPerPair;
let streamSubscription: signalR.ISubscription<string> | null = null;

const hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${config.public.apiUrl}/StreamingHub`, {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
  })
  .build();

const stopStream = async () => {
  try {
    if (hubConnection.state === signalR.HubConnectionState.Connected) {
      await hubConnection.invoke("StopStream");
    }
    
    if (streamSubscription) {
      streamSubscription.dispose();
      streamSubscription = null;
    }
    
    if (hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      await hubConnection.stop();
    }
    
    isStreaming.value = false;
    lastFrameUrl.value = null;
    console.log("[CameraStreamCapture] Stream detenido, estado:", hubConnection.state);
  } catch (error) {
    console.error("[CameraStreamCapture] Error al detener stream:", error);
    isStreaming.value = false;
    lastFrameUrl.value = null;
  }
};

const startStream = async () => {
  try {
    if (!currentCamera.value) {
      return;
    }

    await stopStream();
    await hubConnection.start();
    isStreaming.value = true;

    streamSubscription?.dispose();
    streamSubscription = hubConnection
      .stream("Counter", currentCamera.value, 10, listCameras.value.find(c => c.cameraId === currentCamera.value)?.cameraName ?? "")
      .subscribe({
        next: (item: string) => {
          lastFrameUrl.value = `data:image/png;base64,${item}`;
        },
        complete: () => {
          console.log("Stream completed");
          isStreaming.value = false;
        },
        error: (err: Error) => {
          console.error(err);
          isStreaming.value = false;
        }
      });
  } catch (err) {
    console.error(err);
    isStreaming.value = false;
  }
};

const setSavedCameraNames = (names: string[]) => {
  savedCameraNames.value = names;
};

const fetchSavedCameras = async () => {
  try {
    const response = await $fetch(`${config.public.apiUrl}/api/camera`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json"
      }
    }) as { name: string }[]

    setSavedCameraNames(response?.map((item) => item.name) ?? [])
  } catch (error) {
    console.error("Error al obtener cámaras guardadas:", error)
    setSavedCameraNames([])
  }
}

const applyCameraList = async (data: { cameraId: number; cameraName: string }[]) => {
  listCameras.value = data.filter((camera) =>
    savedCameraNames.value.includes(camera.cameraName)
  );

  const hasCurrent = listCameras.value.some((camera) => camera.cameraId === currentCamera.value);
  if (!hasCurrent) {
    if (listCameras.value.length > 0) {
      currentCamera.value = listCameras.value[0].cameraId;
      await startStream();
    } else {
      currentCamera.value = 0;
      await stopStream();
    }
  }
};

const fetchHomeCameras = async () => {
  const myHeaders = new Headers();
  myHeaders.append("Authorization", "Bearer " + localStorage.getItem("stringjwt"));

  const response = await fetch(`${config.public.apiUrl}/Home/cameras`, {
    method: "GET",
    headers: myHeaders
  });

  if (response.ok) {
    const data = await response.json();
    await applyCameraList(data);
  } else {
    console.error(`Error en la solicitud: ${response.status} - ${response.statusText}`);
  }
};

const fetchCameras = async () => {
  try {
    isLoadingCameras.value = true;
    await fetchSavedCameras();
    await fetchHomeCameras();
  } catch (error) {
    console.error("Error al realizar la solicitud:", error);
  } finally {
    isLoadingCameras.value = false;
  }
};

const capturePhoto = () => {
  if (!lastFrameUrl.value) {
    return;
  }

  capturesStore.addImage(lastFrameUrl.value);
};

const removeImage = (pairIndex: number, imageIndex: number) => {
  capturesStore.removeImage(pairIndex, imageIndex);
};

const clearAll = () => {
  capturesStore.clearAll();
};

const nextSlotLabel = computed(() => {
  const lastPair = capturesStore.pairs[capturesStore.pairs.length - 1];
  const currentCount = lastPair?.images.length ?? 0;
  const slot = currentCount >= maxPerPair ? 1 : currentCount + 1;
  return `Tomar foto (${slot}/${maxPerPair})`;
});

const handleCameraConfigUpdated = async (event: Event) => {
  const detail = (event as CustomEvent<{ name: string }[]>).detail ?? [];
  setSavedCameraNames(detail.map((item) => item.name));
  await fetchHomeCameras();
};

onMounted(() => {
  fetchCameras();
  if (process.client) {
    window.addEventListener("camera-config-updated", handleCameraConfigUpdated as EventListener);
  }
});

onBeforeUnmount(async () => {
  if (process.client) {
    window.removeEventListener("camera-config-updated", handleCameraConfigUpdated as EventListener);
  }
  await stopStream();
});
</script>

<template>
  <div class="card bg-base-100 shadow-md">
    <div class="card-body space-y-4">
      <div class="flex flex-row items-center justify-between gap-3">
        <div>
          <h3 class="text-lg font-semibold">Streaming y captura</h3>
          <p class="text-sm text-gray-500">Captura imágenes en combos de 2 y guarda miniaturas.</p>
        </div>
        <div class="badge badge-outline">{{ isStreaming ? "Conectado" : "Desconectado" }}</div>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <label for="cameraSelect" class="flex items-center gap-2 text-sm font-medium">
          <Icon name="material-symbols:android-camera" />
          Cámara
        </label>
        <select
          id="cameraSelect"
          v-model="currentCamera"
          @change="startStream"
          class="select select-bordered w-full max-w-xs"
          :disabled="isLoadingCameras"
        >
          <option v-for="lc in listCameras" :key="lc.cameraId" :value="lc.cameraId">
            {{ lc.cameraName }}
          </option>
        </select>

        <button class="btn btn-sm btn-primary" @click="startStream" :disabled="!currentCamera || isStreaming">
          Conectar
        </button>
        <button class="btn btn-sm btn-error" @click="stopStream" :disabled="!isStreaming">
          Desconectar
        </button>
      </div>

      <div class="flex flex-col gap-3">
        <div class="flex justify-center rounded bg-black p-2" v-if="lastFrameUrl">
          <img :src="lastFrameUrl" class="max-h-64 rounded" />
        </div>
        <div v-else class="skeleton h-48 w-full rounded"></div>

        <div class="flex flex-wrap items-center gap-3">
          <button class="btn btn-success" @click="capturePhoto" :disabled="!lastFrameUrl">
            {{ nextSlotLabel }}
          </button>
          <button class="btn btn-ghost" @click="clearAll" :disabled="capturesStore.pairs.length === 0">
            Limpiar todo
          </button>
        </div>
      </div>

      <div v-if="capturesStore.pairs.length" class="space-y-4">
        <div
          v-for="(pair, pairIndex) in capturesStore.pairs"
          :key="pair.id"
          class="rounded border border-base-200 p-3"
        >
          <div class="mb-3 flex items-center justify-between">
            <h4 class="font-semibold">Combo {{ pairIndex + 1 }}</h4>
            <span class="text-xs text-gray-500">{{ pair.images.length }}/{{ maxPerPair }}</span>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div
              v-for="(image, imageIndex) in pair.images"
              :key="imageIndex"
              class="group relative overflow-hidden rounded bg-base-200"
            >
              <img :src="image" class="h-24 w-full object-cover" />
              <button
                class="btn btn-xs btn-error absolute right-2 top-2 opacity-0 transition group-hover:opacity-100"
                @click="removeImage(pairIndex, imageIndex)"
              >
                Quitar
              </button>
            </div>
            <div v-if="pair.images.length < maxPerPair" class="flex h-24 items-center justify-center rounded border border-dashed">
              <span class="text-xs text-gray-400">Espacio libre</span>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="text-sm text-gray-400">Aún no hay capturas guardadas.</div>
    </div>
  </div>
</template>
