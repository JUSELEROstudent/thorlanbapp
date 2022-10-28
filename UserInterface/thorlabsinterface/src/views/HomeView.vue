<template>
  <div class="home">
    <img alt="Vue logo" src="../assets/logo.png">
    <login-user />
  </div>
</template>

<script lang="ts">
import { Options, Vue } from 'vue-class-component'
// import HelloWorld from '@/components/HelloWorld.vue' // @ is an alias to /src
import loginUser from '@/components/Login.vue'

@Options({
  components: {
    loginUser
  }
})
export default class HomeView extends Vue {}
</script>
