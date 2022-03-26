<template>
  <div
    class="d-flex" 
    :class="{ 'justify-end': uid == m.senderId }" 
  >
    <div 
      class="d-flex flex-column justify-end px-1" 
      :class="{ 'order-last': uid != m.senderId }" 
      v-if="m.id"
    >
      <v-menu
        @input="$store.commit('dialog/toggleOverlay', $event)"
        absolute
        offset-y
        offset-x
        transition="scale-transition"
        min-width="150px"
      >
        <template v-slot:activator="{ on, attrs }">
          <v-btn
            icon
            small
            v-bind="attrs"
            v-on="on"
          >
            <v-icon>mdi-dots-horizontal</v-icon>
          </v-btn>
        </template>
        
        <v-list class="pa-0">
          <slot></slot>
          <v-list-item v-if="!m.onDelete && !m.location" @click="shareMessage(m)">
            <v-list-item-title>Share</v-list-item-title>
          </v-list-item>
          <v-list-item v-if="m.message" @click="copyMessage(m.message)">
            <v-list-item-title>Copy Message</v-list-item-title>
          </v-list-item>
          <v-list-item v-if="m.file && $checkImageExtension(m.fileName)" @click="copyImage(m.file)">
            <v-list-item-title>Copy Image</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-menu>
    </div>
    <div 
      class="rounded-lg d-flex flex-column pb-1" 
      style="max-width: 85%;"
      :class="[ uid == m.senderId ? 'senderPanel' : 'receiverPanel']"
    >
      <div 
        v-if="showName" 
        class="text-caption pt-1 px-2 name--text font-weight-bold"
      >
        {{ getName(m.senderId) }}
      </div>

      <div 
        class="rounded-lg pt-2 px-2"
        v-if="m.fileName"
      >
        <v-img
          :src="m.file"
          style="cursor: pointer;"
          width="300px"
          height="300px"
          class="rounded-lg"
          v-if="$checkImageExtension(m.fileName)"
          @click="openImage(m.file)"
        >
        </v-img>

        <audio
          v-else-if="$checkAudioExtension(m.fileName)"
          style="width: 250px; height: 35px;"
          @play="playMedia($event, m)"
          controls
          :src="m.file"
        >
        </audio>

        <video
          v-else-if="$checkVideoExtension(m.fileName)"
          style="width: 250px; height: 250px; background-color: black;"
          @play="playMedia($event, m)"
          controls
          :src="m.file"
        >
        </video>

        <v-list 
          v-else
          two-line 
          width="100%"
          max-width="300px"
          class="pa-0"
          dense
          outlined
        >
          <v-list-item :href="m.file" :download="m.fileName">
            <v-list-item-avatar style="border: 1px solid grey;">
              <v-icon class="indigo" dark>
                mdi-download-circle
              </v-icon>
            </v-list-item-avatar>

            <v-list-item-content>
              <v-list-item-title>{{ m.fileName }}</v-list-item-title>
              <v-list-item-subtitle>{{ m.fileName.split('.').pop().toUpperCase() }} / {{ $formatBytes(m.fileSize) }}</v-list-item-subtitle>
            </v-list-item-content>
          </v-list-item>
        </v-list>
      </div>

      <div
        v-else-if="m.location" 
        class="rounded-lg pt-2 px-2"
      >
        <a target="_blank" :href="`https://www.google.com/maps/search/?api=1&query=${getLocation(m.location)}`">
          <v-img 
            :src="`https://maps.googleapis.com/maps/api/staticmap?center=${getLocation(m.location)}&zoom=13&size=300x200&maptype=roadmap
            &markers=${getLocation(m.location)}
            &key=AIzaSyCtRCmriWnKyyUusWBq2eyI9bZLYzynkgg`"
            width="300px"
            height="200px"
            class="rounded-lg"
          >
          </v-img>
        </a>
      </div>

      <div class="d-flex flex-wrap rounded-lg pt-1 px-2">
        <div class="flex-grow-1 d-flex flex-column justify-center message--text text-body-2" style="white-space: pre-line; word-break: break-all;">
          <span v-if="m.onDelete" class="font-weight-bold"> 
            <v-icon small class="pr-1">mdi-cancel</v-icon>
            Message Deleted
          </span>
          <span v-else class="text-body-2" v-html="getMessage(m.message)"></span>
        </div>
        <div class="flex-grow-1 d-flex flex-column justify-end pl-3">
          <div class="text-caption time--text text-right">
            {{ $getTime(new Date(m.onCreate)) }}
            <v-icon small :color="(m.id && m.onSeen) ? 'light-blue' : 'time'">
              {{ (!m.id) ? 'mdi-clock-outline' : 'mdi-check-all' }}
            </v-icon>
          </div>
        </div>
      </div>
      
    </div>

    <v-snackbar
      timeout="2000"
      v-model="showSnackbar"
      content-class="text-center"
      style="bottom: 60px;"
    >
      {{ snackbarText }}
    </v-snackbar>
  </div>
</template>

<script>
export default {
  name: 'MessageBox',
  props: {
    m: {
      type: Object,
      required: true,
    },
    showName: {
      type: Boolean
    }
  },
  data: () => ({
    uid: null,                         // get the user uid from cookie
    origin: window.location.origin,    // current location url origin
    // clipboard
    showSnackbar: false,               // showing snackbar
    snackbarText: '[placeholder]',     // showing snackbar text
  }),
  created() {
    // get the user uid from cookie
    this.uid = this.$getCookie('uid');
  },
  methods: {
    async openImage(url) {
      try {
        let blob = await this.$urlToBlob(url);
        let objectURL = URL.createObjectURL(blob);
        window.open(objectURL, "_self");
        URL.revokeObjectURL(objectURL);
      }
      catch(err) {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured.`);
        console.log(err);
      }
    },
    getMessage(message) {

      // regex for detecting url/email in message
      let urlRegex = /(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])$/ig;
      let emailRegex = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
      let phoneRegex = /(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?$/img

      // first split the words with white space or new line
      let words = message.split(/(\s|\n)/);

      // loop through the array to detect if there is any regex that is match
      // then we inject some html code according to the regex
      // else encode the words to prevent XSS attack.
      words = words.map(w => {
        if (urlRegex.test(w)) 
          return `<a class="url--text" href="${w}">${w}</a>`;
        else if (emailRegex.test(w))
          return `<a class="url--text" href="mailto:${w}">${w}</a>`;
        else if (phoneRegex.test(w))
          return `<a class="url--text" href="tel:${w}">${w}</a>`;
        else 
          return this.$htmlEncode(w);
      })

      // join back the array and return the message
      return words.join(' ');
    },
    getLocation(json) {
      // parse location json to object
      // if cannot parse, just return invalid location
      let location = {}
      try { location = JSON.parse(json); }
      catch { return '500,500'; }
      // get lat and lng
      let lat = location.lat;
      let lng = location.lng;

      // display as query parameters
      return `${lat},${lng}`;
    },
    playMedia(e, m) {
      // get the audio element
      const audio = e.target;

      // get the sender info
      const sender = this.$store.state.accounts.accounts.find(a => a.id == m.senderId);
      const title = sender.name;
      const image = sender.image || require(`@/assets/empty.png`);

      // call audio module in vuex
      this.$store.commit("audio/setAudio", {
        audio: audio,
        title: title,
        image: image,
      });
    },
    async shareMessage(m) {
      // check if this browser support web share api
      if (!navigator.canShare) {
        this.$store.commit('dialog/showErrDialog', `Web share api is not supported in this browser.`);
        return;
      }

      // prepare share data
      let shareData = {
        text: m.message,
      }

      // append to shareData if there is a file.
      if (m.file) {
        let fileObject = await this.$urlToFile(m.file, m.fileName, m.contentType);
        shareData.files = [fileObject];
      }

      // check if sharedata is able to share
      if (!navigator.canShare(shareData)) {
        this.$store.commit('dialog/showErrDialog', `Unable to share the data as it is invalid.`);
        return;
      }

      // show loading state
      this.$store.commit('dialog/showLoading', `Opening Share`);

      // share data
      try {
        await navigator.share(shareData);
      } catch(err) {

        switch (err.name) {
          case "NotAllowedError":
            this.$store.commit('dialog/showErrDialog', `Permission denied to share.`);
            break;
          case "AbortError":
            // this error will be called even the share is close, so do nothing here
            break;
          case "TypeError":
            this.$store.commit('dialog/showErrDialog', `Data is invalid to share.`);
            break;
          case "DataError":
            this.$store.commit('dialog/showErrDialog', `Unable to share data.`);
            break;
          default:
            this.$store.commit('dialog/showErrDialog', `Unexpected error occured: ${err}`);
        }
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
    async copyMessage(message) {
      try {
        // copy the message to clipboard, and show snackbar message
        await navigator.clipboard.writeText(message);
        this.snackbarText = "Message Copied.";
        this.showSnackbar = true;
      }
      catch (err) {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured: ${err}`);
      }
    },
    async copyImage(file) {
      try {
        // copy the image to clipboard, and show snackbar message
        let blob = await this.$urlToBlob(file);
        // we must change the blob type to only image/png, because clipboard only support image/png now
        blob = blob.slice(0, blob.size, "image/png");
        let data = [new ClipboardItem({ ['image/png']: blob })];
        await navigator.clipboard.write(data);
        this.snackbarText = "Image Copied.";
        this.showSnackbar = true;
      }
      catch (err) {
        this.$store.commit('dialog/showErrDialog', `Unexpected error occured: ${err}`);
      }
    },
    getName(uid) {
      return this.$store.state.accounts.accounts.find(a => a.id == uid).name;
    }
  }
}
</script>

<style>

</style>