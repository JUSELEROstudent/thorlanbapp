<template>
  <h1 v-if="testdata">este es el automatico</h1>
  <v-card>
    <v-card-title>Este es el titulo de la tarjeta</v-card-title>
  </v-card>
</template>
<script>
export default {
  app: 'AutomaticMovement',
  data () {
    return {
      testdata: true
    }
  }
}
</script>
