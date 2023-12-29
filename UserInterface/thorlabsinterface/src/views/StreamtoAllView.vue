<template>
  <div class="streamcontainer">
      <img
      v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen"
      >
      <div :class="{videomanageframeON: !streamstate, videomanageframe: streamstate }">
      <div id="elementselectclick" :class="{comboselectcamera: streamstate}" @click="onclickselect" v-show="streamstate">
          <span class="mdi mdi-20px mdi-camera-flip-outline"  v-show="streamstate" title="Select Camera"></span>
          <select id="selectcamera" v-model="currentcamera">
            <option v-for="camara in enablecamerasvar" :key="camara.cameraId" :value="camara">{{camara.cameraName}}</option>
            <i class="mdi mdi-20px mdi-camera-flip-outline" title="Select Camera"></i>
            <!-- <option value="2">dasdf</option> -->
          </select>
      </div>
        <div class="playelement" v-show="streamstate" @click="sourceOpen">
          <span class="mdi mdi-48px mdi-play" title="Play"></span>
        </div>
        <div class="stopelement" v-show="!streamstate" @click="endstreams">
          <span class="mdi mdi-48px mdi-stop" title="Stop"></span>
        </div>
      </div>
  </div>
</template>

<script  lang="ts">
import { defineComponent, ref, onMounted } from 'vue'
import * as signalR from '@microsoft/signalr'
import store from '@/store'

interface camaras {
  cameraId: number,
  cameraName: string
}

const connectionsreamall = new signalR.HubConnectionBuilder().withUrl('https://192.168.10.116:4040/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

export default {
  name: 'StreamToAll',
  setup () {
    const enablecamerasvar = ref([{ cameraId: 1000, cameraName: 'cargando..' }]) // se ponen las camaras por defecto asumiendo que siempre existira una camara
    const streamstate = ref(true)
    const imagen = ref('')
    const currentcamera = ref({ cameraId: 1000, cameraName: 'cargando..' })

    onMounted(() => {
      Getenablecameras()
    })
    function onclickselect () {
      const elemnetselect = document.getElementById('selectcamera')
    }
    function endstreams () {
      streamstate.value = !streamstate.value
    }
    function errorhappen (errors: string) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.$emit('hearerrors', errors)
      streamstate.value = false
    }
    async function sourceOpen (evt: Event) {
      try {
        if (currentcamera.value.cameraId === 100) {
          store.dispatch('showAlert', { message: 'Favor elegir una camara ', type: 'error', tittle: 'es necesario seleccionar una camara para esta tarea.' })
        }
        // await connectionsream.start().catch((err) => document.write(err))
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        streamstate.value = !streamstate.value
        if (!(connectionsreamall.state.toString() === 'Connected')) {
          await connectionsreamall.start().catch((err) => store.dispatch('showAlert', { message: err.toString(), type: 'error', tittle: 'ha sucedido un error en la coneccion' }))
        }
        await connectionsreamall.stream('Counter', currentcamera.value.cameraId, 10)
          .subscribe({
            next: (item) => {
              imagen.value = ''
              imagen.value = item
              console.log('SE SUSCRIBE')
              console.log(connectionsreamall.state)
            },
            complete: () => {
              // const li = document.createElement('li')
            },
            error: (err) => {
              console.log('asdfasd KAJHDKLJHFASKDLJf' + err)
              store.dispatch('showAlert', { message: err.toString(), type: 'error', tittle: 'ha sucedido un error en la coneccion' })
            }
          })
      } catch (error) {
        // console.log(error)
      }
    }
    function Getenablecameras () {
      const myHeaders = new Headers()
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))

      const requestOptions = {
        method: 'GET',
        headers: myHeaders
        // ,
        // redirect: 'follow'
      }

      fetch('https://192.168.10.116:4040/home/cameras', requestOptions)
        .then(response => response.json())
        .then(data => setenablecameras(data))
        .catch(error => store.dispatch('showAlert', { message: error.toString(), type: 'error', tittle: 'Revisar la conexion al servidor' }))
      // console.log(this.enablecameras[1])
    }
    function setenablecameras (response: camaras[]) {
      enablecamerasvar.value = response as camaras[]
      if (enablecamerasvar.value.length < 1) {
        store.dispatch('showAlert', { message: 'No se reconocen camaras en el dispositivo', type: 'error', tittle: 'por favor verifique que se encuentra conectada' })
        return
      }
      currentcamera.value = enablecamerasvar.value[1]
    }

    return { enablecamerasvar, imagen, streamstate, currentcamera, endstreams, sourceOpen, onclickselect }
  },
  emits: ['hearerrors']
}

</script>

<style scoped>
.streamcontainer{
  min-width: 400px;
  min-height: 400px;
  width:50%;
  margin:auto;
  background: rgb(192, 255, 239);
  min-height: 50PX;
  display: flex;
  border: 1px solid #000000;
  box-shadow: 0px 0px 20px 0px;
  border-radius: 5px;
  flex-direction: column;
}
.videomanageframe{
  min-height: 400px;
  min-width: 400px;
  display: flex;
  /* align-items: center; */
}
.videomanageframeON{
  min-height: 0px;
  min-width: 0px;
  /* align-items: center; */
}
.playelement{
  width: auto;
  margin: auto;
  cursor: pointer;
  align-self: center;
}
.stopelement{
  cursor: pointer;
  align-self: flex-end;
}
img {height: 50%;}
.panelbutton{
  display: block;
  align-content: center;
}
button {
  border: 1px solid #000000;
  border-radius: 3px;
  background: #fff;
  padding: 5px;
  height: auto;
  width: auto;
}
#selectcamera {
  max-height: 50px;
}
.comboselectcamera {
  position: absolute;
}
.videomanageframe input,
.videomanageframe select{
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
}

</style>
