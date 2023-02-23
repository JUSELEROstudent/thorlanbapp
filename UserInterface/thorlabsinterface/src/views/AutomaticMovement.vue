
  <!-- Crear la vista especial para el investigador se encargara de desarrollar el trasado de la preview y tambien de definir el sistema de
  barrido automatico que sera personalisable. -->
<template>
      <v-container class="bg-green">
       <v-row>
      <v-col class="bg-red" cols="9">
        <v-card >
          <v-card-title>Este es el titulo de la tarjeta</v-card-title>
          <v-btn @click="startmapping">Iniciar Mapeado</v-btn> <v-card-subtitle>{{ status1 }}</v-card-subtitle>
          <v-btn @click="startcalibrate">Calibrate</v-btn> <v-card-subtitle>{{ status2 }}</v-card-subtitle>
        </v-card>
      </v-col>
      <v-col cols="3">
       <v-row >
        <h1 v-if="testdata">este es el automatico</h1>
        <img src=" https://localhost:7166/SouerceStaticFiles/campos.svg" alt="">
       </v-row>
      </v-col>
        <!-- <h1 v-if="testdata">este es el automatico</h1>
        <img src=" https://localhost:7166/SouerceStaticFiles/campos.svg" alt=""> -->
       </v-row>
      <v-col class="bg-blue" cols="12">
       <v-row >
        <h1 v-if="testdata">este es el automatico</h1>
        <img src=" https://localhost:7166/SouerceStaticFiles/campos.svg" >
       </v-row>
      </v-col>
      </v-container>
</template>

<script>
export default {
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
