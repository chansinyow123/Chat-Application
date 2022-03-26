<template>
  <div>
    <div class="panel pa-2">
      <div class="d-flex align-end" v-if="!isRecording">
        <v-menu
          @input="$store.commit('dialog/toggleOverlay', $event)"
          origin="bottom left"
          transition="scale-transition"
          min-width="150px"
        >
          <template v-slot:activator="{ on, attrs }">
            <v-btn
              icon
              dark
              v-bind="attrs"
              v-on="on"
            >
              <v-icon>mdi-paperclip</v-icon>
            </v-btn>
          </template>
          
          <v-list class="pa-0">
            <v-list-item @click="$refs.file.click()">
              <input type="file" ref="file" class="d-none" @change="hitFilesBtn($event)" :accept="$fileExtension.join()" multiple>
              <v-list-item-title>Upload Files</v-list-item-title>
            </v-list-item>
            <v-list-item @click="sendLocation()">
              <v-list-item-title>Send Current Location</v-list-item-title>
            </v-list-item>
            <v-list-item @click="sendContacts()">
              <v-list-item-title>Send Contacts</v-list-item-title>
            </v-list-item>
            <v-list-item @click="startSpeechRecognition()">
              <v-list-item-title>Speech-to-Text</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>

        <v-textarea
          dark
          hide-details
          id="textarea"
          label="Type a message"
          placeholder="Type a message"
          single-line
          outlined
          dense
          auto-grow
          rows="1"
          class="px-1 align-self-end"
          ref="textarea"
          v-model.lazy.trim="textarea"
          maxlength="4096"
          @keydown.enter.exact="hitEnter($event)"
          @paste="pasteFiles($event.clipboardData.files)"
          :autofocus="$vuetify.breakpoint.lgAndUp"
        >
        </v-textarea>

        <v-btn icon dark v-show="textarea" @click="hitSendBtn()">
          <v-icon>mdi-send</v-icon>
        </v-btn>

        <v-btn icon dark v-show="!textarea" @click="recordAudio()">
          <v-icon>mdi-microphone</v-icon>
        </v-btn>
      </div>

      <div v-else-if="isLoadingRecord" class="d-flex align-center">
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
        <span class="pl-1"> Getting Audio Input... </span>
      </div>

      <div class="d-flex justify-space-between" v-else>
        <div class="d-flex align-center">
          Recording: 
        </div>
        <div class="d-flex align-center">
          <v-btn icon dark @click="closeAudio()">
            <v-icon>mdi-close-circle-outline</v-icon>
          </v-btn>

          <div class="text-h6 px-3">
            {{ minutes }} : {{ (seconds).toLocaleString('en-US', {minimumIntegerDigits: 2, useGrouping:false}) }}
          </div>

          <v-btn icon dark @click="sendAudio()">
            <v-icon>mdi-send</v-icon>
          </v-btn>
        </div>
      </div>
    </div>

    <v-dialog
      v-model="recognitionDialog"
      max-width="500"
      @click:outside="recognition.stop()"
    >
      <v-card outlined>
        <v-card-title>
          Speech-to-Text
        </v-card-title>

        <v-card-text>
          <v-divider></v-divider>
          <v-select
            v-model="voiceLangSelect"
            :items="voices"
            :item-text="(v) => `${v.name} (${v.lang})`"
            item-value="lang"
            label="Voice Languauge"
            class="mt-2"
            @change="changeRecognitionLang()"
          ></v-select>
          <div class="mt-2">
            <div v-if="!finalTranscript && !interimTranscript">(Speak now!)</div>
            <div v-else>
              <span class="text--primary">{{ finalTranscript }}</span>
              <span class="text--disabled">{{ interimTranscript }}</span>
            </div>
          </div>
        </v-card-text>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn
            color="green darken-1"
            text
            @click="recognition.stop()"
          >
            Stop
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script>
export default {
  name: 'TextArea',
  data: () => ({
    textarea: '',                      // textarea v-model
    // audio
    isRecording: false,                // to show record audio
    isLoadingRecord: true,             // to show loading state for obtaining audio device
    minutes: 0,                        // to display minutes during audio recoding
    seconds: 0,                        // to display seconds during audio recoding
    timerInterval: null,               // start the timer
    audioStream: null,                 // store audioStream
    mediaRecorder: null,               // to put audio inside the mediaRecorder
    recordedChunks: [],                // store array of audio
    // speech recognition
    voiceLangSelect: 'en-US',          // the voice that is selected by the user, default to en-US
    recognition: null,                 // the speechRecognition object, if available
    recognitionDialog: false,          // the model dialog to show the text when recording speech
    finalTranscript: '',               // the final text that has recorded on the speech
    interimTranscript: '',             // the recording text that has recorded on the speech
    changeLanguage: false,             // to detect language change in speechRecognition 'onend' listener
  }),
  computed: {
    voices: {
      get() { return this.$store.state.speech.voices; }
    }
  },
  created() {
    // close the recording, if available
    window.addEventListener('blur', this.closeAllRecording);

    // prepare speechrecognition variable, if supported
    if ("webkitSpeechRecognition" in window) {

      // declare and configure speech recognition
      let recognition = new webkitSpeechRecognition();
      recognition.continuous = true;
      recognition.lang = 'en-US';
      recognition.interimResults = true;
      recognition.maxAlternatives = 1;

      // onstart event
      recognition.onstart = () => {
        // open the model dialog and reset finalTranscript
        this.recognitionDialog = true;
        this.finalTranscript = "";
      }

      // onend event
      recognition.onend = () => {
        // close the model dialog and add finalTranscript to textarea
        this.recognitionDialog = false;
        this.textarea += this.finalTranscript;
        // if it was a language change, then start the speechRecognition again
        // then assign changeLanguage to false
        if (this.changeLanguage) {
          recognition.start();
          this.changeLanguage = false;
        }
      }

      // on error event
      recognition.onerror = (e) => {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured: ${e.error}.`);
      }

      // when getting the speech result from the recording
      recognition.onresult = (e) => {

        // reset the interimTranscript
        this.interimTranscript = "";

        // Loop through the results from the speech recognition object.
        for (let i = e.resultIndex; i < e.results.length; i++) {
          // If the result item isfinal, add it to finalTranscript, else add it to interimTranscript
          if (e.results[i].isFinal) {
            this.finalTranscript += event.results[i][0].transcript;
          } else {
            this.interimTranscript += event.results[i][0].transcript;
          }
        }
      }

      // assign recognition object
      this.recognition = recognition;
    }
  },
  beforeDestroy() {
    // remove window eventlistener
    window.removeEventListener('blur', this.closeAllRecording);
  },
  methods: {
    closeAllRecording() {
      // close all the audio, if avaialble
      this.closeAudio();

      // stop the speech recognition, if opening
      if (this.recognition) {
        this.recognition.stop();
      }
    },
    closeAudio() {
      // remove any listener on this mediaRecoder, if available
      if (this.mediaRecorder) {
        this.removeMediaRecorderEvent();
        this.mediaRecorder.stop();
      }

      // reset variable and stop audio Stream
      this.resetAudioVariable();
    },
    resetAudioVariable() {
      // clear timer interval
      clearInterval(this.timerInterval);

      // back to textarea input
      this.isRecording = false;

      // stop all the audioStream, if available
      if (this.audioStream) {
        this.audioStream.getTracks().forEach((track) => {
          track.stop();
        });
      }

      // reset audioStream and mediaRecorder back to null
      this.audioStream = null;
      this.mediaRecorder = null;
    },
    removeMediaRecorderEvent() {
      // to remove all event listener for mediaRecorder
      this.mediaRecorder.removeEventListener('dataavailable', this.audioDataAvailable);
      this.mediaRecorder.removeEventListener('stop', this.audioStop);
    },
    async recordAudio() {

      // vibrate the device
      navigator.vibrate(100);

      // show audio record loading state
      this.isLoadingRecord = true;
      
      try {
        
        // get the audio stream from microphone
        this.audioStream = await navigator.mediaDevices.getUserMedia({ video: false, audio: true });

        // prepare / reset mediaRecorder
        this.recordedChunks = [];
        this.mediaRecorder = new MediaRecorder(this.audioStream);

        // call this method for storing audio input
        this.mediaRecorder.addEventListener('dataavailable', this.audioDataAvailable);

        // call this method when stop
        this.mediaRecorder.addEventListener('stop', this.audioStop);

        // start the mediaRecorder
        this.mediaRecorder.start();

        // reset all value
        // and remove audio record loading state
        this.isRecording = true;
        this.minutes = 0;
        this.seconds = 0;
        this.isLoadingRecord = false;

        // start the timer
        this.timerInterval = setInterval(() => {
          // increment timer
          this.seconds++;
          if (this.seconds >= 60) {
            this.seconds = 0;
            this.minutes++;
          }
          // if audio exceed maximum minute, stop the recording
          let maximumMin = 5;
          if (this.minutes >= maximumMin) {
            this.closeAudio();
            this.$store.commit('dialog/showErrDialog', `Audio recording can only be within ${maximumMin} minute.`);
          }
        }, 1000);

      } catch(err) {

        // show error message
        switch(err.name) {
          case 'NotAllowedError':
            this.$store.commit('dialog/showErrDialog', 'Audio recording permission denied.');
            break;
          case 'NotReadableError':
            this.$store.commit('dialog/showErrDialog', 'Audio input not readable. Try switch to other audio recording device.');
            break;
          case 'NotFoundError':
            this.$store.commit('dialog/showErrDialog', 'Audio recording device not found.');
            break;
          default:
            this.$store.commit('dialog/showErrDialog', `Unexpected Error Occur: ${err.name}`);
            console.log(err);
        }

        // close all audio
        this.closeAudio();
      }
    },
    audioDataAvailable(e) {
      if (e.data.size > 0) this.recordedChunks.push(e.data);
    },
    audioStop() {
      // send the audio
      let blob = new Blob(this.recordedChunks);
      let file = new File([blob], "voice_chat.mp3", { type: "audio/mp3" });
      
      // remove all eventlistener for media recorder
      this.removeMediaRecorderEvent();

      // reset variable and display back to textarea input
      this.resetAudioVariable();

      // send audio file
      this.$emit('send-message', {
        textarea: '',
        file: file,
        location: null,
      });
    },
    sendAudio() {
      // if the recording is below 1 seconds, do nothing
      // this is to ensure that the audio is at least 1 second above
      if (this.minutes == 0 && this.seconds < 1) return;

      // stop the mediaRecorder to triger stop event listener
      this.mediaRecorder.stop();
    },
    hitFilesBtn(e) {
      // get the files
      let files = e.target.files;

      // upload file
      this.$emit('upload-files', files);
      
      // reset the file input value for onchange to work another time
      this.$refs.file.value = null;
    },
    sendLocation() {

      // if geolocation IS NOT available, display error and do nothing
      if (!'geolocation' in navigator) {
        this.$store.commit('dialog/showErrDialog', 'Geolocation is not supported in this browser.');
        return;
      }

      // get location
      navigator.geolocation.getCurrentPosition((position) => {

        let location = {
          lat: position.coords.latitude,
          lng: position.coords.longitude
        }

        let json = JSON.stringify(location);
        this.$emit('send-message', {
          textarea: '',
          file: null,
          location: json
        });

      }, (error) => {
        switch (error.code) {
          case 1:
            this.$store.commit('dialog/showErrDialog', 'Getting geolocation permission denied.');
            break;
          case 2:
            this.$store.commit('dialog/showErrDialog', 'Unable to get the location from your device.');
            break;
          case 3:
            this.$store.commit('dialog/showErrDialog', 'Getting location timeout.');
            break;
          default:
            this.$store.commit('dialog/showErrDialog', 'Unexpected error occured.')
        }
      });
    },
    async sendContacts() {
      // check if contact api is supported in this browser
      const supported = ('contacts' in navigator && 'ContactsManager' in window);
      if (!supported) {
        this.$store.commit('dialog/showErrDialog', `The Contact API is only supported in Android Phone.`);
        return;
      }
      
      // only get name, email and telephone
      // multiple contacts can be selected
      const props = ['name', 'email', 'tel'];
      const opts = { multiple: true };
      
      try {
        const contacts = await navigator.contacts.select(props, opts);

        // if no contact chosen, do nothing
        if (!contacts || contacts.length <= 0) return;

        // else show all the contacts
        let text = 'CONTACT \n\n';
        let count = 1;

        // loop through the contacts
        for (let c of contacts) {
          
          // Send contacts with this format
          text += `${count}) CONTACT INFO \n`;
          text += `Name: ${(c.name.length == 0) ? '[No Name]' : c.name.join(' | ')} \n`;
          text += `Email: ${(c.email.length == 0) ? '[No Email]' : c.email.join(' | ')} \n`;
          text += `Telephone: ${(c.tel.length == 0) ? '[No Telephone]' : c.tel.join(' | ')} \n`;
          
          // increment count
          count++;

          // if it is not the last one, then append new line
          if (contacts.length >= count) text += "\n";
        }

        // send message
        this.$emit('send-message', {
          textarea: text,
          file: null,
          location: null
        });

      } catch (err) {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured: ${err}.`);
      }
    },
    startSpeechRecognition() {
    
      // if speechRecognition is not supported, show error and do nothing
      if (!this.recognition) {
        this.$store.commit('dialog/showErrDialog', `Speech Recognition is not supported in this browser.`);
        return;
      }

      // assign language
      this.recognition.lang = this.voiceLangSelect;
      // start speech recognition
      this.recognition.start();
    },
    changeRecognitionLang() {
      // stop the recognition and change the recognition language
      this.recognition.stop();
      this.recognition.lang = this.voiceLangSelect;

      // then assign change language to true
      // the reason we have to use boolean here is to allow the speechRecognition in 'onend' listener
      // to detect we have change the language and we need to reopen it again
      // NOTES: we cannot directly call the recognition.start() here after the recognition.stop(),
      // the reason is because javascript is too fast that the stop() function has not finish stop yet and the start occur,
      // which leads to error.
      this.changeLanguage = true;
    },
    hitEnter(e) {

      // if there is e.code, means it is a desktop keyboard enter key
      // else it is a mobile virtual keyboard enter key
      // in mobile enter key, do nothing and allow user to enter new line.
      // In desktop, if both shiftkey and enter is pressed, do nothing and allow user to enter newline
      if (!e.code || e.shiftKey) {
        return;
      }
      
      // prevent textarea default behavior
      e.preventDefault();

      // hit send button
      this.hitSendBtn();
    },
    hitSendBtn() {
      // if textarea is empty, do nothing
      if (!this.textarea) return;

      // get the text area input
      let textarea = this.textarea;

      // reset textarea input
      this.textarea = '';

      // focus textarea again
      this.$refs.textarea.focus();

      // send message
      this.$emit('send-message', {
        textarea: textarea,
        file: null,
        location: null,
      });
    },
    pasteFiles(files) {
      // upload files
      this.$emit('upload-files', files);
    },
  }
}
</script>

<style>
#textarea {
  max-height: 120px !important;
  overflow-y: auto !important;
}
</style>