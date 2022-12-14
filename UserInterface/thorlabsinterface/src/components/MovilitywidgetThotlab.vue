<template>
     <div style="width:50%; margin:auto;">
    <!--<div class="col">columna 1 </div><div class="col">cloumna 2 </div>
    </div>
    <div class="row">
    <div class="col">columna 1 </div><div class="col Primary">cloumna 2 </div><div class="col" id="confirmarconeccion">cloumna 3 </div>
    </div>
    <button v-on:click="respuest"> enviar mensajes </button>
    <button v-show="streamstate" v-on:click="sourceOpen"> test streaming </button>
    <button v-show="!streamstate" v-on:click="endstream">end stream </button>
    <video autoplay></video>
    <img v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen">
    <button type="button" class="btn btn-primary" >
    Launch demo modal 2
  </button> -->

<!-- Provides the application the proper gutter -->

  <!-- If using vue-router -->
  <v-card>
    <v-card-title>
      <span v-text="nombremio"></span>
      <v-img
        v-bind:src="'data:image/png;base64,' + imagen"
      ></v-img>
      <v-spacer></v-spacer>

    </v-card-title>

    <v-card-text>
      <v-btn v-show="streamstate" @click="sourceOpen">
        Iniciar
      </v-btn>
      <v-btn v-show="!streamstate" @click="endstream">
        Terminar Transmision
      </v-btn>
    </v-card-text>
  </v-card>

  </div>
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connection = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.36:45455/chatHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

const connectionsream = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.36:45455/StreamingHub', {
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
      streamstate: true
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
    sourceOpen: async function (evt: Event) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.streamstate = !this.streamstate
      if (!(connectionsream.state.toString() === 'Connected')) {
        await connectionsream.start().catch((err) => document.write(err + '{}{}{ EL ERROR}'))
      }
      console.log('se llama por evento handelr')
      // URL.revokeObjectURL(vidElement.src)
      const mime = 'video/mp4; codecs="avc1.42E01E, mp4a.40.2"'
      const mediaSource = evt.target
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
            // Document?.getElementById('ItemPreview').src = 'data:image/png;base64,' + item
            console.log('SE SUSCRIBE')
            // console.log('la respuesta es ' + item)
            console.log(connection.state)
          },
          complete: () => {
            const li = document.createElement('li')
            li.textContent = 'Stream completed'
            console.log('coneccion TERMINADA')
          },
          error: (err) => {
            const li = document.createElement('li')
            li.textContent = err
            console.log(err)
          }
        })
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
    this.coneccionhub()
    connectionsream.start().catch((err) => document.write(err))
  }
})
</script>
