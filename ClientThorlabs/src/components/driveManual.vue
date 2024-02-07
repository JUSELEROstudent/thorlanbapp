<template>

    <div class="flex flex-row min-h-72 w-full bg-gray rounded">
        <div class="flex flex-row bg-lightgray w-full m-2 rounded">
            <div class="w-full">
                <span class="text-black m-4">movimiento manual</span>
                <input type="checkbox" class="checkbox align-middle"  id="test1ORM" v-model="modeselect"  value="manual" >
                    <label>  
                        <span class="text-black block w-full m-2">Selecione device</span>
                        <div class="flex items-center"> 
                        <select  v-model="configdeviceselect.deviceId" class="select select-bordered w-full max-w-xs">
                            <option  v-for="device in listdevices" :key="device" required>{{ device }}</option>
                        </select>
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Mover a posicion</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full" name="fname" v-model="configdeviceselect.moveto">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Aceleracion de paso</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full" name="fname" v-model="configdeviceselect.stepaceleration">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Longitud de paso</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full" name="fname" v-model="configdeviceselect.steprate">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">seleccione el canal</span>
                        <div class="flex  items-center">
                        <select  v-model="configdeviceselect.chaneltomove" class="select select-bordered w-full max-w-xs">
                            <option  v-for="chanel in devicechanels" :key="chanel" required>{{ chanel }}</option>
                        </select>
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
            </div>
            <div class="w-full align-middle">
                <span class="m-4 text-black">Movimiento por teclado</span>
                <input type="checkbox" class="checkbox align-middle "  id="test2"  v-model="modeselect" value="keyboard" >
                
            </div>
        </div>


        <div  class="bg-lightgray w-full m-2 rounded">
            test2
        </div>

    </div>
</template>

<script setup lang="ts">
//import  {ref , onMounted } from 'Vue'
interface InfoToManageDevice {
    deviceId: null | number,
    steprate: number,
    stepaceleration: number,
    moveto: number,
    chaneltomove: number
}
const modeselect = ref(["manual"]);
const listdevices = ref([1,2,3]);
const configdeviceselect = ref<InfoToManageDevice>({
    deviceId:0,
    steprate: 100,
    stepaceleration: 100,
    moveto: 500,
    chaneltomove: 1
});
const devicechanels =ref([1,2,3,4])


watch(modeselect, async (newSelect, oldSelect) => {
    if(newSelect != null)
    {
        if (modeselect.value.length > 1 )
        {
            //any time you check a box in the view the value is set at the end of array this delete the first valuea and persist the most resent selection
            modeselect.value.shift()
        }else if(newSelect.length == 0 )
        {
            modeselect.value = oldSelect
        }
    }
})

// function onMounted( () => {
//     console.log("adfasdfasdf")
// })

</script>