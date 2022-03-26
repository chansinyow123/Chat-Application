<template>
  <v-card
    tile
    flat
    class="d-flex flex-column fill-height"
  >
    <v-app-bar
      flat
      tile
      color="panel"
      dark
      class="flex-grow-0"
    >
      <v-btn icon @click="$_back({ name: 'Profile' })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Image Capture</v-toolbar-title>

      <v-spacer></v-spacer>

      <v-menu
        @input="$store.commit('dialog/toggleOverlay', $event)"
        left 
        origin="top right"
        transition="scale-transition"
        min-width="150px"
      >
        <template v-slot:activator="{ on, attrs }">
          <v-btn
            icon
            v-bind="attrs"
            v-on="on"
            v-show="camera.length > 0 && !imageCaptured && !initLoading"
          >
            <v-icon>mdi-dots-vertical</v-icon>
          </v-btn>
        </template>
        
        <v-list class="pa-0">
          <v-list-item-group
            v-model="selectedCamera"
            color="primary"
          >
            <v-list-item v-for="c of camera" @click="openCamera(c.deviceId)" :key="c.deviceId" >
              <v-list-item-title>{{ c.label }}</v-list-item-title>
            </v-list-item>
          </v-list-item-group>
        </v-list>
      </v-menu>

    </v-app-bar>

    <v-card
      tile
      flat
      class="flex-grow-1 overflow-y-auto"
      :loading="submitLoading"
    >

      <div 
        v-show="initLoading"
        class="text-center py-3"
      >
        <div>Preparing Camera...</div>
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
      </div>

      <div 
        v-show="cameraError"
        class="text-center py-3"
      >
        {{ cameraError }}
      </div>


      <div v-show="!cameraError && !initLoading" class="d-flex flex-column fill-height">
        
        <div class="flex-grow-1 d-flex justify-center align-center" style="position: relative">
          <video ref="video" v-show="!imageCaptured" style="position: absolute; left: 0; top: 0; width: 100%; height: 100%;"></video>

          <canvas ref="canvas" v-show="false"></canvas>
          <v-avatar v-show="imageCaptured" size="200">
            <v-img 
              :src="src" 
            ></v-img>
          </v-avatar>
        </div>

        <div class="d-flex justify-center py-2">
          <div v-show="!imageCaptured">
            <v-btn
              color="primary"
              depressed
              @click="capture()"
            >
              Capture
            </v-btn>
          </div>
          <div v-show="imageCaptured">
            <v-btn
              color="primary"
              depressed
              :loading="submitLoading"
              @click="submit()"
            >
              Upload
            </v-btn>
            <v-btn
              color="secondary"
              class="ml-2"
              depressed
              :loading="submitLoading"
              @click="imageCaptured = false"
            >
              Take again
            </v-btn>
          </div>
        </div>
      </div>
    </v-card>
  </v-card>
</template>

