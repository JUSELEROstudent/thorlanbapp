<template>
  <h1>hola</h1>
   <streamto-all-view @hearerrors="(errors) =>hearerrorsf(errors)"></streamto-all-view>
   <div>
  <v-alert v-if="errorsmsj.state"
      outlined
      type="warning"
      prominent

    >
      {{errorsmsj.msjerror}}
    </v-alert>
  </div>
    <!-- <div v-if="errorsmsj.state"> se ve </div> -->
</template>

<script>
import StreamtoAllView from '@/views/StreamtoAllView.vue'
export default {
  name: 'ShareAll',
  components: { StreamtoAllView },
  import: { StreamtoAllView },
  data () {
    return {
      errorsmsj: {
        state: false,
        msjerror: 'por favor esperar a que el administrador inicie la transmicion'
      }
    }
  },
  methods: {
    hearerrorsf: function (errors) {
      console.log('ENTRAR AL EMITTT>>>>>>' + errors)
      this.errorsmsj.errors = errors
      this.errorsmsj.state = true
    }
  }
}
</script>
