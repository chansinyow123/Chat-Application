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
      <v-btn icon @click="$_back({ name: 'Group', params: { groupId: groupId } })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Group Info</v-toolbar-title>

    </v-app-bar>

    <v-card
      tile
      flat
      class="flex-grow-1 overflow-y-auto"
      color="chatBackground"
    >
      <div 
        v-if="initLoading"
        class="fill-height d-flex flex-column justify-center align-center"
      >
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>  
        <div class="mt-3">Getting Group Info...</div>
      </div>

      <div
        v-else-if="notFound"
        class="fill-height d-flex justify-center align-center"
      >
        <div>
          <span>This group is not available.</span>
        </div>
      </div>

      <div
        v-else-if="notAllow || isExit"
        class="fill-height d-flex justify-center align-center"
      >
        <div>
          <span>You are no longer a member in this group.</span>
        </div>
      </div>

      <div v-else>
        <div class="d-flex justify-center mt-2">
          <v-avatar size="200" style="border: 1px solid grey;">
            <v-img
              :src="image" 
            ></v-img>
          </v-avatar>
        </div>
        <div class="d-flex justify-center mt-2 text--primary"> {{ name }} </div>
        
        <v-divider class="mt-2"></v-divider>

        <v-list 
          class="pa-0"
          subheader
        >
          <v-subheader>{{ group.users.length}} Members</v-subheader>

          <!-- <v-list-item v-if="group.isAdmin">
            <v-list-item-avatar color="green darken-4">
              <v-icon dark> 
                mdi-account-plus 
              </v-icon>
            </v-list-item-avatar>
            <v-list-item-content>
              <v-list-item-title class="font-weight-medium">Add Members</v-list-item-title>
            </v-list-item-content>
          </v-list-item> -->

          <v-divider></v-divider>

          <template v-for="account in group.users">
            <v-menu
              @input="$store.commit('dialog/toggleOverlay', $event)"
              absolute
              offset-y
              offset-x
              transition="scale-transition"
              min-width="150px"
              :disabled="!group.isAdmin || account.id == uid || !!account.onDelete"
            >
              <template v-slot:activator="{ on, attrs }">
                <v-list-item 
                  :key="account.id" 
                  two-line
                  v-bind="attrs"
                  v-on="on"
                >
                  <v-list-item-avatar>
                    <v-img :src="getUserImage(account.id)"></v-img>
                  </v-list-item-avatar>
                  <v-list-item-content>
                    <v-list-item-title v-text="getUserName(account.id)"></v-list-item-title>
                    <v-list-item-subtitle v-text="getUserEmail(account.id)"></v-list-item-subtitle>
                  </v-list-item-content>

                  <v-list-item-action>
                    <v-list-item-action-text v-if="account.onDelete" class="red--text font-weight-medium">Removed on {{ new Date(account.onDelete).toLocaleDateString() }}</v-list-item-action-text>
                    <v-list-item-action-text v-else-if="account.isAdmin" class="green--text font-weight-medium">Group Admin</v-list-item-action-text>

                    <div></div>
                  </v-list-item-action>

                </v-list-item>
              </template>
              
              <v-list class="pa-0">
                <v-list-item v-if="!account.onDelete" @click="removeUser(account.id)">
                  <v-list-item-title>Remove</v-list-item-title>
                </v-list-item>
                <!-- <v-list-item v-if="!!account.onDelete" @click="addUsers([account.id])">
                  <v-list-item-title>Add Back</v-list-item-title>
                </v-list-item> -->
                <v-list-item v-if="account.isAdmin" @click="dismissAdmin(account.id)">
                  <v-list-item-title>Dismiss as admin</v-list-item-title>
                </v-list-item>
                <v-list-item v-else-if="!account.onDelete" @click="makeAdmin(account.id)">
                  <v-list-item-title>Make group admin</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>

            <v-divider></v-divider>
          </template>
        </v-list>
      </div>
    </v-card>
  </v-card>
</template>

