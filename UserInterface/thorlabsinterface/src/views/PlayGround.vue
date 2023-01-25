<template>
  <v-card :elevation="5">
  <div class="body">
    <div class="d-flex">
  <v-form>
    <v-col cols="12">
    <v-card-title><h1>Move Manually</h1></v-card-title>
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
  </v-col>
  </v-form>
  <v-col>
    <div class="joystick">
      <table>
        <tr>
          <td colspan="3"><div> <div class=" row up">1</div></div> </td>
        </tr>
        <tr>
          <td ><div class="row left">1 </div> </td>
          <td><div class="center">0 </div></td>
          <td><div class="row right">1</div></td>
        </tr>
        <tr>
          <td colspan="3"><div class="row down">1</div></td>
        </tr>
      </table>
      <!-- <div class="up">1</div>
      <div class="left">1 </div>
      <div right="right">1</div>
      <div class="down">1</div> -->
    </div>
  </v-col>
  </div>
  <v-btn v-on:click="moverunpaso" v-if="onrequest" color="success"  large block>Move whit configuration</v-btn>
    <div   v-if="!onrequest" ><v-btn disabled>Move whit configuration</v-btn></div>
  </div>
  </v-card>
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
 .joystick {
  background:rgb(216, 214, 214);
  align-items: center;
 }

 table {
  width:100%;
  max-width: 500px;
  padding: 50px;
  margin: auto;
 }
.center {
  height: 100px;
  width: 100px;
  border-radius: 100px;
  background: black;
  border: 2px solid #ffffff ;
  margin: auto;
}
.left , .right , .down , .up {
  background: gray;
}
.left{
  rotate: -90deg;
}
.down{
  rotate: 180deg;
}
.right{
  rotate: 90deg;
}
.row{
  height: 80px ;
  width: 30px ;
  position: relative;
  margin: auto;
  z-index: 10;
  cursor: pointer;
}
.row::before{
  z-index: -2;
  position: absolute;
  height: 50px ;
  width: 50px ;
  background: gray;
  top: -24px;
  right: -10px;
  rotate: 45deg;
  border-top:2px solid #24724e;
  border-left:2px solid #ffffff;
  content: '';
}
.row::after{
  z-index: -1;
  position: absolute;
  height: 20px ;
  width: 80px ;
  background:rgb(216, 214, 214);
  top: 2px;
  right: -24px;
  content: '';
}

</style>