<script>
export default {
  name: 'ImageCapture',
  data: () => ({
    initLoading: true,       // to show init loading state
    cameraError: '',         // display camera error, if any
    submitLoading: false,    // show loading state when submit
    imageCaptured: false,    // to check if image is captured
    camera: [],              // to get a list of cameras
    selectedCamera: 0,       // to get the selected camera
    context: null,           // context for 2d canvas
    src: '',                 // to show the preview image
  }),
  async mounted() {
    // check if mediaDevice is supported in this browser or not --------------------------------------
    if (!'mediaDevices' in navigator) {
      this.$store.commit('dialog/showErrDialog', 'Browser Not Supported');
      this.$_back({ name: 'Profile' });
      return;
    }

    // get the canvas context -------------------------------------------------------------------------
    this.context = this.$refs.canvas.getContext('2d');

    await this.openCamera();
  },
  beforeDestroy() {
    // Stop all video streams, if available.
    this.stopCamera();
  },
  methods: {
    async openCamera(id='') {
      // Stop all video streams, if available.
      this.stopCamera();

      // reset initLoading and cameraError
      this.initLoading = true;
      this.cameraError = '';

      // this is for camera device highlighting in menu
      let currentDeviceId = '';

      try {

        // declare constraint
        // if got deviceId, use that camera, else use suitable camera
        let constraint = {};
        if (id) { constraint = { audio: false, video: { deviceId: id } }; }
        else    { constraint = { audio: false, video: true }; }

        // get the stream from camera
        let stream = await navigator.mediaDevices.getUserMedia(constraint);
        this.$refs.video.srcObject = stream;
        // get the current deviceId
        currentDeviceId = stream.getTracks()[0].getSettings().deviceId;
        this.$refs.video.play();

      } catch(err) {

        // handle the error
        let message = ''
        switch(err.name) {
          case 'NotAllowedError':
            message = 'Camera permission not allowed.';
            this.$store.commit('dialog/showErrDialog', message);
            break;
          case 'NotReadableError':
            message = 'Camera not readable. Try switch to other camera';
            this.$store.commit('dialog/showErrDialog', message);
            break;
          case 'NotFoundError':
            message = 'Camera not found.';
            this.$store.commit('dialog/showErrDialog', message);
            break;
          default:
            message = `Unexpected Error Occur: ${err.name}`;
            this.$store.commit('dialog/showErrDialog', message);
            console.error(err);
        }

        this.cameraError = message;
      }
      finally {
        // remove initLoading state
        this.initLoading = false;
      }

      // get all the video input devices ------------------------------------------------------------
      // reset the devices available
      this.camera = []; 
      let devices = await navigator.mediaDevices.enumerateDevices();

      // assign to camera variable
      devices.forEach((d) => {
        if (d.kind === 'videoinput') {
          this.camera.push(d);
        }
      });

      // assign the index of current camera, if have one
      if (currentDeviceId) {
        this.selectedCamera = this.camera.findIndex(c => c.deviceId == currentDeviceId);
      }
    },
    stopCamera() {
      // Stop all video streams, if available.
      if (this.$refs.video.srcObject) {
        this.$refs.video.srcObject.getVideoTracks().forEach(track => track.stop());
      }
    },
    capture() {
      // Draw the video frame to the canvas.
      // Have to resize the canvas first
      this.$refs.canvas.width = this.$refs.video.videoWidth;
      this.$refs.canvas.height = this.$refs.video.videoHeight;
      this.context.drawImage(this.$refs.video, 0, 0, this.$refs.canvas.width, this.$refs.canvas.height);
      this.src = this.$refs.canvas.toDataURL();
      this.imageCaptured = true;
    },
    submit() {

      // show submitLoading state
      this.submitLoading = true;

      // prepare formdata for post request
      this.$refs.canvas.toBlob(async (blob) => {
        await this.uploadFile(blob);
      });

    },
    async uploadFile(blob) {

      try {
        // convert blob to file, for additional property like file extension, name, etc
        let file = new File([blob], "file.png", { type: "image/png" });

        // validate size
        // file size must below 5 MB
        const sizeLimit = 5242880;
        if (file.size > sizeLimit) {
          this.$store.commit('dialog/showErrDialog', `File exceeded ${this.$formatBytes(sizeLimit)}.`);
          return;
        }

        // prepare formdata for post request
        let formData = new FormData();
        formData.append("file", file);

        // axios post request to upload image
        await this.$axios.post('/account/upload-image', formData, {
            headers: {
              'Content-Type': 'multipart/form-data'
            }
        })

        // show success message and go back to profile page
        this.$store.commit("dialog/showSuccessDialog", 'Image Updated! Please refresh the page to see the changes.');

        this.$router.replace({ name: 'Profile' });
      }
      catch (error) {

        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = error.response.data;
          const errStatus = error.response.status;

          switch (errStatus) {
            case 400: // if field has error
              this.$store.commit("dialog/showErrDialog", errResponse.errors.File);
              break;
          }
        } else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }
      finally {
        // remove submitLoading state
        this.submitLoading = false;
      }
    },
  }
}
</script>

<style>

</style>