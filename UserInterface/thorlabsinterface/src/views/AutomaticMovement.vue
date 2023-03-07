
  <!-- Crear la vista especial para el investigador se encargara de desarrollar el trasado de la preview y tambien de definir el sistema de
  barrido automatico que sera personalisable. -->
<template>
      <v-container fluid="true" class="bg-green">
        <v-row >
          <v-col class="bg-red"  cols="7">
            <current-status-view></current-status-view>
            <!-- <img class="resourceimg" src="https://localhost:7166/SouerceStaticFiles/campos.svg" alt="vista preeliminar" height="400"> -->
          </v-col>
          <v-col class="bg-yellow" cols="5">
            <play-ground>
            </play-ground>
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
      status2: ''
    }
  },
  methods: {
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
