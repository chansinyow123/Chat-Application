<template>
  <v-banner
    :single-line="$vuetify.breakpoint.mdAndUp" 
    :two-line="!$vuetify.breakpoint.mdAndUp" 
    :value="installPrompt != null"
  >
    <v-avatar
      slot="icon"
      color="orange accent-4"
      size="40"
    >
      <v-icon
        icon="mdi-lock"
        color="white"
      >
        mdi-download
      </v-icon>
    </v-avatar>

    Install this website as your desktop / mobile app.

    <template v-slot:actions="{ dismiss }">
      <v-btn
        text
        color="primary"
        @click="installApp"
      >
        Install
      </v-btn>

      <v-btn
        text
        color="primary"
        @click="installPrompt = null"
      >
        Dismiss
      </v-btn>
    </template>
  </v-banner>
</template>

<script>
export default {
  name: 'DownloadBanner',
  data: () => ({
    // install app
    installPrompt: null
  }),
  created() {
    // Install App
    window.addEventListener("beforeinstallprompt", e => {
      // assign prompt event
      e.preventDefault();
      this.installPrompt = e;
    });
  },
  methods: {
    // App Installation
    installApp() {
      if (this.installPrompt) {
        this.installPrompt.prompt();
        this.installPrompt = null;
      }
    }
  }
}
</script>

<style>

</style>