// to manage speech synthesis
export default {
  namespaced: true,
  state: () => ({ 
    voices: [],               // a list of voices list available
    speechDialog: false,      // to show speech dialog  
    speech: '',               // string that needs to speech using speechsynthesis
    isTakingSpeech: false,    // to stop the speech at the top of the chat
  }),
  mutations: {
    // Set Speech Voices
    setVoices(state, voices) {
      state.voices = voices;
    },
    openSpeechDialog(state, speech) {
      state.speechDialog = true;
      state.speech = speech;
    },
    closeSpeechDialog(state) {
      state.speechDialog = false;
    },
    toggleTakingSpeech(state, boolean) {
      state.isTakingSpeech = boolean;
    },
  },
  actions: {
    openSpeechMessage(context, message) {
      // check if speechsynthesis is supported
      if (!"speechSynthesis" in window) {
        context.commit('dialog/showErrDialog', `Speech Synthesis is not supported in this browser.`, { root: true });
        return;
      };

      // open the dialog and set the speech message
      context.commit('openSpeechDialog', message);
    },
  }
}