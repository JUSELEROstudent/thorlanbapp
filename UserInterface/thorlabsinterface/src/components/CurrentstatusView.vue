<template>
  <h1>current estatus</h1>
  <h5>{{ Urlcurrentimg }}</h5>
  <button @click="initconectionupdateimg">Iniciar</button>
  <img :src=Urlcurrentimg >
</template>

<script  lang="ts">
import * as signalR from '@microsoft/signalr'
const connectionsreamall = new signalR.HubConnectionBuilder().withUrl('https://localhost:7166/UpdateStatus', {
  skipNegotiation: true,
  transport: signalR.HttpTransportType.WebSockets
}).build()

export default {
  app: 'CurrentStatusView',
  data () {
    return {
      testdata: true,
      Urlcurrentimg: 'https://localhost:7166/SouerceStaticFiles/camtakedV1.jpg',
      status2: ''
    }
  },
  methods: {
    errorhappen: function (errors: string) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.$emit('hearerrors', errors)
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      this.streamstate = false
    },
    initconectionupdateimg: async function () {
      try {
        // await connectionsream.start().catch((err) => document.write(err))
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
        this.streamstate = !this.streamstate
        if (!(connectionsreamall.state.toString() === 'Connected')) {
          await connectionsreamall.start().catch((err) => this.errorhappen(err.toString()))
        }
        await connectionsreamall.stream('Imgupdate')
          .subscribe({
            next: (item) => {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
              this.Urlcurrentimg = ''
              // eslint-disable-next-line @typescript-eslint/ban-ts-comment
              // @ts-ignore
              this.Urlcurrentimg = item
              console.log(item)
              console.log(connectionsreamall.state)
            },
            complete: () => {
              const li = document.createElement('li')
            },
            error: (err) => {
              // no se hace nada en estos casos mas que mensajes de consola
              console.log(err)
            }
          })
      } catch (error) {
        // console.log(error)
      }
    }
  }
}
</script>

<style scoped>
img {
  width: 30%;
}
</style>
