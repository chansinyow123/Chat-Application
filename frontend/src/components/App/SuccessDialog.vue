<template>
  <v-dialog
    v-model="successDialog"
    max-width="300"
    @click:outside="closeSuccessDialog()"
    :retain-focus="false"
  >
    <v-card outlined>
      <v-card-title>
        <v-icon
          left
          color="green"
        >
          mdi-check-circle
        </v-icon>
        <span>Success</span>
      </v-card-title>

      <v-card-text>
        {{ successDialogText }}
      </v-card-text>

      <v-card-actions>
        <v-spacer></v-spacer>

        <v-btn
          color="green darken-1"
          text
          @click="closeSuccessDialog()"
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
  name: 'SuccessDialog',
  computed: {
    // Get From Vuex Store
    successDialogText: {
      get() { return this.$store.state.dialog.successDialogText }
    },
    // successDialog is bind with v-model, and we need a setter for it to change the variable in the store
    successDialog: {
      get() { return this.$store.state.dialog.successDialog },
    }
  },
  methods: {
    // Store Mutation ---------------------------------------------------------------------------
    ...mapMutations({
      closeSuccessDialog: 'dialog/closeSuccessDialog',
    }),
  }
}
</script>

<style>

</style>