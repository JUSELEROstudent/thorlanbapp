import { defineStore } from 'pinia'

export interface ErrorItemAlert {
  type: 'error'|'warning'|'OK',
  data: string | null ,
  tittle: string
}

export const alertsClient = defineStore( 'listAlertsStore', () =>
  {
    const listAelerts = ref<ErrorItemAlert[]>([]);

    function NewAlert(newAlert: ErrorItemAlert) {
      listAelerts.value.push(newAlert)

      setTimeout(() => {
          // listAelerts.value.pop()
          listAelerts.value.splice(0,1)

      }, 2500);
    }

    return {listAelerts,NewAlert}
    
  }
)