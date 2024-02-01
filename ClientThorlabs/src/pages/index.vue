<template>
    <div class=" w-full "> 
        <div>
            <button class="btn btn-primary" v-on:click="alertahome()"> click me</button>
        </div>
    </div>
</template>
<script setup lang="ts">
function alertahome(){
    alert("llego aleta")
}
</script>
