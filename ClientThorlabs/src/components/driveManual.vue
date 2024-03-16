<template>

    <div class="flex flex-row min-h-72 w-full bg-gray rounded">
        <div class="flex flex-row bg-lightgray w-full m-2 rounded ">
            <div class="w-full pr-1" :class="{'bg-greenuis': !isKeyboardmode}">
                <div class="flex flex-col m-3 ">
                    <label class="m-auto" >
                    <input type="checkbox" class="checkbox checkbox-success align-middle"  id="test1ORM" v-model="modeselect"  value="manual" >
                    <span class="m-4 text-black">movimiento manual</span>              
                </label>
                </div>  
                    <label>  
                        <span class="text-black block w-full m-2">Selecione dispositivo :</span>
                        <div class="flex items-center"> 
                        <select  v-model="configdeviceselect.deviceId" class="ml-2 select select-bordered w-full ">
                            <option  v-for="device in listdevices" :key="device" required>{{ device }}</option>
                        </select>
                        <svg :class="{'text-white': !isKeyboardmode}" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Movimiento absoluto a :</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full ml-2" name="fname" v-model="configdeviceselect.moveto" :disabled="isKeyboardmode" >
                        <svg :class="{'text-white': !isKeyboardmode}" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Aceleracion de paso :</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full ml-2" name="fname" v-model="configdeviceselect.stepaceleration">
                        <svg :class="{'text-white': !isKeyboardmode}" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">Longitud de paso :</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full ml-2" name="fname" v-model="configdeviceselect.steprate" >
                        <svg :class="{'text-white': !isKeyboardmode}" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                    <label>
                        <span class="text-black block w-full m-2">seleccione el canal :</span>
                        <div class="flex  items-center">
                        <select  v-model="configdeviceselect.chaneltomove" class="ml-2 select select-bordered w-full " :disabled="isKeyboardmode" >
                            <option  v-for="chanel in devicechanels" :key="chanel" required>{{ chanel }}</option>
                        </select>
                        <svg :class="{'text-white': !isKeyboardmode}" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                        </div>
                    </label>
                <button class="btn btn-success bg-blue-900 self-center m-4 w-10/12 text-white" @click="MoveDeviceRemote()" :disabled="isKeyboardmode">MOVER</button>

            </div>
            <div class="w-full align-middle h-full" :class="{'bg-greenuis': isKeyboardmode}">
                <div class="flex flex-col  m-3">
                <label class="m-auto">
                    <input type="checkbox" class="checkbox align-middle checkbox-success "  id="test2"  v-model="modeselect" value="keyboard" >
                    <span class="m-4 text-black">Movimiento por teclado</span>
                </label>
                <img class="m-8" src="/awsd.png">
                <label>
                        <span class="text-black block w-full m-2">Paso relativo al tipear</span>
                        <div class="flex items-center">
                        <input type="number" class="input input-bordered w-full ml-2" name="fname" v-model="configdeviceselect.relativemoveto" :disabled="!isKeyboardmode" >
                        <svg :class="{'text-white': isKeyboardmode}"  xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 48 48"><path fill="currentColor" d="M24 4c11.046 0 20 8.954 20 20s-8.954 20-20 20S4 35.046 4 24S12.954 4 24 4m0 2.5C14.335 6.5 6.5 14.335 6.5 24S14.335 41.5 24 41.5S41.5 33.665 41.5 24S33.665 6.5 24 6.5m.25 25.5a1.5 1.5 0 1 1 0 3a1.5 1.5 0 0 1 0-3m0-19c3.365 0 6.25 2.882 6.25 6.249c.002 2.12-.769 3.47-2.678 5.528l-1.015 1.087c-1.023 1.139-1.428 1.861-1.466 2.715l-.003.162l-.006.128l-.018.124a1.25 1.25 0 0 1-2.476-.234c-.013-1.789.677-3.012 2.308-4.785l1.027-1.098c1.358-1.492 1.828-2.373 1.827-3.626c0-1.987-1.765-3.75-3.75-3.75c-1.92 0-3.636 1.654-3.744 3.559l-.012.319A1.25 1.25 0 0 1 18 19.25c0-3.365 2.886-6.25 6.25-6.25"/></svg>
                    </div>
                </label>
                <!-- <img src="/_nuxt/img2.jpg"> -->


                </div>
            </div>
        </div>


        <div  class="bg-lightgray w-full m-2 rounded">
            <div class="text-black p-2 "> 
                <div class="flex m-4">
                <span class="text-black mr-3"> Estado del dispositivo :    {{ infoKimFourchannel.deviceId }}</span>
                <span v-if="stateofpetition == 'loading'" class="loading loading-spinner loading-md"></span>
                <span v-if="stateofpetition == 'success'"><svg class="text-green-500" xmlns="http://www.w3.org/2000/svg" width="25" height="25" viewBox="0 0 32 32"><path fill="currentColor" d="M1 5.125A4.125 4.125 0 0 1 5.125 1h21.75A4.125 4.125 0 0 1 31 5.125v21.75A4.125 4.125 0 0 1 26.875 31H5.125A4.125 4.125 0 0 1 1 26.875zm11.183 17.444c.293.288.676.431 1.059.431c.383 0 .767-.143 1.059-.43l11.26-11.06a1.452 1.452 0 0 0 0-2.08a1.517 1.517 0 0 0-2.118 0L13.242 19.45l-4.685-4.602a1.517 1.517 0 0 0-2.118 0a1.453 1.453 0 0 0 0 2.08z"/></svg></span>
                <span v-if="stateofpetition == 'error'"><svg class="text-red-600" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M9.344 20q-.323 0-.628-.13q-.304-.132-.522-.349L4.48 15.806q-.217-.218-.348-.522Q4 14.979 4 14.656V9.344q0-.323.13-.628q.132-.304.349-.522L8.194 4.48q.218-.217.522-.348Q9.021 4 9.344 4h5.312q.323 0 .628.13q.304.132.522.349l3.715 3.715q.217.218.348.522q.131.305.131.628v5.312q0 .323-.13.628q-.132.304-.349.522l-3.715 3.715q-.218.217-.522.348q-.305.131-.628.131zM12 12.708l2.496 2.496q.14.14.344.15q.204.01.364-.15t.16-.354q0-.194-.16-.354L12.708 12l2.496-2.496q.14-.14.15-.344q.01-.204-.15-.364t-.354-.16q-.194 0-.354.16L12 11.292L9.504 8.796q-.14-.14-.344-.15q-.204-.01-.364.15t-.16.354q0 .194.16.354L11.292 12l-2.496 2.496q-.14.14-.15.344q-.01.204.15.364t.354.16q.194 0 .354-.16z"/></svg></span>
                </div>
              </div>
                    
            <div class="overflow-x-auto text-black">
            <table class="table bg-white ">
                <!-- head -->
                <thead>
                <tr class="text-black">
                    <th >Channel</th>
                    <th>Position</th>
                    <th>Acelertion</th>
                    <th>Steps</th>
                </tr>
                </thead>
                <tbody>
                <!-- row 1 -->
                <tr v-for="(channel,keys) in infoKimFourchannel.channelsInfo">
                    <th>{{keys + 1}}</th>
                    <td>{{channel.position}}</td>
                    <td>{{channel.aceleration}}</td>
                    <td>{{channel.rate}}</td>
                </tr>
                <!-- row 2 -->
                </tbody>
            </table>
            </div>
            <!-- {{ infoKimFourchannel }} -->
        </div>

    </div>
