<template>
  <div class="streamcontainer">
      <img
      v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen"
      >
      <div class="videomanageframe" >
        <select name="cars" id="cars">
          <option v-for="camara in enablecamerasvar" :key="camara.cameraId" :value="camara.cameraId">{{camara.cameraName}}</option>
          <!-- <option value="2">dasdf</option> -->
        </select>
        <div class="playelement" v-show="streamstate" @click="sourceOpen">
          <span class="mdi mdi-48px mdi-play" title="Play"></span>
        </div>
        <div class="stopelement" v-show="!streamstate" @click="endstreams">
          <span class="mdi mdi-48px mdi-stop" title="Stop"></span>
        </div>
      </div>
      <!-- <div class="panelbutton">
        <button v-show="streamstate" @click="sourceOpen">
          Iniciar
        </button>
        <button v-show="!streamstate" @click="endstreams">
          Terminar Transmision
        </button>
      </div> -->
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
// const enablecameras: camaras = reactive([{ cameraId: 100, cameraName: 'cargando..' }])
// let enablecamerasvar: camaras[] = [{ cameraId: 100, cameraName: 'cargando..' }]

const connectionsreamall = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.37:4040/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

export default {
  name: 'StreamToAll',
  setup () {
    const enablecamerasvar = ref([{ cameraId: 100, cameraName: 'cargando..' }])
    const streamstate = ref(true)
    const imagen = ref('')

    onMounted(() => {
      Getenablecameras()
    })

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
        // await connectionsream.start().catch((err) => document.write(err))
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
        streamstate.value = !streamstate.value
        if (!(connectionsreamall.state.toString() === 'Connected')) {
          await connectionsreamall.start().catch((err) => errorhappen(err.toString()))
        }
        await connectionsreamall.stream('Counter', 10, 10)
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
            // no se hace nada en estos casos mas que mensajes de consola
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
              console.log('asdfasd KAJHDKLJHFASKDLJf' + err)
              store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido un error en la coneccion' })
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

      fetch('https://192.168.1.37:4040/home/cameras', requestOptions)
        .then(response => response.json())
        .then(data => setenablecameras(data))
        .catch(error => console.log('errror', error))
      // console.log(this.enablecameras[1])
    }
    function setenablecameras (response: camaras[]) {
      enablecamerasvar.value = response as camaras[]
    }

    return { enablecamerasvar, imagen, streamstate, endstreams, sourceOpen }
  },
  emits: ['hearerrors']
  // props: {
  //   indexcamera: [Number, Boolean]
  // },
  // data () {
  //   return {
  //     streamstate: true,
  //     imagen: ''
  //     // enablecameras: enablecamerasvar // chimbada de solucion
  //   }
  // },
  // methods: {
  //   endstreams: function () {
  //     // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //     // @ts-ignore
  //     this.streamstate = !this.streamstate
  //   },
  //   errorhappen: function (errors: string) {
  //     // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //     // @ts-ignore
  //     this.$emit('hearerrors', errors)
  //     // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //     // @ts-ignore
  //     this.streamstate = false
  //   },
  //   sourceOpen: async function (evt: Event) {
  //     try {
  //       // await connectionsream.start().catch((err) => document.write(err))
  //     // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //     // @ts-ignore
  //       this.streamstate = !this.streamstate
  //       if (!(connectionsreamall.state.toString() === 'Connected')) {
  //         await connectionsreamall.start().catch((err) => this.errorhappen(err.toString()))
  //       }
  //       await connectionsreamall.stream('Counter', 10, 10)
  //         .subscribe({
  //           next: (item) => {
  //           // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //           // @ts-ignore
  //             this.imagen = ''
  //             // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //             // @ts-ignore
  //             this.imagen = item
  //             console.log('SE SUSCRIBE')
  //             console.log(connectionsreamall.state)
  //           },
  //           complete: () => {
  //             // const li = document.createElement('li')
  //           },
  //           error: (err) => {
  //           // no se hace nada en estos casos mas que mensajes de consola
  //           // eslint-disable-next-line @typescript-eslint/ban-ts-comment
  //           // @ts-ignore
  //             this.$store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido un error' })
  //           }
  //         })
  //     } catch (error) {
  //       // console.log(error)
  //     }
  //   },
  //   Getenablecameras: function () {
  //     const myHeaders = new Headers()
  //     myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))

  //     const requestOptions = {
  //       method: 'GET',
  //       headers: myHeaders
  //     // ,
  //     // redirect: 'follow'
  //     }

  //     fetch('https://192.168.1.37:4040/home/cameras', requestOptions)
  //       .then(response => response.json())
  //       .then(data => this.setenablecameras(data))
  //       .catch(error => console.log('errror', error))
  //     // console.log(this.enablecameras[1])
  //   },
  //   setenablecameras: function (response: camaras[]) {
  //     enablecamerasvar = response as camaras[]
  //   }
  // },
  // mounted () {
  //   this.Getenablecameras()
  // }
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
</style>