<script>
export default {
  name: 'GroupInfo',
  props: {
    groupId: {
      required: true,
    }
  },
  data: () => ({
    uid: '',                       // to get the current user uid
    initLoading: true,             // show initloading state
    notFound: false,               // to show group not found
    notAllow: false,               // to show not allow to view the group
    source: null,                  // to cancel axios operation
    // members: [],                // for member select
    group: {                       // display group info
      isAdmin: false,
      users: [
        {
          id: '',
          onDelete: '',
          isAdmin: false,
        }
      ],
    },
  }),
  async created() {

    // initialize cancelToken source and current user uid
    this.source = this.$axios.CancelToken.source();
    this.uid = this.$getCookie('uid');

    // get groupinfo
    await this.getGroupInfo();
  },
  beforeDestroy() {
    // cancel any axios operation
    this.source.cancel();
  },
  computed: {
    isExit() { return this.$store.getters['groups/getIsExit'](this.groupId); },
    image() { return this.$store.getters['groups/getImage'](this.groupId); },
    name() { return this.$store.getters['groups/getName'](this.groupId); },
  },
  methods: {
    getUserName(id) { return this.$store.getters['accounts/getName'](id); },
    getUserEmail(id) { return this.$store.getters['accounts/getEmail'](id); },
    getUserImage(id) { return this.$store.getters['accounts/getImage'](id) },
    async getGroupInfo() {
      // show initLoading state
      this.initLoading = true;

      try {
        // get the group info response
        let response = await this.$axios.get(`/group/info/${this.groupId}`, {
          'axios-retry': { retries: Infinity },
          cancelToken: this.source.token,
        });

        // assign group data
        this.group = response.data;

      }
      catch(error) {
        // if operation is cancel, no need do error handling
        if (this.$axios.isCancel(error)) {
          console.log('Request cancelled');
          return;
        }

        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = error.response.data;
          const errStatus = error.response.status;

          switch (errStatus) {
            case 404: 
              // if no group info found
              this.notFound = true;
              break;
            case 405:
              // if not allow to view the group info
              this.notAllow = true; 
              break;
          }
        }
      }

      // hide initLoading state
      this.initLoading = false;
    },
    async removeUser(userId) {
      // show loading state
      this.$store.commit('dialog/showLoading', `Removing User`);

      try {
        // post remove request
        await this.$axios.post(`/group/remove`, {
          groupId: this.groupId,
          userId: userId,
        });

        // hide loading state and refresh the data
        await this.getGroupInfo();
      }
      catch(error) {
        await this.handleError(error);
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
    async makeAdmin(userId) {
      // show loading state
      this.$store.commit('dialog/showLoading', `Making User Admin`);

      try {
        // post remove request
        await this.$axios.post(`/group/make-admin`, {
          groupId: this.groupId,
          userId: userId,
        });

        // hide loading state and refresh the data
        await this.getGroupInfo();
      }
      catch(error) {
        await this.handleError(error);
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
    async dismissAdmin(userId) {
      // show loading state
      this.$store.commit('dialog/showLoading', `Making User Admin`);

      try {
        // post remove request
        await this.$axios.post(`/group/dismiss-admin`, {
          groupId: this.groupId,
          userId: userId,
        });

        // hide loading state and refresh the data
        await this.getGroupInfo();
      }
      catch(error) {
        await this.handleError(error);
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
    // async addUsers(members) {
    //   // show loading state
    //   this.$store.commit('dialog/showLoading', `Adding User`);

    //   try {
    //     // post add request
    //     await this.$axios.post(`/group/add`, {
    //       groupId: this.groupId,
    //       userIds: members,
    //     });

    //     // hide loading state and refresh the data
    //     await this.getGroupInfo();
    //   }
    //   catch(error) {
    //     await this.handleError(error);  
    //   }

    //   // hide loading state
    //   this.$store.commit('dialog/hideLoading');
    // },
    async handleError(error) {
      if (error.response) {
        // The request was made and the server responded with a status code
        // that falls out of the range of 2xx
        const errResponse = error.response.data;
        const errStatus = error.response.status;

        // show error dialog and refresh the data
        this.$store.commit('dialog/showErrDialog', 'Unexpected error occured, please try again.');
        await this.getGroupInfo();

      } else if (error.request) {
        // The request was made but no response was received
        this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
      }
    },
    uploadFile() {
      console.log('halo');
    }
  },
}
</script>

<style>

</style>