import Vue from 'vue'
import Vuex from 'vuex'
import chats from './modules/chats'
import accounts from './modules/accounts'
import dialog from './modules/dialog'
import audio from './modules/audio'
import speech from './modules/speech'
import groups from './modules/groups'

Vue.use(Vuex)

export default new Vuex.Store({
  modules: {
    chats: chats,
    accounts: accounts,
    dialog: dialog,
    audio: audio,
    speech: speech,
    groups: groups,
  },
})
