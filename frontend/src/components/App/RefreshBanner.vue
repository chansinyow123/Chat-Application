<template>
  <v-banner
    :single-line="$vuetify.breakpoint.mdAndUp" 
    :two-line="!$vuetify.breakpoint.mdAndUp" 
    :value="updateExists"
  >
    <v-avatar
      slot="icon"
      color="deep-purple accent-4"
      size="40"
    >
      <v-icon
        icon="mdi-lock"
        color="white"
      >
        mdi-refresh
      </v-icon>
    </v-avatar>

    You have new content available, please refresh your page to update content on site.

    <template v-slot:actions>
      <v-btn
        text
        color="primary"
        @click="refreshApp"
      >
        Refresh
      </v-btn>
    </template>
  </v-banner>
</template>

<script>
export default {
  name: 'RefreshBanner',
  data: () => ({
    refreshing: false,    // this is to prevent multiple refresh for multiple service worker
    registration: null,   // get the service worker registration detail
    updateExists: false,  // to check if there is any update available for service worker
  }),
  created() {

    // Update Service Worker
    document.addEventListener('swUpdated', this.updateAvailable, { once: true });
    
    // when one of the service worker has taken control
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      // We'll also need to add 'refreshing' to our data originally set to false.
      if (this.refreshing) return;
      this.refreshing = true;
      // Here the actual reload of the page occurs
      window.location.reload();
    });
  },
  methods: {
    // Service Worker Update --------------------------------------------------------------------
    updateAvailable(event) {
      this.registration = event.detail;
      this.updateExists = true;
    },
    refreshApp() {
      this.updateExists = false;
      // Make sure we only send a 'skip waiting' message if the SW is waiting
      if (!this.registration || !this.registration.waiting) return;
      // Send message to SW to skip the waiting and activate the new SW
      this.registration.waiting.postMessage({ type: 'SKIP_WAITING' });
    },
  }
}
</script>

<style>

</style>