<template>
  <v-dialog
    v-model="refreshDialog"
    max-width="300"
    persistent
    :retain-focus="false"
  >
    <v-card outlined>
      <v-card-title>
        <v-icon
          left
          color="orange"
        >
          mdi-refresh
        </v-icon>
        <span>Refresh Required</span>
      </v-card-title>

      <v-card-text>
        {{ refreshDialogText }}
      </v-card-text>

      <v-card-actions>
        <v-spacer></v-spacer>

        <v-btn
          color="green darken-1"
          text
          @click="$refresh()"
        >
          Refresh
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>

export default {
  name: 'RefreshDialog',
  computed: {
    // Get From Vuex Store
    refreshDialogText: {
      get() { return this.$store.state.dialog.refreshDialogText }
    },
    // refreshDialog is bind with v-model, and we need a setter for it to change the variable in the store
    refreshDialog: {
      get() { return this.$store.state.dialog.refreshDialog },
    }
  },
}
</script>

<style>

</style>