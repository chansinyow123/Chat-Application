<template>
  <v-dialog
    v-model="errDialog"
    max-width="300"
    @click:outside="closeErrDialog()"
    :retain-focus="false"
  >
    <v-card outlined>
      <v-card-title>
        <v-icon
          left
          color="red"
        >
          mdi-alert
        </v-icon>
        <span>Error</span>
      </v-card-title>

      <v-card-text>
        {{ errDialogText }}
      </v-card-text>

      <v-card-actions>
        <v-spacer></v-spacer>

        <v-btn
          color="green darken-1"
          text
          @click="closeErrDialog()"
        >
          Ok
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import { mapMutations } from 'vuex'

export default {
  name: 'ErrorDialog',
  computed: {
    // Get From Vuex Store
    errDialogText: {
      get() { return this.$store.state.dialog.errDialogText }
    },
    // errDialog is bind with v-model, and we need a setter for it to change the variable in the store
    errDialog: {
      get() { return this.$store.state.dialog.errDialog },
    }
  },
  methods: {
    // Store Mutation ---------------------------------------------------------------------------
    ...mapMutations({
      closeErrDialog: 'dialog/closeErrDialog',
    }),
  }
}
</script>

<style>

</style>