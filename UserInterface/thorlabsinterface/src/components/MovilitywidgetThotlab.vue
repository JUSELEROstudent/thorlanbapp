<template>
  <div class="container" id="respuestadehub">se crea un estilo bootstrap 5
    <div class="row">
    <div class="col">columna 1 </div><div class="col">cloumna 2 </div>
    </div>
    <div class="row">
    <div class="col">columna 1 </div><div class="col Primary">cloumna 2 </div><div class="col" id="confirmarconeccion">cloumna 3 </div>
    </div>
    <button v-on:click="respuest"> enviar mensajes </button>
    <button v-show="streamstate" v-on:click="sourceOpen"> test streaming </button>
    <button v-show="!streamstate" v-on:click="endstream">end stream </button>
    <video autoplay></video>
    <img  v-bind:src="'data:image/png;base64,' + imagen">
  </div>
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connection = new signalR.HubConnectionBuilder().withUrl('https://localhost:7166/chatHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

const connectionsream = new signalR.HubConnectionBuilder().withUrl('https://localhost:7166/StreamingHub', {
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
      nombremio: ' adfdf',
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
            // @ts-igno
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
