const state = {
  message: '',
  type: ''
}

const mutations = {
  setMessage (state, message) {
    state.message = message
  },
  setType (state, type) {
    state.type = type
  }
}

const actions = {
  showAlert ({ commit }, { message, type }) {
    commit('setMessage', message)
    commit('setType', type)
  },
  hideAlert ({ commit }) {
    commit('setMessage', '')
    commit('setType', '')
  }
}

export default {
  namespaced: true,
  state,
  mutations,
  actions
}
