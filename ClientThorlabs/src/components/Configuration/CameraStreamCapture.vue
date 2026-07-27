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

// ── Indicador de enfoque ─────────────────────────────────────────
// El backend calcula la nitidez sobre el mismo cuadro que ya envía para el
// preview, así que no cuesta una captura extra. La escala no es absoluta: lo
// útil es maximizar el número girando el enfoque del microscopio, por eso se
// muestra también el mejor valor visto en la sesión como referencia.
const focusScore = ref<number | null>(null);
const focusThreshold = ref<number | null>(null);
const isFocusAcceptable = ref<boolean>(true);
const bestFocusSeen = ref<number>(0);

interface StreamFrame {
  frame: string;
  focus: number;
  focusThreshold: number | null;
  isFocusAcceptable: boolean;
}

let streamSubscription: signalR.ISubscription<StreamFrame> | null = null;

const focusPercent = computed(() => {
  if (focusScore.value === null || bestFocusSeen.value <= 0) return 0;
  return Math.min(100, Math.round((focusScore.value / bestFocusSeen.value) * 100));
});

const resetFocus = () => {
  focusScore.value = null;
  focusThreshold.value = null;
  isFocusAcceptable.value = true;
  bestFocusSeen.value = 0;
};

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
    resetFocus();
    console.log("[CameraStreamCapture] Stream detenido, estado:", hubConnection.state);
  } catch (error) {
    console.error("[CameraStreamCapture] Error al detener stream:", error);
    isStreaming.value = false;
    lastFrameUrl.value = null;
    resetFocus();
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
    // Se usa CounterWithMetrics en lugar de Counter para recibir además la medida
    // de enfoque de cada cuadro. Counter sigue existiendo sin cambios para las
    // otras vistas que lo consumen (index.vue y signalrtest.vue).
    streamSubscription = hubConnection
      .stream("CounterWithMetrics", currentCamera.value, 10, listCameras.value.find(c => c.cameraId === currentCamera.value)?.cameraName ?? "")
      .subscribe({
        next: (item: StreamFrame) => {
          lastFrameUrl.value = `data:image/png;base64,${item.frame}`;
          focusScore.value = item.focus;
          focusThreshold.value = item.focusThreshold ?? null;
          isFocusAcceptable.value = item.isFocusAcceptable;
          if (item.focus > bestFocusSeen.value) bestFocusSeen.value = item.focus;
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

        <!-- Nitidez en vivo: sirve para enfocar mirando un número en vez de a ojo.
             Gire el enfoque del microscopio buscando maximizar el valor. -->
        <div v-if="focusScore !== null" class="rounded border border-base-200 p-3 space-y-2">
          <div class="flex items-center justify-between gap-3">
            <span class="text-sm font-medium">Nitidez</span>
            <div class="flex items-center gap-2">
              <span class="font-mono text-sm">{{ focusScore.toFixed(1) }}</span>
              <span
                v-if="focusThreshold !== null"
                class="badge badge-xs"
                :class="isFocusAcceptable ? 'badge-success' : 'badge-error'"
              >
                {{ isFocusAcceptable ? 'Enfocada' : 'Desenfocada' }}
              </span>
            </div>
          </div>

          <progress
            class="progress w-full"
            :class="isFocusAcceptable ? 'progress-success' : 'progress-error'"
            :value="focusPercent"
            max="100"
          ></progress>

          <p class="text-xs text-gray-500">
            Máximo visto en esta sesión: {{ bestFocusSeen.toFixed(1) }}.
            <span v-if="focusThreshold !== null"> Umbral configurado: {{ focusThreshold.toFixed(1) }}.</span>
            <span v-else> Sin umbral definido para esta cámara.</span>
            La barra es relativa al máximo visto, no una escala absoluta.
          </p>
        </div>

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
