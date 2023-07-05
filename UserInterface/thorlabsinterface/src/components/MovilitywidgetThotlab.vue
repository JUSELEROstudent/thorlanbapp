<template>
  <div class="streamcontainer">
      <img
      v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen"
      >
      <div class="panelbutton">
        <button v-show="streamstate" @click="sourceOpen">
          Iniciar
        </button>
        <button v-show="!streamstate" @click="endstream">
          Terminar Transmision
        </button>
      </div>
      <div v-if="errors.errorstate">
        <v-divider  inset
          >
        </v-divider>
        <!-- <v-alert
          outlined
          type="warning"
          prominent
        >
      {{errors.errorsmsj}}
    </v-alert> -->
      </div>
  </div>
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connection = new signalR.HubConnectionBuilder().withUrl('https://192.168.10.85:4040/chatHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

const connectionsream = new signalR.HubConnectionBuilder().withUrl('https://192.168.10.852:4040/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
// let vidElement: Element = document.querySelector('video')
// const vidElement = document.querySelector('video')
export default ({
  name: 'movilityWidget',
  data () {
    return {
      nombremio: 'Juan Sebastian Leon Rodriguez',
      listastreamin: [],
      imagen: '',
      streamstate: true,
      errors: { errorsmsj: 'Verificar que la aplicacin .net esta en ejecucion.', errorstate: false }
    }
  },
  methods: {
    coneccionhub: function () {
      console.log('funcion para la coneccion de hub')
      connection.start().catch((err) => document.write(err))
      console.log(connection.state)
      connection.on('ReceiveMessage', (username: string, message: string) => {
        console.log('se llamo de otro lado' + username + ' ' + message)
      })

      // Disable the send button until connection is established.
    },
    respuest: function () {
      connection.send('SendMessage', 'Mensaje predeterminado', 'segundo valor !"#"')
      console.log(connection.state)
    },
    actionbutton: function () {
      console.log('el boton se preciono')
    },
    endstream: function () {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
      this.streamstate = !this.streamstate
      connectionsream.stop()
    },
    errorhappen: function (errors: string) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      // this.streamstate = true
      this.errors.errorstate = true
      console.log(errors)
    },
    sourceOpen: async function (evt: Event) {
      try {
        await connectionsream.start().catch((err) => this.errorhappen(err.toString()))
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        this.streamstate = !this.streamstate
        if (!(connectionsream.state.toString() === 'Connected')) {
          await connectionsream.start().catch((err) => console.log(err + '{}{}{ EL ERROR}'))
        }
        // const sourceBuffer = mediaSource.addSourceBuffer(mime)
        await connectionsream.stream('Counter', 10, 10)
          .subscribe({
            next: (item) => {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
              this.imagen = ''
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              this.imagen = item
              console.log('SE SUSCRIBE')
              console.log(connection.state)
            },
            complete: () => {
              const li = document.createElement('li')
              li.textContent = 'Stream completed'
              console.log('coneccion TERMINADA')
            },
            error: (err) => {
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              this.$store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido unerror' })
            }
          })
      } catch (error) {
        console.log(error)
      }
    },
    coneccionstream: function () {
      const vidElement = document.querySelector('video')
      console.log('EL ESTADO ES ' + connectionsream.state)
      console.log(vidElement + 'ESTE ESTADO')
      const mediaSource = new MediaSource()
      // vidElement.src = URL?.createObjectURL(mediaSource)
      mediaSource.addEventListener('sourceopen', this.sourceOpen)
    }
  },
  mounted () {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    // this.coneccionhub()    ESTA LINIA INICIA LA CONEXION PARA SIGNALr DE STREAM STRING
    // connectionsream.start().catch((err) => document.write(err))
  }
})
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
button {
  border: 1px solid #000000;
  border-radius: 3px;
  background: #fff;
  padding: 5px;
  height: auto;
  width: auto;
}
</style>
