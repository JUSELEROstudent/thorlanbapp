<template>
  <div class="container" id="respuestadehub">se crea un estilo bootstrap 5
    <div class="row">
    <div class="col">columna 1 </div><div class="col">cloumna 2 </div>
    </div>
    <div class="row">
    <div class="col">columna 1 </div><div class="col Primary">cloumna 2 </div><div class="col" id="confirmarconeccion">cloumna 3 </div>
    </div>
    <button v-on:click="respuest"> enviar mensajes </button>
    <button v-on:click="coneccionstream"> test streaming </button>

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

export default ({
  name: 'movilityWidget',
  data () {
    return {
      nombremio: ' adfdf',
      listastreamin: []
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
    coneccionstream: function () {
      console.log('EL ESTADO ES ' + connectionsream.state)
      connectionsream.stream('Counter', 10, 3000)
        .subscribe({
          next: (item) => {
            const li = document.createElement('li')
            li.textContent = item
            console.log('inicia lla conexion de streamin')
            console.log(connection.state)
            console.log(item)
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
