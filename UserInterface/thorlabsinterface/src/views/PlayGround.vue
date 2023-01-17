<template>
  <div class="body">
    <h1>Play ground para la vista Thorlabs</h1>
    <v-btn
    elevation="2"
  >Precionar para mover </v-btn>
  <v-form>
    <h1>Move Manually</h1>
    <v-select
    v-model="configdeviceselect.deviceId"
    :items="listdevices" required
    ></v-select>
    <v-text-field
        v-model="configdeviceselect.moveto"
        label="Site to move"
        required
    ></v-text-field>
    <v-text-field
        v-model="configdeviceselect.stepaceleration"
        label="Step Aceleration"
        required
    ></v-text-field>
    <v-text-field
        v-model="configdeviceselect.steprate"
        label="Step "
        required
    ></v-text-field>
    <v-select
    v-model="configdeviceselect.chaneltomove"
    :items="[ 1, 2, 3, 4]" required
    ></v-select>

  </v-form>
  <v-btn v-on:click="moverunpaso" v-if="onrequest">Move whit configuration</v-btn>
    <div   v-if="!onrequest" ><v-btn disabled>Move whit configuration</v-btn></div>
  </div>
</template>

<script>
export default {
  app: 'playground',
  data () {
    return {
      onrequest: true,
      listdevices: [],
      configdeviceselect: { deviceId: null, steprate: 100, stepaceleration: 100, moveto: 500, chaneltomove: 1 }
    }
  },
  methods: {
    moverunpaso: function () {
      this.onrequest = false
      const myHeaders = new Headers()
      myHeaders.append('Content-Type', 'application/json')
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
      const raw = JSON.stringify(this.configdeviceselect)
      const requestOptions = {
        method: 'POST',
        headers: myHeaders,
        body: raw
      }

      fetch('https://localhost:7166/movedevice', requestOptions)
        .then(response => response.json())
        .then(data => console.log(data))
        .then(this.onrequest = true)
        .catch(error => console.log('errror', error))
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
      headers: myHeaders
      // , redirect: 'follow'
    }

    fetch('https://localhost:7166/home/devices', requestOptions)
      .then(response => response.json())
      .then(data => this.listdeviceson(data))
      .catch(error => console.log('ERRRONRRRRRR', error))
  }
}
</script>

<style scoped>
.body {
  margin: auto;
  max-width: 50%;
}
</style>
