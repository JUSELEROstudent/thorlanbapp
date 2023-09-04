<template>
  <div class="streamcontainer">
      <img
      v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen"
      >
      <div class="videomanageframe" >
        <span class="mdi mdi-20px mdi-camera-flip-outline"  v-show="streamstate" title="Select Camera"></span>
        <select id="selectcamera" v-model="currentcamera">
          <option v-for="camara in enablecamerasvar" :key="camara.cameraId" :value="camara">{{camara.cameraName}}</option>
          <i class="mdi mdi-20px mdi-camera-flip-outline" title="Select Camera"></i>
          <!-- <option value="2">dasdf</option> -->
        </select>
        <div class="playelement" v-show="streamstate" @click="sourceOpen">
          <span class="mdi mdi-48px mdi-play" title="Play"></span>
        </div>
        <div class="stopelement" v-show="!streamstate" @click="endstream">
          <span class="mdi mdi-48px mdi-stop" title="Stop"></span>
        </div>
      </div>
      <div v-if="errors.errorstate">
        <v-divider  inset
          >
        </v-divider>
      </div>
  </div>
</template>

<script  lang="ts">
import { defineComponent, ref, onMounted } from 'vue'
import store from '@/store'
import * as signalR from '@microsoft/signalr'
const connection = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.37:4040/chatHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

const connectionsream = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.37:4040/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
// let vidElement: Element = document.querySelector('video')
// const vidElement = document.querySelector('video')

interface camaras {
  cameraId: number,
  cameraName: string
}

export default {
  name: 'movilityWidget',
  setup () {
    const nombremio = ref('Juan Sebastian Leon Rodriguez')
    // const listastreamin = ref([{}])
    const imagen = ref('')
    const streamstate = ref(true)
    const errors = ref({ errorsmsj: 'Verificar que la aplicacin .net esta en ejecucion.', errorstate: false })
    const enablecamerasvar = ref([{ cameraId: 100, cameraName: 'cargando..' }])
    const currentcamera = ref({ cameraId: 100, cameraName: 'cargando..' })

    onMounted(() => {
      Getenablecameras()
    })

    function coneccionhub () {
      console.log('funcion para la coneccion de hub')
      connection.start().catch((err) => document.write(err))
      console.log(connection.state)
      connection.on('ReceiveMessage', (username: string, message: string) => {
        console.log('se llamo de otro lado' + username + ' ' + message)
      })

      // Disable the send button until connection is established.
    }
    function respuest () {
      connection.send('SendMessage', 'Mensaje predeterminado', 'segundo valor !"#"')
      console.log(connection.state)
    }
    function actionbutton () {
      console.log('el boton se preciono')
    }
    function endstream () {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
      streamstate.value = !streamstate.value
      connectionsream.stop()
    }
    function errorhappen (errors: string) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      // this.streamstate = true
      errors.value.errorstate = true
      console.log(errors)
    }
    async function sourceOpen (evt: Event) {
      try {
        await connectionsream.start().catch((err) => errorhappen(err.toString()))
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        streamstate.value = !streamstate.value
        if (!(connectionsream.state.toString() === 'Connected')) {
          await connectionsream.start().catch((err) => console.log(err + '{}{}{ EL ERROR}'))
        }
        // const sourceBuffer = mediaSource.addSourceBuffer(mime)
        await connectionsream.stream('Counter', currentcamera.value.cameraId, 10)
          .subscribe({
            next: (item) => {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
              imagen.value = ''
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              imagen.value = item
              console.log('SE SUSCRIBE')
              console.log(connection.state)
            },
            complete: () => {
              // const li = document.createElement('li')
              // li.textContent = 'Stream completed'
              // console.log('coneccion TERMINADA')
            },
            error: (err) => {
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido unerror' })
            }
          })
      } catch (error) {
        console.log(error)
      }
    }
    function coneccionstream () {
      const vidElement = document.querySelector('video')
      console.log('EL ESTADO ES ' + connectionsream.state)
      console.log(vidElement + 'ESTE ESTADO')
      const mediaSource = new MediaSource()
      // vidElement.src = URL?.createObjectURL(mediaSource)
      mediaSource.addEventListener('sourceopen', sourceOpen)
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
    return { coneccionstream, errors, enablecamerasvar, sourceOpen, endstream, streamstate, imagen, currentcamera }
  }
}

