<template>
<div class="imglogingots ">
  <!-- <img class="w-full -z-10 absolute overflow-hidden" src="/GOTSoptica.jpg" >fdfg -->
  <slot></slot>
  <img class="max-w-52 max-h-20 bottom-10 right-10 absolute" src="/logouis.png" alt="">
</div>

</template>

<script>
</script>

<style>
.imglogingots {
  height: 100vh;
    background-image: url('/GOTSoptica.jpg'); /* Ruta de tu imagen */
    background-size: cover; /* Cubre todo el área del fondo */
    background-position: center; /* Centra la imagen */
    background-attachment: fixed;
}
</style>