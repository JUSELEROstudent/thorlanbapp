<template>
  <v-card :elevation="5">
    <v-card-title><h1>Dispositivo Actual {{ currentPositionDevice.deviceId }}</h1></v-card-title>
  <v-card-title>Dispositivo Actualizado correctaemnte :
    <v-icon v-if="currentPositionDevice.status" title="OK" color="green" x-large >mdi-check-circle</v-icon>
    <v-icon v-if="!currentPositionDevice.status"  title="Error" color="red" x-large >mdi-cancel</v-icon>
  </v-card-title>
    <div class="d-md-flex d-xs-block">
      <v-col cols="8">
        <v-card-title>Seleccione el dispositivo</v-card-title>
      </v-col>
      <v-col cols="4">
        <v-select
        v-model="currentPositionDevice.deviceId"
        :items="listdevices" required
        ></v-select>
      </v-col>
    </div>
    <div class="d-md-flex  align-center">
      <v-col class="align-center">
        <v-icon title=":D" x-large >mdi-compass</v-icon>
      </v-col>
      <v-col >
        <v-card-title>Canal 1 :</v-card-title>
        <v-card-title>Canal 2 :</v-card-title>
        <v-card-title>Canal 3 :</v-card-title>
        <v-card-title>Canal 4 :</v-card-title>
      </v-col>
      <v-col >
        <v-card-item> {{ currentPositionDevice.channel1 }}</v-card-item>
        <v-card-item> {{ currentPositionDevice.channel2 }}</v-card-item>
        <v-card-item> {{ currentPositionDevice.channel3 }}</v-card-item>
        <v-card-item> {{ currentPositionDevice.channel4 }}</v-card-item>
      </v-col>
    </div>
    <v-btn color="success"  large block> Actualizar estado</v-btn>
  </v-card>
</template>

<script>

export default ({
  name: 'MotorState',
  data () {
    return {
      currentPositionDevice: {
        channel1: 0,
        channel2: 0,
        channel3: 0,
        channel4: 0,
        deviceId: 0,
        status: null,
        DeviceType: null
      },
      listdevices: []
    }
  }
})
</script>
