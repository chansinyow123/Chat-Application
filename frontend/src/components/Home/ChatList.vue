<template>
  <v-card
    tile
    flat
    class="d-flex flex-column fill-height"
  >
    <v-app-bar
      color="panel"
      flat
      tile
      dark
      class="flex-grow-0 flex-shrink-0"
    >
      <v-toolbar-title>{{ name ? name : 'Loading...' }}</v-toolbar-title>

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
          >
            <v-icon>mdi-dots-vertical</v-icon>
          </v-btn>
        </template>
        
        <v-list class="pa-0">
          <v-list-item :to="{ name: 'NewGroup' }">
            <v-list-item-title>New Group</v-list-item-title>
          </v-list-item>
          <v-list-item @click="changeTheme()">
            <v-list-item-content>
              <v-list-item-title>Dark Mode</v-list-item-title>
            </v-list-item-content>

            <v-list-item-action>
              <v-switch
                class="pl-3"
                v-model="$vuetify.theme.dark"
                disabled
              ></v-switch>
            </v-list-item-action>
          </v-list-item>
          <v-list-item v-if="name" @click="setNotification()">
            <v-list-item-content>
              <v-list-item-title>Notification</v-list-item-title>
            </v-list-item-content>

            <v-list-item-action>
              <v-switch
                class="pl-3"
                v-model="needNotification"
                disabled
              ></v-switch>
            </v-list-item-action>
          </v-list-item>
          <v-list-item :to="{ name: 'Profile' }">
            <v-list-item-title>Edit Profile</v-list-item-title>
          </v-list-item>
          <v-list-item :to="{ name: 'ChangePassword' }">
            <v-list-item-title>Change Password</v-list-item-title>
          </v-list-item>
          <v-list-item :to="{ name: 'Account' }" v-if="isAdmin">
            <v-list-item-title class="font-weight-bold">Accounts (Admin)</v-list-item-title>
          </v-list-item>
          <v-list-item @click="$refresh()">
            <v-list-item-title>Refresh</v-list-item-title>
          </v-list-item>
          <v-list-item :to="{ name: 'Login' }">
            <v-list-item-title>Log out</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-menu>

      <template v-slot:extension>
        <v-tabs 
          v-model="tab"
          fixed-tabs
        >
          <v-tab>Recent</v-tab>
          <v-tab>Contact</v-tab>
        </v-tabs>
      </template>
    </v-app-bar>

    <Recent v-show="tab == 0"/>
    <Contact v-show="tab == 1"/>
  </v-card>
</template>

<script>
import Recent from './Recent.vue'
import Contact from './Contact.vue'

export default {
  name: 'ChatList',
  components: {
    Recent,
    Contact,
  },
  data: () => ({
    tab: 0,                   // check which tab the user is on
    isAdmin: false,           // check is this user admin or not
    needNotification: false   // set user notification
  }),
  created() {
    // if is admin, show account navigation in menu popup
    this.isAdmin = this.$getCookie('is_admin') ?? false;
  },
  computed: {
    name() {
      // get this user info, if no info found, return null
      let thisUser = this.$store.state.accounts.accounts.find(a => a.id == this.$getCookie('uid'));
      if (thisUser == null) return null;

      // else assign need notification and return this user's name
      this.needNotification = thisUser.needNotification;

      return thisUser.name;
    },
  },
  methods: {
    changeTheme() {
      this.$vuetify.theme.dark = !this.$vuetify.theme.dark
      this.$setTheme(this.$vuetify.theme.dark);
    },
    logout() {
      this.$logout();
      this.$router.push({ name: 'Login' });
    },
    async setNotification() {

      // show loading state
      this.$store.commit('dialog/showLoading', `Updating Notification`);

      // post request to update notification
      try {
        await this.$axios.post('/account/toggle-notification');
      }
      catch(err) {
        if (err.response) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'Bad Request.');
        } else if (err.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    },
  },
}
</script>

<style>

</style>