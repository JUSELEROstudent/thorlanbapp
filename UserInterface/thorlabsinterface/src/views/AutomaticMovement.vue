
  <!-- Crear la vista especial para el investigador se encargara de desarrollar el trasado de la preview y tambien de definir el sistema de
  barrido automatico que sera personalisable. -->
<template>
      <v-container fluid="true">
        <v-row >
          <v-col   cols="7">
            <current-status-view @addcarrousel="addimgtostack"></current-status-view>
          </v-col>
          <v-col cols="5">
            <play-ground>
            </play-ground>
            <v-carousel>
              <v-carousel-item  v-for="img in carousel" :key="img"
                  :src=" img "
                ></v-carousel-item>
            </v-carousel>
          </v-col>
        </v-row>
      </v-container>
      <v-divider> b</v-divider>
      <streamto-all-view width="200"></streamto-all-view>
</template>
<script>
import StreamtoAllView from './StreamtoAllView.vue'
import PlayGround from './PlayGround.vue'
import CurrentStatusView from '../components/CurrentstatusView.vue'

export default {
  components: { StreamtoAllView, PlayGround, CurrentStatusView },
  app: 'AutomaticMovement',
  data () {
    return {
      testdata: true,
      status1: '',
      status2: '',
      carousel: ['https://localhost:7166/SouerceStaticFiles/HxVmosaic.jpg']
    }
  },
  methods: {
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
        .then(result => console.log('EXITO metodo Mapping' + result)).then(() => { this.status1 = 'ha terminado la peticion MAPPING' })
        .catch(error => console.log('error de el metodo Mapping', error))
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
</style>
