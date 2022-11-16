<template>
  <div class="container" id="respuestadehub">se crea un estilo bootstrap 5
    <div class="row">
    <div class="col">columna 1 </div><div class="col">cloumna 2 </div>
    </div>
    <div class="row">
    <div class="col">columna 1 </div><div class="col Primary">cloumna 2 </div><div class="col" id="confirmarconeccion">cloumna 3 </div>
    </div>
    <button v-on:click="respuest"> enviar mensajes </button>
  </div>
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connection = new signalR.HubConnectionBuilder().withUrl('https://localhost:7166/chatHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

export default ({
  name: 'movilityWidget',
  data () {
    return {
      nombremio: ' adfdf'
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
    }
  },
  mounted () {
    this.coneccionhub()
  }
})
</script>
