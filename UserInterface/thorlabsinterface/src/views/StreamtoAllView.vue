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
import { defineComponent } from 'vue'
import * as signalR from '@microsoft/signalr'
const connectionsreamall = new signalR.HubConnectionBuilder().withUrl('https://192.168.1.37:4040/StreamingHub', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()
export default defineComponent({
  name: 'StreamToAll',
  emits: ['hearerrors'],
  // props: {
  //   indexcamera: [Number, Boolean]
  // },
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
    errorhappen: function (errors: string) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.$emit('hearerrors', errors)
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.streamstate = false
    },
    sourceOpen: async function (evt: Event) {
      try {
        // await connectionsream.start().catch((err) => document.write(err))
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
        this.streamstate = !this.streamstate
        if (!(connectionsreamall.state.toString() === 'Connected')) {
          await connectionsreamall.start().catch((err) => this.errorhappen(err.toString()))
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
            // no se hace nada en estos casos mas que mensajes de consola
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
              this.$store.dispatch('showAlert', { message: err, type: 'error', tittle: 'ha sucedido un error' })
            }
          })
      } catch (error) {
        // console.log(error)
      }
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
