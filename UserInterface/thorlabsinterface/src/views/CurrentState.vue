<template>
  <div class="d-md-flex">
<v-col cols="6" >
  <motor-state ></motor-state>
</v-col>
<v-col cols="6" >
  <motor-Mobility></motor-Mobility>
</v-col>
</div>
</template>

<script>
import MotorState from '@/components/MotorState.vue'
import MotorMobility from '@/views/PlayGround.vue'
export default {
  components: { MotorState, MotorMobility },
  name: 'currentState',
  import: { MotorState, MotorMobility }
}
</script>
