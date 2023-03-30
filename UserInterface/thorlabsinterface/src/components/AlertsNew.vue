<template>
  <v-alert closable v-if="viewstatus" density="compact" :type="type" :title="title" :text="msg" position="sticky">
  </v-alert>
</template>

<script>

export default {
  name: 'AlertsNew',
  props: ['title', 'msg', 'type'],
  data: function () {
    return {
      typeerrormsg: 'error',
      messagge: '',
      viewstatus: true
    }
  }
}
</script>

<style>
.alert {
  padding: 10px;
  border: 1px solid;
  margin-bottom: 10px;
}

.alert-success {
  background-color: lightgreen;
  border-color: green;
}

.alert-danger {
  background-color: lightcoral;
  border-color: red;
}

.alert1 {
  position: absolute;
  float: right;
  width: 30vw;
  left: 68vw;
  top: 90vh;
  z-index: 99;
}
</style>
