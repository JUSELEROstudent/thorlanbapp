<template>
  <v-card>
    <v-card-title>Dispositivo conectado</v-card-title>
    <v-col cols="4">
    <v-select
    v-model="currentPositionDevice.deviceId"
    :items="listdevices" required
    ></v-select>
  </v-col>
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
