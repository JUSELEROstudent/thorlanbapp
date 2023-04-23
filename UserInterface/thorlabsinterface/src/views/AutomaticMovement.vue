
  <!-- Crear la vista especial para el investigador se encargara de desarrollar el trasado de la preview y tambien de definir el sistema de
  barrido automatico que sera personalisable. -->
<template>
      <v-container fluid="true">
        <div>
          <v-row justify="end">
            <v-col cols="1" >
              <v-switch
                v-model="currentmode"
                hide-details
                value="picsmovemodule"
                :label="``"
              >
            </v-switch>
            </v-col>
          </v-row>
        </div>
        <v-row >
          <v-col   cols="7">
            <current-status-view @addcarrousel="addimgtostack" @hearerrors="startalert"></current-status-view>
            <img id="imagen-descarga" src="https://localhost:7166/SouerceStaticFiles/HxVmosaic.jpg" alt="Imagen de ejemplo" style="display: none;">
            <a :href=carousel[0] download="imgtest">
              descargar
            <!-- <button id="boton-descarga" @click="downloadimg">Descargar imagen</button> -->
            </a>
          </v-col>
          <v-col v-if="currentmode == 'picsmovemodule'" cols="5">
            <play-ground>
            </play-ground>
            <v-carousel>
              <v-carousel-item  v-for="img in carousel" :key="img"
                  :src=" img "
                ></v-carousel-item>
            </v-carousel>
          </v-col>
          <v-col v-else  cols="5">
            <streamto-all-view width="200"></streamto-all-view>
          </v-col>
        </v-row>
      </v-container>
      <v-divider></v-divider>
      <!-- <v-alert v-show="boolalert==true" class="alert1"
        closable
        density="compact"
        :type=alerttype
        title="Ha sucedido algo inesperado"
        :text="textalert"
      ></v-alert> -->
</template>

<script>
import StreamtoAllView from './StreamtoAllView.vue'
import PlayGround from './PlayGround.vue'
import CurrentStatusView from '../components/CurrentstatusView.vue'
import { warn } from 'vue'
import { ConsoleLogger } from '@microsoft/signalr/dist/esm/Utils'

export default {
  components: { StreamtoAllView, PlayGround, CurrentStatusView },
  app: 'AutomaticMovement',
  data () {
    return {
      testdata: true,
      status1: '',
      status2: 'error',
      alerttype: 'error',
      carousel: ['https://192.168.126.172:4040/SouerceStaticFiles/HxVmosaic.jpg'],
      currentmode: 'picsmovemodule',
      textalert: 'zvczvzc',
      boolalert: false
    }
  },
  watch: {
    currentmode: function (nuevovalor, anteriorvalor) {
      // if (this.alerttype === 'success') { this.alerttype = 'error' } else { this.alerttype = 'success' }
      // this.boolalert = true
      this.$store.dispatch('showAlert', { message: 'mensage emitido por el watcher', type: 'success', tittle: 'Se cambio de modo' })
      this.$forceUpdate()
      console.log(this.$store.state.message)
    }
  },
  methods: {
    downloadimg: function () {
      const url = this.carousel[0]
      // console.log(url)
      // fetch(url, { mode: 'no-cors' })
      //   .then(response => response.blob())
      //   .then(blob => {
      //     const blobUrl = window.URL.createObjectURL(blob)
      //     const a = document.createElement('a')
      //     a.download = url.replace(/^.*[\\\\/]/, '')
      //     a.href = blobUrl
      //     document.body.appendChild(a)
      //     a.click()
      //     a.remove()
      //   })
    },
    startalert: function (mesage) {
      this.$store.dispatch('showAlert', { message: mesage, type: 'warning', tittle: 'inicia el metodo Startalert' })
    },
    addimgtostack: function (stackedurl) {
      // var namejsonurl = 'img' + this.carousel.length
      this.carousel.push(stackedurl)
      this.$forceUpdate()
      console.log(stackedurl)
    },
    startmapping: function () {
      const requestOptions = {
        method: 'GET',
        redirect: 'follow'
      }
      this.status1 = 'ha sido enviado Mapping'
      fetch('https://localhost:7166/automotion/mapping', requestOptions)
        .then(response => response.text())
        .then(result => console.log('EXITO metodo Mapping' + result)).then(() => { this.$store.dispatch('showAlert', { message: 'El mappeado finalizo exitosamente', type: 'success', tittle: 'mapping a fallado' }) })
        .catch(error => this.$store.dispatch('showAlert', { message: error.toString, type: 'error', title: 'Ha fallado el mapeado' }))
    },
    startcalibrate: function () {
      const requestOptions = {
        method: 'GET',
        redirect: 'follow'
      }
      this.status2 = 'ha sido enviado Calibrate'
      fetch('https://localhost:7166/automotion/calibrate', requestOptions)
        .then(response => response.text())
        .then(result => console.log('EXITO metodo Calibrate' + result)).then(() => { this.status2 = 'ha terminado la peticion CALIBRATE' })
        .catch(error => console.log('error de el metodo Calibrate', error))
    }
  }
}
</script>

<style scoped>
.resourceimg {
display: flex;
margin: auto;
background: #8fc49c;
}
.alert1 {
  position: absolute;
  float: right;
  width: 30vw;
  left: 68vw;
  top: 98vh;
  z-index: 99;
}
</style>
