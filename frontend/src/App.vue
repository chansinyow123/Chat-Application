<template>
  <v-app>
    <v-main>
      <div class="d-flex flex-column main-content">
        <v-system-bar color="orange" class="black--text justify-center" v-show="hasDisconnected">
          <v-icon color="black">mdi-wifi-off</v-icon>
          <span>No internet connection</span>
        </v-system-bar>
        <RefreshBanner />
        <DownloadBanner />
        <router-view class="flex-grow-1 overflow-y-auto "/>
        <ErrorDialog />
        <SuccessDialog />
        <LoadingDialog />
        <RefreshDialog />
        <SpeechDialog />
        <v-overlay :value="showOverlay">
        </v-overlay>
      </div>
    </v-main>
  </v-app>
</template>

<script>
import RefreshBanner from './components/App/RefreshBanner.vue'
import DownloadBanner from './components/App/DownloadBanner.vue'
import ErrorDialog from './components/App/ErrorDialog.vue'
import SuccessDialog from './components/App/SuccessDialog.vue'
import LoadingDialog from './components/App/LoadingDialog.vue'
import RefreshDialog from './components/App/RefreshDialog.vue'
import SpeechDialog from './components/App/SpeechDialog.vue'

export default {
  name: 'App',
  components: {
    RefreshBanner,
    DownloadBanner,
    ErrorDialog,
    SuccessDialog,
    LoadingDialog,
    RefreshDialog,
    SpeechDialog,
  },
  data: () => ({
    // check online status
    hasDisconnected: false,
  }),
  computed: {
    showOverlay() {
      return this.$store.state.dialog.showOverlay;
    }
  },
  created() {
    // check online status ------------------------------------------------------------------
    this.hasDisconnected = !navigator.onLine;
    window.addEventListener("online", () => this.hasDisconnected = false);
    window.addEventListener("offline", () => this.hasDisconnected = true);
  },
};
</script>

<style>
.main-content {
  height: 100vh; 
  width: 100%; 
  max-width: 2000px; 
  margin: auto;
}
</style>
