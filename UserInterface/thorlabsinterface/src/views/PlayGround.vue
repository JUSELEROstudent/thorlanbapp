<template>
  <h1>Play ground para la vista Thorlabs</h1>
  <v-btn
  elevation="2" v-on:click="moverunpaso"
>Precionar para mover </v-btn>
<v-select
:v-model="configdeviceselect.nombre"
:items="listdevices"
></v-select>
</template>
<div>est estpacio es de {{configdeviceselect.nombre}}</div>
<script>
export default {
  app: 'playground',
  data () {
    return {
      iddispositivo: '',
      listdevices: [],
      configdeviceselect: [{ nombre: 'nda', apellido: 'soledo' }]
    }
  },
  methods: {
    moverunpaso: function () {
      console.log('SE A DADO UN PASO')
    },
    listdeviceson: function (data) {
      this.listdevices = data
    }
  },
  beforeCreate () {
    const myHeaders = new Headers()
    myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))

    const requestOptions = {
      method: 'GET',
      headers: myHeaders,
      redirect: 'follow'
    }

    fetch('https://localhost:7166/home/devices', requestOptions)
      .then(response => response.json())
      .then(data => this.listdeviceson(data))
      .catch(error => console.log('errror', error))
    console.log(this.listdevices + 'ESTA ES LA RESPUESTA ')
  }
}
</script>
