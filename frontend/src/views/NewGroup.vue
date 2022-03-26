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

      <div class="d-flex px-md-2 flex-grow-1" style="min-width: 0;">
        <div class="d-flex flex-column pl-3" style="min-width: 0;">
          <div class="text-body-1 text-truncate" >New Group</div>
          <div class="text-caption grey--text text--lighten-1 text-truncate">{{ members.length }} Selected</div>
        </div>
      </div>
    </v-app-bar>

    <div class="d-flex pa-2">
      <div class="d-flex justify-center align-center">
        <v-avatar style="border: 1px solid grey; cursor: pointer" @click="$refs.file.click()">
          <v-img
            :src="preview ? preview : require(`@/assets/empty-group.png`)" 
          ></v-img>
        </v-avatar>
        <input type="file" ref="file" :accept="$imageExtension.join()" class="d-none" @change="uploadFile($event)">
      </div>

      <div class="d-flex align-center px-2 flex-grow-1">
        <v-text-field
          label="Type group name here..."
          v-model.trim="groupName"
          hide-details
          clearable
          dense
          outlined
          solo
          flat
          @keydown.enter.exact="createGroup()"
          maxlength="30"
        ></v-text-field>
      </div>

      <div class="d-flex justify-center align-center">
        <v-btn 
          fab
          dark
          small
          depressed
          color="green darken-4"
          @click="createGroup()"
        >
          <v-icon dark>
            mdi-plus
          </v-icon>
        </v-btn>
      </div>
    </div>

    <div class="flex-grow-1 overflow-y-auto">

      <v-divider></v-divider>

      <div 
        v-if="initLoading"
        class="text-center py-4"
      >
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>  
        <div class="mt-3">Getting Accounts...</div>
      </div>

      <div
        v-else-if="accounts.length == 0"
        class="text-center py-4"
      >
        <span>There are no account created yet.</span>
      </div>

      <div v-else>
        <v-list 
          two-line
          class="pa-0"
          subheader
          flat
        >
          <v-subheader>Select Members</v-subheader>

          <v-list-item-group
            v-model="members"
            multiple
          >
            <template v-for="account in accounts">
              <v-list-item :key="account.id" :value="account.id">
                <template v-slot:default="{ active }">
                  <v-badge
                    bordered
                    bottom
                    :value="active"
                    icon="mdi-check-circle"
                    color="green"
                    offset-x="30"
                    offset-y="32"
                  >
                    <v-list-item-avatar>
                      <v-img :src="account.image ? account.image : require(`@/assets/empty.png`)"></v-img>
                    </v-list-item-avatar>
                  </v-badge>

                  <v-list-item-content>
                    <v-list-item-title v-text="account.name"></v-list-item-title>
                    <v-list-item-subtitle v-text="account.email"></v-list-item-subtitle>
                  </v-list-item-content>

                </template>
              </v-list-item>

              <v-divider></v-divider>
            </template>
          </v-list-item-group>
        </v-list>
      </div>
    </div>

  </v-card>
</template>

<script>
export default {
  name: 'NewGroup',
  data: () => ({
    members: [],
    groupName: '',
    preview: '',
    file: null,
  }),
  computed: {
    initLoading() {
      return this.$store.state.accounts.initLoading;
    },
    accounts() {
      // get a list of account excluding this users account, and then sort by name
      return this.$store.state.accounts.accounts
        .filter(x => x.id != this.$getCookie('uid'))
        .sort((x, y) => {
          let a = x.name.toUpperCase();
          let b = y.name.toUpperCase();
          return a == b ? 0 : a > b ? 1 : -1;
        });
    },
  },
  methods: {
    async createGroup() {

      // if group name is empty, prompt error message and do nothing
      if (!this.groupName) {
        this.$store.commit('dialog/showErrDialog', `Group name cannot be empty.`);
        return;
      }

      // If group name is over 30, prompt error message and do nothing
      if (this.groupName.length > 30) {
        this.$store.commit('dialog/showErrDialog', 'Group name must below 30 character.');
        return;
      }

      // if member is empty, prompt error message and do nothing
      if (this.members.length <= 0) {
        this.$store.commit('dialog/showErrDialog', 'Must at least 1 member to be selected.');
        return;
      }

      // prepare formdata
      let formData = new FormData();
      for (let i = 0; i < this.members.length; i++) {
        formData.append('usersId[]', this.members[i]);
      }
      formData.append("groupName", this.groupName);
      if (this.file != null) {
        formData.append("file", this.file);
      }

      // show loading state
      this.$store.commit('dialog/showLoading', `Creating Group`);

      try {
        // create group with post request
        let response = await this.$axios.post('/group', formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });

        // show success message and redirect to home page
        this.$store.commit('dialog/showSuccessDialog', `Group "${this.groupName}" Created!`);
        this.$router.replace({ name: 'Home' });
      }
      catch(err) {

        if (err.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = err.response.data;
          const errStatus = err.response.status;

          switch (errStatus) {
            case 400:
              // if file is invalid
              this.status400(errResponse.errors);
              break;
            case 404:
              // if receiverId is not found, or data passed wrong
              this.$store.commit('dialog/showErrDialog', errResponse.error);
              break;
          }
        } 
        else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
    status400(errors) {
      // show error message
      if (errors.UsersId) { this.$store.commit("dialog/showErrDialog", errors.UsersId); }
      else if (errors.GroupName) { this.$store.commit("dialog/showErrDialog", errors.GroupName); }
      else if (errors.File) { this.$store.commit('dialog/showErrDialog', errors.File); }
    },
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

      // create blobURL and show preview
      this.preview = URL.createObjectURL(file);
    },
  }
}
</script>

<style>

</style>