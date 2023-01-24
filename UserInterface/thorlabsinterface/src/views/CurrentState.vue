<template>
  <div class="d-md-flex d-block-sm">
<v-col  >
  <motor-state ></motor-state>
</v-col>
<v-col >
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
