<template>
      <v-alert v-if="messagge != ''" class="alert1"
        closable
        density="compact"
        :type="typeerrormsg"
        title="Ha sucedido algo inesperado"
        :text="messagge"
      ></v-alert>
</template>

<script>

export default {
  name: 'AlertsNew',
  data: function () {
    return {
      typeerrormsg: 'error',
      messagge: ''
    }
  },
  mounted: function () {
    this.$store.dispatch('showAlert', { message: 'funciono?', type: 'success' })// asi se debe llamar los store
    console.log(this.$store.state.message + ' - ' + this.$store.state.type)
    this.messagge = this.$store.state.message
    this.typeerrormsg = this.$store.state.type
    // console.log('PASO POR LA ALERTAAA')
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
</style>
