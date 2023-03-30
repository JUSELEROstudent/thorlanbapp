import { createStore } from 'vuex'

export default createStore({
  state: {
    message: '',
    type: '',
    tittle: '',
    alerts: [] as Array<{ msg: string, title: string, uuid: string, type: string }>
  },
  getters: {
  },
  mutations: {
    setAlert (state, alert: { msg: string, title: string, type: string }) {
      const random = Math.floor(Math.random() * 100000)
      const timestamp = Date.now().toString()
      state.alerts.push({ msg: alert.msg, title: alert.title, uuid: `${timestamp}-${random}`, type: alert.type })
      setTimeout(() => {
        const index = state.alerts.findIndex((a) => a.uuid === `${timestamp}-${random}`)
        if (index !== -1) {
          state.alerts.splice(index, 1)
        }
      }, 5000)
    },
    setMessage (state, message) {
      state.message = message
    },
    setType (state, type) {
      state.type = type
    },
    setTittle (state, tittle) {
      state.tittle = tittle
    }
  },
  actions: {
    showAlert ({ commit }, { message, type, tittle }) {
      // commit('setMessage', message)
      // commit('setType', type)
      // commit('setTittle', tittle)
      commit('setAlert', { msg: message, title: tittle, type: type })
    }
  },
  modules: {
  }
})
