<template>
  <div class=" w-full flex-col space-y-4 p-4 ">
    <div class="flex flex-row space-x-4 items-center px-4 bg-gray rounded p-1">


      <div class="flex-1">
        <button class="btn btn-success float-end"> GUARDAR</button>
      </div>

    </div>
    <div class="flex flex-row">
      <div class="bg-black w-9/12 h-[90vh]">ddd</div>
      <div class="bg-lightgray w-3/12 h-[90vh] border-l-greenuis border-2 p-4 ml-2 rounded">
        <label class="text-black p-5">
          Datasets por procesar
        </label>
        <div class="border-2 border-black m-2 rounded">
          <div class="flex flex-row border-b-2 pb-2 pt-2">
            <span class="w-1/12 text-black text-center">ID</span>
            <span class="w-2/6 text-black text-center">Date</span>
            <span class="w-2/6 text-black text-center">Name</span>
            <span class="w-1/12 text-black text-center">Pro.</span>
            <span class="w-1/6 text-black text-center">State</span>
          </div>
          <div class="flex flex-row " v-for="tour in Tours" :key="tour.idTour">
            <span class="w-1/12 truncate  text-center">{{ tour.idTour }}</span>
            <span class="w-2/6 truncate ">{{ tour.date }}</span>
            <span class="w-2/6 truncate ">{{ tour.nameFolder }}</span>
            <span class="w-1/6 truncate flex justify-center">
              <button v-if="tour.endStatus" class=" btn btn-xs justify-center mr-2"><svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 20 20"><path fill="white" d="M12 14a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-3a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-3a1 1 0 0 0-1-1zM5 9v3.5A2.5 2.5 0 0 0 7.5 15H11v-1H9.707l1.66-1.66c.235-.402.571-.738.973-.973L14 9.707V11h1V7.5A2.5 2.5 0 0 0 12.5 5H9v1h1.293l-1.66 1.66c-.235.402-.57.738-.973.973L6 10.293V9zm6.707-3h.793c.232 0 .45.052.647.146l-7 7A1.494 1.494 0 0 1 6 12.5v-.793zM7.5 14c-.232 0-.45-.053-.647-.146l7-7c.095.195.147.414.147.646v.794L8.293 14zM1 3a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1z"/></svg></button></span>
            <span class="w-1/12 truncate flex">
              <span v-if="tour.endStatus == 'succes'"><svg class="text-green-500" xmlns="http://www.w3.org/2000/svg" width="25" height="25" viewBox="0 0 32 32"><path fill="currentColor" d="M1 5.125A4.125 4.125 0 0 1 5.125 1h21.75A4.125 4.125 0 0 1 31 5.125v21.75A4.125 4.125 0 0 1 26.875 31H5.125A4.125 4.125 0 0 1 1 26.875zm11.183 17.444c.293.288.676.431 1.059.431c.383 0 .767-.143 1.059-.43l11.26-11.06a1.452 1.452 0 0 0 0-2.08a1.517 1.517 0 0 0-2.118 0L13.242 19.45l-4.685-4.602a1.517 1.517 0 0 0-2.118 0a1.453 1.453 0 0 0 0 2.08z"/></svg></span>
              <span v-if="tour.endStatus == null"><svg class="text-red-600" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M9.344 20q-.323 0-.628-.13q-.304-.132-.522-.349L4.48 15.806q-.217-.218-.348-.522Q4 14.979 4 14.656V9.344q0-.323.13-.628q.132-.304.349-.522L8.194 4.48q.218-.217.522-.348Q9.021 4 9.344 4h5.312q.323 0 .628.13q.304.132.522.349l3.715 3.715q.217.218.348.522q.131.305.131.628v5.312q0 .323-.13.628q-.132.304-.349.522l-3.715 3.715q-.218.217-.522.348q-.305.131-.628.131zM12 12.708l2.496 2.496q.14.14.344.15q.204.01.364-.15t.16-.354q0-.194-.16-.354L12.708 12l2.496-2.496q.14-.14.15-.344q.01-.204-.15-.364t-.354-.16q-.194 0-.354.16L12 11.292L9.504 8.796q-.14-.14-.344-.15q-.204-.01-.364.15t-.16.354q0 .194.16.354L11.292 12l-2.496 2.496q-.14.14-.15.344q-.01.204.15.364t.354.16q.194 0 .354-.16z"/></svg></span>
            </span>
            </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts" >
const config = useRuntimeConfig()

const Tours = ref<Tour[]>([]);

onMounted(async () => {
  try {
    const myHeaders = new Headers()
    myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))

    const requestOptions = {
      method: 'GET',
      headers: myHeaders,
      redirect: 'follow',
    }
    const response = await fetch(`${config.public.apiUrl}/api/Tour`, requestOptions);
    if (response.ok) {
      const data = await response.json();
      Tours.value= data;
    } else {
      console.error(`Error en la solicitud: ${response.status} - ${response.statusText}`);
    }
  } catch (error) {
    console.error('Error al realizar la solicitud:', error);
  }
})

interface Tour {
    idTour: number;
    date: string;
    nameFolder: string;
    numberX: number;
    numberY: number;
    numberZ: number;
    camera: string;
    endStatus: string;
    idStitching?: number | null;
    idUser?: number | null;
}


</script>