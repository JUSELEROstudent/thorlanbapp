<template>
  <div class="streamcontainer">
      <img
      v-show="!streamstate" v-bind:src="'data:image/png;base64,' + imagen"
      >
      <div class="panelbutton">
        <button v-show="streamstate" @click="sourceOpen">
          Iniciar
        </button>
        <button v-show="!streamstate" @click="endstreams">
          Terminar Transmision
        </button>
      </div>
  </div>
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connectionsreamall = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.36:45455/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()
export default ({
  name: 'StreamToAll',
  data () {
    return {
      streamstate: true,
      imagen: ''
    }
  },
  methods: {
    endstreams: function () {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.streamstate = !this.streamstate
    },
    sourceOpen: async function (evt: Event) {
      // await connectionsream.start().catch((err) => document.write(err))
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.streamstate = !this.streamstate
      if (!(connectionsreamall.state.toString() === 'Connected')) {
        await connectionsreamall.start().catch((err) => document.write(err + '{}{}{ EL ERROR}'))
      }
      await connectionsreamall.stream('Counter', 10, 10)
        .subscribe({
          next: (item) => {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            this.imagen = ''
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            this.imagen = item
            console.log('SE SUSCRIBE')
            console.log(connectionsreamall.state)
          },
          complete: () => {
            const li = document.createElement('li')
          },
          error: (err) => {
            const li = document.createElement('li')
            // no se hace nada en estos casos mas que mensajes de consola
            console.log(err)
          }
        })
    }
  }
})
</script>

<style scoped>
.streamcontainer{
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
