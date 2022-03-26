<template>
  <v-row 
    no-gutters 
    v-resize="onResize"
  >
    <v-col
      cols="12"
      md="4"
      lg="3"
      v-show="showOuter"
      class="fill-height"
      style="border-right: 1px solid rgba(128, 128, 128, 0.5);"
    >

      <ChatList />
      
    </v-col>
    <v-col
      cols="12"
      md="8"
      lg="9"
      v-show="showInner"
      class="fill-height"
    >
      <router-view :key="routerKey"></router-view> 
    </v-col>
  </v-row>
</template>

<script>
import ChatList from '../components/Home/ChatList.vue'
import signalr from '../mixins/signalr'

export default {
  name: 'Home',
  components: {
    ChatList
  },
  mixins: [signalr],
  data: () => ({
    showOuter: true,      // to show outer element
    showInner: true,      // to show inner route
    routeName: 'Home',    // to detect route changed
    routerKey: '',        // to cache the same privateId and groupId route
  }),
  async created() {

    // whenever a user enter the page, assign the destination route name
    this.routeName = this.$route.name;

    // set router view key
    this.setKey();

    // close all notification if available
    await this.closeAllNotification();

    // add event listener to close all notification whenever the page is focus
    window.addEventListener('focus', this.closeAllNotification);

    // Subscribe push notification --------------------------------------------------------------------
    // if service worker is not supported, do nothing
    if (!('serviceWorker' in navigator)) return;

    // ask for permission -----------------------------------------------------------------------------
    try { await this.askPermission(); }
    catch(err) { console.log(err); return; }

    // get the service worker registration ------------------------------------------------------------
    // if there is no registration, then do nothing
    // NOTES: From now on, every catch block will log the error and do nothing.
    let registration = await this.getRegistration();
    if (!registration) return;

    // prepare subscribe options and subscribe push subscription --------------------------------------
    const subscribeOptions = {
      userVisibleOnly: true,
      applicationServerKey: 'BOqlCwZsvXGles0e9OXSht_kLuoR7W0DLiM44QA_WT0qsWawP5HjP4d6dUN_ELx5EenIELUlPZoXcd1X8dEEWwk'
    };

    // subscribe push subscription --------------------------------------------------------------------
    let pushSubscription = null;
    try { pushSubscription = await registration.pushManager.subscribe(subscribeOptions); }
    catch(err) { console.log(err); return; }

    // prepare subscriptionObject for saving into database --------------------------------------------
    const subscriptionObject = {
      endpoint: pushSubscription.endpoint,
      p256dh: this.$arrayBufferToBase64(pushSubscription.getKey('p256dh')),
      auth: this.$arrayBufferToBase64(pushSubscription.getKey('auth')),
    };
    
    // make a post request to save the push subscription into database --------------------------------
    try { await this.$axios.post('/chat/subscribe-notification', subscriptionObject); }
    catch(err) { console.log(err); return; }
  },
  beforeDestroy() {
    // close the recording, if available
    window.removeEventListener('focus', this.closeAllNotification);
  },
  methods: {
    async closeAllNotification() {

      // get the registration
      // if there is no registration, then do nothing
      let registration = await this.getRegistration();
      if (!registration) return;

      // close all notification
      let notifications = await registration.getNotifications()
      
      // loop through the notification to close all the notification
      // if it is the same, then we assign that notification
      // this is to merge the notification later
      for(let i = 0; i < notifications.length; i++) {
        notifications[i].close();
      }
    },
    async getRegistration() {
      // return promise or null
      try { return await navigator.serviceWorker.getRegistration(); }
      catch(err) { console.log(err); return null; }
    },
    askPermission() {
      // request push notification permission
      // some browser will use callback function to retrieve permission, and some browser will use promise
      // therefore we check both in here
      return new Promise((resolve, reject) => {
        // Callback function
        const permissionResult = Notification.requestPermission((result) => {
          resolve(result);
        });

        // Promise
        if (permissionResult) {
          permissionResult.then(resolve, reject);
        }
      })
      .then((permissionResult) => {
        // here will have 3 permission result: 'granted', 'default', 'denied'.
        // if not granted, then throw error
        if (permissionResult !== 'granted') {
          throw new Error('Permission for push notification is not permitted.');
        }
      });
    },
    onResize() {
      // if window width is above the sm thresholds, then show all container
      if (window.innerWidth >= this.$vuetify.breakpoint.thresholds.sm) {
        this.showOuter = true;
        this.showInner = true;
      }
      else {
        // if it is the Home Route (Default Route), then only show outer
        // if it is not the Home Route (Default Route), then only show inner
        this.showOuter = this.routeName == 'Home' ? true : false;
        this.showInner = this.routeName != 'Home' ? true : false;
      }
    },
    setKey() {
      // set router view key
      // this is to cache component in the same route, but also trigger lifecycle after navigate to other route.
      if (this.$route.params.privateId) {
        // if privateId exist, set private route key
        this.routerKey = "private/" + this.$route.params.privateId;
      }
      else if (this.$route.params.groupId) {
        // if groupId exist, set group route key
        this.routerKey = "group/" + this.$route.params.groupId;
      }
      else {
        // else full route update
        this.routerKey = this.$route.fullPath;
      }
    }
  },
  beforeRouteUpdate(to, from, next) {
    // whenever a user switching page, assign the destination route name
    // then resize the page
    this.routeName = to.name;
    this.onResize();
    next();

    // set router view key
    this.setKey();
  }
}
</script>

<style>

</style>

