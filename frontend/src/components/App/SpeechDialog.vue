<template>
  <v-dialog
    v-model="speechDialog"
    max-width="500"
    @click:outside="closeSpeechDialog()"
  >
    <v-card outlined>
      <v-card-title>
        Speech Message
      </v-card-title>
      <v-card-text>
        <v-select
          v-model="voiceLangSelect"
          :items="voices"
          :item-text="(v) => `${v.name} (${v.lang})`"
          item-value="lang"
          label="Voice Language"
        ></v-select>
        <v-slider
          class="mt-2"
          v-model="rate"
          label="Speed"
          thumb-label
          step="0.1"
          min="0.5"
          max="2.0"
        ></v-slider>
        <v-slider
          v-model="pitch"
          label="Pitch"
          thumb-label
          step="0.1"
          min="0.1"
          max="2.0"
        ></v-slider>
      </v-card-text>

      <v-card-actions>
        <v-spacer></v-spacer>

        <v-btn
          color="green darken-1"
          text
          @click="closeSpeechDialog()"
        >
          Close
        </v-btn>

        <v-btn
          color="green darken-1"
          text
          @click="speechMessage()"
        >
          Speak
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import { mapMutations } from 'vuex'

export default {
  name: 'SpeechDialog',
  data: () => ({
    voiceLangSelect: 'en-US',          // the voice that is selected by the user, default to en-US
    speechTimeout: null,               // to handle long speech
    rate: 1,                           // speechSynthesisVoice rate
    pitch: 1,                          // speechSynthesisVoice pitch
  }),
  computed: {
    voices: {
      get() { return this.$store.state.speech.voices; },
    },
    // speechDialog is bind with v-model, and we need a setter for it to change the variable in the store
    speechDialog: {
      get() { return this.$store.state.speech.speechDialog },
    },
    speech: {
      get() { return this.$store.state.speech.speech },
    }
  },
  created() {
    // prepare speech synthesis
    this.populateVoiceList();
    if (speechSynthesis.onvoiceschanged !== undefined) {
      speechSynthesis.onvoiceschanged = () => this.populateVoiceList();
    }
  },
  methods: {
    // Store Mutation ---------------------------------------------------------------------------
    ...mapMutations({
      closeSpeechDialog: 'speech/closeSpeechDialog',
    }),
    populateVoiceList() {
      // get all the voices
      let voices = speechSynthesis
        .getVoices()
        .sort(function (a, b) {
          const aname = a.name.toUpperCase(), bname = b.name.toUpperCase();
          if ( aname < bname ) return -1;
          else if ( aname == bname ) return 0;
          else return +1;
        });

      // set voices
      this.$store.commit('speech/setVoices', voices);
    },
    speechTimer() {
      // FOUND CHROME BUG IN THIS SPEECHSYNTHESIS
      // if using pause, any language can be proceed with desktop chrome, but all language cannot work in android.
      // if without pause, some language cannot be proceed with desktop chrome, but all language is able to work in android.
      // So here is the workaround, detect if the screensize is larger than mobile size, then include pause().
      // Disadvantage is if desktop is in mobile screen, then some language wont work
      if (window.innerWidth >= this.$vuetify.breakpoint.thresholds.sm) {
        speechSynthesis.pause();
      }
      speechSynthesis.resume();
      this.speechTimeout = setTimeout(() => this.speechTimer(), 10000);
    },
    speechMessage() {

      // first cancel any speech systhesis queue, if available
      speechSynthesis.cancel();

      // assign message to speech
      let utterThis = new SpeechSynthesisUtterance(this.speech);

      // start the timer whenever a speech start
      // the reason we loop the speech is because the speech can only persist 200-300 character
      // therefore we have to always resume the speech every round.
      utterThis.onstart = (e) => {
        this.$store.commit('speech/toggleTakingSpeech', true);
        this.speechTimer();
      };

      // end the speech and clear the timer
      utterThis.onend = (e) => {
        this.$store.commit('speech/toggleTakingSpeech', false);
        clearTimeout(this.speechTimeout);
      }

      // show error if cannot play the speech
      utterThis.onerror = (event) => {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured for speech.`);
      }

      // configure speech
      utterThis.voice = this.voices.find(v => v.lang == this.voiceLangSelect);
      utterThis.pitch = this.pitch;
      utterThis.rate = this.rate;
      utterThis.volume = 1;
      utterThis.lang = this.voiceLangSelect;

      // start speech
      speechSynthesis.speak(utterThis);
      
      // close the speech dialog
      this.closeSpeechDialog();
    },
  },
}
</script>

<style>

</style>