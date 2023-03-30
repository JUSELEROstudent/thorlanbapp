import { createStore } from 'vuex'

export default createStore({
  state: {
    message: '',
    type: ''
  },
  getters: {
  },
  mutations: {
    setMessage (state, message) {
      state.message = message
    },
    setType (state, type) {
      state.type = type
    }
  },
  actions: {
    showAlert ({ commit }, { message, type }) {
      console.log(message + '   ' + type)
      commit('setMessage', message)
      commit('setType', type)
    }
  },
  modules: {
  }
})