</template>

<script setup lang="ts">
import { alertsClient } from './../stores/alerts'
const alertStore = alertsClient()
const config = useRuntimeConfig()
interface InfoToManageDevice {
    deviceId: null | number,
    steprate: number,
    stepaceleration: number,
    moveto: number,
    chaneltomove: number,
    kindMovement: "relative" | "absolute",
    relativemoveto: number
}

// interface InfoKimFourChannels {  POSIBLEMENTE SEA NECESARIO PARA RECIBIR LOS VALORES DE LA API DEL ESTADO DEL DISPOSITIVO
//     deviceId:string,
//     channelsInfo: InfoSingleChannel[]

// }

// interface InfoSingleChannel{
//     position: number,
//     rate: number,
//     aceleration: number
// }

const modeselect = ref(["manual"]);
const isKeyboardmode = ref<boolean>(false);
const listdevices = ref([]);
const infoKimFourchannel = ref([]);
const devicechanels =ref([1,2,3,4]);
const stateofpetition = ref<string>();
const configdeviceselect = ref<InfoToManageDevice>({
    deviceId:0,
    steprate: 100,
    stepaceleration: 100,
    moveto: 500,
    chaneltomove: 1,
    kindMovement:  "absolute",
    relativemoveto: 50
});




onMounted( async () => { 
  getDevices();
  addEventListener("keydown",(event)=> OnEventListenerExecute(event))
});

