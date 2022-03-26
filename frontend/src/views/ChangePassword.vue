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
      <v-btn icon @click="$_back({ name: 'Home' })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Change Password</v-toolbar-title>

    </v-app-bar>

    <v-card
      tile
      flat
      :loading="submitLoading"
      class="flex-grow-1 overflow-y-auto"
    >
      <v-form
        class="pa-3" 
        autocomplete="off"
        @submit.prevent="submit()"
      >
        <v-text-field
          label="Old Password"
          required
          outlined
          :disabled="submitLoading"
          :type="showOldPassword ? 'text' : 'password'"
          :append-icon="showOldPassword ? 'mdi-eye' : 'mdi-eye-off'"
          @click:append="showOldPassword = !showOldPassword"
          v-model="oldPassword"
          :error-messages="oldPasswordErrors"
          @input="oldPasswordInput()"
          @blur="oldPasswordInput()"
          class="mt-4" 
        ></v-text-field>

        <v-text-field
          label="New Password"
          required
          outlined
          :disabled="submitLoading"
          :type="showNewPassword ? 'text' : 'password'"
          :append-icon="showNewPassword ? 'mdi-eye' : 'mdi-eye-off'"
          @click:append="showNewPassword = !showNewPassword"
          v-model="newPassword"
          :error-messages="newPasswordErrors"
          @input="newPasswordInput()"
          @blur="newPasswordInput()"
          class="mt-4" 
        ></v-text-field>

        <v-text-field
          label="Confirm Password"
          required
          outlined
          :disabled="submitLoading"
          :type="showCfmPassword ? 'text' : 'password'"
          :append-icon="showCfmPassword ? 'mdi-eye' : 'mdi-eye-off'"
          @click:append="showCfmPassword = !showCfmPassword"
          v-model="cfmPassword"
          :error-messages="cfmPasswordErrors"
          @input="cfmPasswordInput()"
          @blur="cfmPasswordInput()"
          class="mt-4" 
        ></v-text-field>

        <div class="mt-4">
          <v-btn
            color="primary"
            depressed
            :loading="submitLoading"
            type="submit"
          >
            Create
          </v-btn>

          <v-btn
            color="secondary"
            depressed
            :loading="submitLoading"
            class="ml-4" 
            @click="clear()"
          >
            Clear
          </v-btn>
        </div>
      </v-form>
    </v-card>
  </v-card>
</template>

<script>
import { required, minLength, sameAs } from 'vuelidate/lib/validators'

export default {
  name: 'ChangePassword',
  data: () => ({
    oldPassword: '',
    newPassword: '',
    cfmPassword: '',
    showOldPassword: false,
    showNewPassword: false,
    showCfmPassword: false,
    submitLoading: false,
    oldPasswordErrors: [],
    newPasswordErrors: [],
    cfmPasswordErrors: [],
  }),
  validations () {
    return {
      oldPassword: { required },
      newPassword: { required, minLength: minLength(6) },
      cfmPassword: { required, sameAs: sameAs('newPassword') }
    }
  },
  methods: {
    oldPasswordInput() {
      this.$v.oldPassword.$touch();
      this.oldPasswordErrors = [];
      if (!this.$v.oldPassword.$dirty) return;
      !this.$v.oldPassword.required && this.oldPasswordErrors.push('Old password is required.');
    },
    newPasswordInput() {
      this.$v.newPassword.$touch();
      this.newPasswordErrors = [];
      if (!this.$v.newPassword.$dirty) return;
      !this.$v.newPassword.required && this.newPasswordErrors.push('New password is required.');
      !this.$v.newPassword.minLength && this.newPasswordErrors.push('New password must above 6 characters long.');
      this.cfmPasswordInput();
    },
    cfmPasswordInput() {
      this.$v.cfmPassword.$touch();
      this.cfmPasswordErrors = [];
      if (!this.$v.cfmPassword.$dirty) return;
      !this.$v.cfmPassword.required && this.cfmPasswordErrors.push('Confirm password is required.');
      !this.$v.cfmPassword.sameAs && this.cfmPasswordErrors.push('Confirm password must be the same as New Password.');
    },
    async submit() {
      // validate all field
      this.oldPasswordInput();
      this.newPasswordInput();
      this.cfmPasswordInput();

      // check if any field is invalid
      // if invalid, popup error message
      if (this.$v.$invalid) {
        this.$store.commit('dialog/showErrDialog', 'Invalid Field Detected.');
        return;
      } 
    
      // show submitLoading state
      this.submitLoading = true;

      try {

        // axios put request to change password
        await this.$axios.post('/password/change-password', {
          oldPassword: this.oldPassword,
          newPassword: this.newPassword,
        });

        // clear all input
        this.clear();
        // show success message
        this.$store.commit("dialog/showSuccessDialog", 'Password Changed!');
      }
      catch (error) {

        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = error.response.data;
          const errStatus = error.response.status;

          switch (errStatus) {
            case 400: 
              this.status400(errResponse.errors);
              break;
          }
        } else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }
      finally {
        // remove submitLoading state
        this.submitLoading = false;
      }
    },
    clear() {
      this.oldPassword = '';
      this.newPassword = '';
      this.cfmPassword = '';
      this.oldPasswordErrors = [];
      this.newPasswordErrors = [];
      this.cfmPasswordErrors = [];
    },
    status400(errors) {

      // show error message
      if (errors.OldPassword) { this.oldPasswordErrors = errors.OldPassword; }
      if (errors.NewPassword) { this.newPasswordErrors = errors.NewPassword; }

      // popup error message
      this.$store.commit("dialog/showErrDialog", 'Invalid Field Detected.');
    }
  }
}
</script>

<style>

</style>