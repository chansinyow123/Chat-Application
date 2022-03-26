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
      <v-btn icon @click="$_back({ name: 'Private', params: { privateId: privateId } })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Account Info</v-toolbar-title>

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
        <div class="mt-3">Getting Account...</div>
      </div>

      <div
        v-else-if="!account"
        class="fill-height d-flex justify-center align-center"
      >
        <div>
          <span>This account is not available.</span>
        </div>
      </div>

      <div v-else>
        <div class="d-flex justify-center mt-2">
          <v-avatar size="200" style="border: 1px solid grey;">
            <v-img
              :src="account.image ? account.image : require(`@/assets/empty.png`)" 
            ></v-img>
          </v-avatar>
        </div>
        <div class="d-flex justify-center mt-2 text--primary"> {{ account.name }} </div>
        <div class="d-flex justify-center text--secondary"> {{ account.email }} </div>
      </div>

    </v-card>
  </v-card>
</template>

<script>
export default {
  name: 'PrivateInfo',
  props: {
    privateId: {
      type: String,
      required: true,
    }
  },
  computed: {
    initLoading() {
      // show init loading state
      return this.$store.state.accounts.initLoading;
    },
    account() {
      return this.$store.state.accounts.accounts.find(a => a.id == this.privateId);
    },
  },
}
</script>

<style>

</style>