watch(modeselect, async (newSelect, oldSelect) => {
    if(newSelect != null)
    {
        if (modeselect.value.length > 1 )
        {
            //Whenever you check a box in the view, its value is added to the end of an array. This action deletes the first value in the array, ensuring that only the most recent selection persists.
            modeselect.value.shift()
        }else if(newSelect.length == 0 )
        {
            modeselect.value = oldSelect
        }

        if (modeselect.value[0] == "keyboard") 
        {
            configdeviceselect.value.kindMovement = "relative"
            isKeyboardmode.value = true;
        }else 
        {
            configdeviceselect.value.kindMovement = "absolute"
            isKeyboardmode.value = false;
        }
    }
})

async function getDevices() {
  const myHeaders = new Headers();
  myHeaders.append('Content-Type', 'application/json')
  myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
  const requestOptions = {
    method: 'GET',
    headers: myHeaders,
  }
  const petition = await fetch( `${config.public.apiUrl}/home/devices`, requestOptions)
  const data = await petition.json()
  listdevices.value = data
  //console.log(data)
}

function MoveDeviceRemote(){
    // debugger
    MoveDevice(configdeviceselect.value);
}
async function MoveDevice(valuestomoved:InfoToManageDevice) {
      const myHeaders = new Headers()
      myHeaders.append('Content-Type', 'application/json')
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
      if(valuestomoved.deviceId!=null && typeof(valuestomoved.deviceId) == 'string'){
      const raw = JSON.stringify(valuestomoved)
      const requestOptions = {
        method: 'POST',
        headers: myHeaders,
        body: raw
      }
      try {
      stateofpetition.value= 'loading'
      const petition = await fetch( `${config.public.apiUrl}/movedevice`, requestOptions)
      const data = await petition.json()
      infoKimFourchannel.value =data;
      alertStore.NewAlert({type: 'OK',data:'Se movio con exito el dispositivo', tittle:'Moviemiento Exitoso'})
      stateofpetition.value= 'success'
      }
      catch{
        alertStore.NewAlert({type: 'error',data:'Fallo en la peticion al servidor', tittle:'Revisar conexiones '})
      }
      }
      else{
        alertStore.NewAlert({type: 'error',data:'verifique la configuracion  y la seleccion del dispositivo', tittle:'Revisar conexiones '})
      }

}

function GetAlert() {
  alertStore.NewAlert({type: 'OK',data:'estado de prueba ', tittle:'TITULO'})
}

function OnEventListenerExecute(evento:any) {
    if(modeselect.value[0] == "keyboard"){
        if(evento != null )
        {
            const lowerkey = evento.key.toString();
            //define current status of instance for form
            let movebykey: InfoToManageDevice =
            {
                deviceId: configdeviceselect.value.deviceId,
                steprate: configdeviceselect.value.steprate,
                stepaceleration: configdeviceselect.value.stepaceleration,
                moveto: configdeviceselect.value.moveto,
                chaneltomove: configdeviceselect.value.chaneltomove,
                kindMovement: configdeviceselect.value.kindMovement,
                relativemoveto: configdeviceselect.value.relativemoveto
            }
            //when you define the  chanels will change the chanels in the mesage
            if (lowerkey.toLowerCase() == "a") 
            {
                movebykey.chaneltomove =1
                movebykey.relativemoveto = movebykey.relativemoveto*(-1)
                MoveDevice(movebykey)
            }
            if (lowerkey.toLowerCase() == "d")
            {
                movebykey.chaneltomove =1
                movebykey.relativemoveto = movebykey.relativemoveto*(1)
                MoveDevice(movebykey)
            }
            if (lowerkey.toLowerCase() == "w")
            {
                movebykey.chaneltomove =2
                movebykey.relativemoveto = movebykey.relativemoveto*(1)
                MoveDevice(movebykey)
            }
            if (lowerkey.toLowerCase() == "s")
            {
                movebykey.chaneltomove =2
                movebykey.relativemoveto = movebykey.relativemoveto*(-1)
                MoveDevice(movebykey)
            }

        }

    }
}

</script>