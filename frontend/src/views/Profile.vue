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
      <v-btn icon @click="$_back({ name: 'Home' })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Edit Profile</v-toolbar-title>

    </v-app-bar>

    <v-card
      tile
      flat
      class="flex-grow-1 overflow-y-auto"
      :loading="submitLoading"
    >

      <div 
        v-if="initLoading"
        class="text-center py-3"
      >
        <div>Getting Profile Info...</div>
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
      </div>

      <div 
        v-else 
        class="pa-3"  
      >
        <v-avatar size="200" style="border: 1px solid grey;">
          <v-img 
            :src="account.image ? account.image : require(`@/assets/empty.png`)" 
          ></v-img>
        </v-avatar>

        <div class="mt-5">
          <div>Email: {{ account.email }}</div>
          <div>Name: {{ account.name }}</div>
        </div>

        <div class="mt-4">
          <v-btn
            color="secondary"
            depressed
            :loading="submitLoading"
            @click="showPreview(null)"
          >
            Set Image to Default
          </v-btn>
        </div>

        <div class="mt-4">
          <v-btn
            color="secondary"
            depressed
            @click="$refs.file.click()"
            :loading="submitLoading"
          >
            Upload Image File
          </v-btn>
          <input type="file" ref="file" :accept="$imageExtension.join()" class="d-none" @change="uploadFile($event)">
        </div>

        <div class="mt-4">
          <v-btn
            color="secondary"
            depressed
            :loading="submitLoading"
            :to="{ name: 'ImageCapture' }"
          >
            Built in camera
          </v-btn>
        </div>
      </div>
    </v-card>

    <v-dialog
      v-model="dialog"
      max-width="300"
    >
      <v-card>
        <v-card-title>
          Preview
        </v-card-title>

        <v-card-text class="text-center">
          <v-avatar size="200">
            <v-img :src="preview ? preview : require(`@/assets/empty.png`)"></v-img>
          </v-avatar>
        </v-card-text>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn
            color="green darken-1"
            text
            @click="dialog = false"
          >
            Cancel
          </v-btn>

          <v-btn
            color="green darken-1"
            text
            @click="submitFile()"
          >
            Upload Image
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script>
export default { 
  name: 'Profile',
  data: () => ({
    uid: '',
    dialog: false,
    file: null,
    preview: '',
    submitLoading: false,
  }),
  computed: {
    initLoading() { return this.$store.state.accounts.initLoading; },
    account() { return this.$store.state.accounts.accounts.find(a => a.id == this.uid); },
  },
  async created() {
    // Init Account Uid
    this.uid = this.$getCookie('uid');
  },
  methods: {
    uploadFile(e) {
      
      // get the file
      const file = e.target.files[0];

      // reset the value for onchange to work another time
      this.$refs.file.value = null;

      // if file is null, do nothing
      if (file == null) return; 

      // only allow certain type
      if (!this.$checkImageExtension(file.name)) {
        this.$store.commit('dialog/showErrDialog', `File type must be ${this.$imageExtension.join(", ")}.`);
        return;
      }

      // file size must below 5 MB
      const sizeLimit = 5242880;
      if (file.size > sizeLimit) {(number/1048576).toFixed(1)
        this.$store.commit('dialog/showErrDialog', `File exceeded ${this.$formatBytes(sizeLimit)}.`);
        return;
      }

      // set to file variable, for later submission
      this.file = file;

      // open dialog and show preview
      let url = URL.createObjectURL(file);
      this.showPreview(url);
    },
    showPreview(url) {
      // open dialog and show preview
      this.preview = url;
      this.dialog = true;
    },
    async submitFile() {

      // close the dialog
      this.dialog = false;

      // show submitLoading state
      this.submitLoading = true;

      try {

        // prepare formdata for post request
        let formData = new FormData();
        // if the file is not empty
        if (this.file != null) {
          formData.append("file", this.file);
        }

        // axios post request to upload image
        await this.$axios.post('/account/upload-image', formData, {
            headers: {
              'Content-Type': 'multipart/form-data'
            }
        });

        // show success message
        this.$store.commit("dialog/showSuccessDialog", 'Image Updated! Please refresh the page to see the changes.');
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
        // reset file
        this.file = null;
      }

    },
  }
}
</script>

<style>

</style>