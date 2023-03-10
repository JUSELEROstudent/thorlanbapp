<template>
  <a :href=" Urlcurrentimg " Target="_blank">{{ Urlcurrentimg }}</a>
  <button @click="initconectionupdateimg">Iniciar</button>
  <!-- <img :src=Urlcurrentimg > -->

<figure class='zoom' v-on:mousemove=" zoom($event)" :style="{ backgroundImage: 'url(' + Urlcurrentimg + ')' }">
  <a :href=" Urlcurrentimg " Target="_blank">
  <img :src=Urlcurrentimg />
</a>
</figure>

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
      Urlcurrentimg: 'https://localhost:7166/SouerceStaticFiles/HxVmosaic.jpg',
      status2: ''
    }
  },
  methods: {
    zoom: function (e: Event) {
      const zoomer = e.currentTarget
      let offsetX
      let offsetY
      // console.log(e)
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      e.offsetX ? (offsetX = e.offsetX) : (offsetX = e.touches[0].pageX)
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      e.offsetY ? (offsetY = e.offsetY) : (offsetX = e.touches[0].pageX)
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      const x = (offsetX / zoomer.offsetWidth) * 100
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      const y = (offsetY / zoomer.offsetHeight) * 100
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      zoomer.style.backgroundPosition = x + '% ' + y + '%'
    },
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
.zoom {
  display: flex;
  margin: auto;
  background: violet;
}

figure.zoom {
  background-position: 50% 50%;
  position: relative;
  overflow: hidden;
  width: 50%;
  cursor: zoom-in;
}
figure.zoom img:hover {
  opacity: 0;
}
figure.zoom img {
  transition: opacity 0.5s;
  display: flex;
  margin: auto;
  max-height: 88vh;
}
</style>