// export default ({
//   // name: 'movilityWidget',
//   // data () {
//   //   return {
//   //     // nombremio: 'Juan Sebastian Leon Rodriguez',
//   //     // listastreamin: [],
//   //     // imagen: '',
//   //     // streamstate: true,
//   //     // errors: { errorsmsj: 'Verificar que la aplicacin .net esta en ejecucion.', errorstate: false }
//   //   }
//   // },
//   methods: {
//     coneccionhub: function () {
//       console.log('funcion para la coneccion de hub')
//       connection.start().catch((err) => document.write(err))
//       console.log(connection.state)
//       connection.on('ReceiveMessage', (username: string, message: string) => {
//         console.log('se llamo de otro lado' + username + ' ' + message)
//       })

//       // Disable the send button until connection is established.
//     },
//     respuest: function () {
//       connection.send('SendMessage', 'Mensaje predeterminado', 'segundo valor !"#"')
//       console.log(connection.state)
//     },
//     actionbutton: function () {
//       console.log('el boton se preciono')
//     },
//     endstream: function () {
//       // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//     // @ts-ignore
//       this.streamstate = !this.streamstate
//       connectionsream.stop()
//     },
//     errorhappen: function (errors: string) {
//       // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//       // @ts-ignore
//       // this.streamstate = true
//       this.errors.errorstate = true
//       console.log(errors)
//     },
//     sourceOpen: async function (evt: Event) {
//       try {
//         await connectionsream.start().catch((err) => this.errorhappen(err.toString()))
//         // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//         // @ts-ignore
//         this.streamstate = !this.streamstate
//         if (!(connectionsream.state.toString() === 'Connected')) {
//           await connectionsream.start().catch((err) => console.log(err + '{}{}{ EL ERROR}'))
//         }
//         // const sourceBuffer = mediaSource.addSourceBuffer(mime)
//         await connectionsream.stream('Counter', 10, 10)
//           .subscribe({
//             next: (item) => {
//             // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//             // @ts-ignore
//               this.imagen = ''
//               // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//               // @ts-ignore
//               this.imagen = item
//               console.log('SE SUSCRIBE')
//               console.log(connection.state)
//             },
//             complete: () => {
//               const li = document.createElement('li')
//               li.textContent = 'Stream completed'
//               console.log('coneccion TERMINADA')
//             },
//             error: (err) => {
//               // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//               // @ts-ignore
//               this.$store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido unerror' })
//             }
//           })
//       } catch (error) {
//         console.log(error)
//       }
//     },
//     coneccionstream: function () {
//       const vidElement = document.querySelector('video')
//       console.log('EL ESTADO ES ' + connectionsream.state)
//       console.log(vidElement + 'ESTE ESTADO')
//       const mediaSource = new MediaSource()
//       // vidElement.src = URL?.createObjectURL(mediaSource)
//       mediaSource.addEventListener('sourceopen', this.sourceOpen)
//     }
//   },
//   mounted () {
//     // eslint-disable-next-line @typescript-eslint/ban-ts-comment
//     // @ts-ignore
//     // this.coneccionhub()    ESTA LINIA INICIA LA CONEXION PARA SIGNALr DE STREAM STRING
//     // connectionsream.start().catch((err) => document.write(err))
//   }
// })
</script>

<style scoped>
.streamcontainer{
  width:50%;
  margin:auto;
  background: red;
  min-height: 50PX;
  display: flex;
  border: 1px solid #000000;
  box-shadow: 0px 0px 20px 0px;
  border-radius: 5px;
  flex-direction: column;
}
.panelbutton{
  display: block;
  align-content: center;
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
.videomanageframe input,
.videomanageframe select{
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
}
</style>
