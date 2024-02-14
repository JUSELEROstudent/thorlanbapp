import { defineStore } from 'pinia'

export interface ErrorItemAlert {
  type: 'error'|'warning'|'OK',
  data: string,
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

      }, 2000);
    }

    return {listAelerts,NewAlert}
    
  }
)