<template>

    <div class="flex flex-row min-h-72 w-full bg-gray rounded">
        <div class="flex flex-row bg-lightgray w-full m-2 rounded">
            <div class="w-full">
                <span>movimiento manual</span>
                <input type="checkbox" class="checkbox"  id="test1ORM" v-model="modeselect"  value="manual" >
                
            </div>
            <div class="w-full align-middle">
                <span>Movimiento por teclado</span>
                <input type="checkbox" class="checkbox"  id="test2"  v-model="modeselect" value="keyboard" >
                
            </div>
        </div>
        <div  class="bg-lightgray w-full m-2 rounded">
            test2
        </div>

    </div>
</template>

<script setup lang="ts">
const modeselect = ref([]);
    
const selectmode = ref<string>("manual");
function  SelectMovementType (modeselected: string) {
    if ( modeselected== "manual")
    {

    }
}

watch(modeselect, async (newSelect, oldSelect) => {
    if(newSelect != null)
    {
        debugger
        if (modeselect.value.length > 1 )
        {
            //any time you check a box in the view the value is set at the end of array this delete the first valuea and persist the most resent selection
            modeselect.value.shift()
        }
        // console.log( newSelect.length + "   " +oldSelect.length)
        // if (newSelect.length == 0 || oldSelect.length == 1)
        // {
        //     modeselect.value = oldSelect
        // }

    }
})


</script>