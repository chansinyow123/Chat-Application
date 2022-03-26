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
      <v-btn icon @click="$_back({ name: 'Account' })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Create Account</v-toolbar-title>

    </v-app-bar>

    <v-card
      tile
      flat
      :loading="submitLoading"
      class="flex-grow-1 overflow-y-auto"
    >
      <v-form
        autocomplete="off"
        @submit.prevent="submit()"
        class="pa-3"
      >
        <v-text-field
          label="Email"
          v-model.trim="email"
          :error-messages="emailErrors"
          @input="emailInput()"
          @blur="emailInput()"
          required
          outlined
          :disabled="submitLoading"
          counter="255"
          class="mt-4" 
          :autofocus="$vuetify.breakpoint.lgAndUp"
        ></v-text-field>

        <v-text-field
          label="Name"
          v-model.trim="name"
          :error-messages="nameErrors"
          @input="nameInput()"
          @blur="nameInput()"
          required
          outlined
          :disabled="submitLoading"
          counter="30"
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
import { required, maxLength, email } from 'vuelidate/lib/validators'

export default {
  name: 'AccountCreate',
  data: () => ({
    email: '',              // email field
    name: '',               // name field
    submitLoading: false,   // is submitting form
    emailErrors: [],        // show all email errors
    nameErrors: [],         // show all name errors
  }),
  validations() {

    // check if email has been used by others
    const emailExist = (value) => this.$store.state.accounts.accounts.find(a => a.email == value) == null;

    return {
      email: { required, email, maxLength: maxLength(255), emailExist },
      name: { required, maxLength: maxLength(30) },
    }
  },
  methods: {
    emailInput() {
      this.$v.email.$touch();
      this.emailErrors = [];
      if (!this.$v.email.$dirty) return
      !this.$v.email.email && this.emailErrors.push('Must be valid e-mail.');
      !this.$v.email.maxLength && this.emailErrors.push('Email must below 255 characters long.');
      !this.$v.email.required && this.emailErrors.push('E-mail is required.');
      !this.$v.email.emailExist && this.emailErrors.push("User already exist.");
    },
    nameInput() {
      this.$v.name.$touch();
      this.nameErrors = [];
      if (!this.$v.name.$dirty) return;
      !this.$v.name.maxLength && this.nameErrors.push('Name must below 30 characters long.');
      !this.$v.name.required && this.nameErrors.push('Name is required.');
    },
    async submit() {

      // validate all field
      this.emailInput();
      this.nameInput();

      // check if any field is invalid
      // if invalid, popup error message
      if (this.$v.$invalid) {
        this.$store.commit('dialog/showErrDialog', 'Invalid Field Detected.');
        return;
      } 
    
      // show submitLoading state
      this.submitLoading = true;

      try {
        // prepare ResetPassword Url 
        const resetPasswordRoute = this.$router.getRoutes().find(r => r.name == "ResetPassword").path;
        const url = window.location.origin + resetPasswordRoute;

        // axios post request to create account
        const response = await this.$axios.post('/account', {
          email: this.email,
          name: this.name,
          path: url
        });

        // get the response data
        const data = response.data;

        // add account into vuex
        this.$store.commit('accounts/addAccounts', data);
        // clear all input
        this.clear();
        // show success message
        this.$store.commit("dialog/showSuccessDialog", `${data.email}'s account created!`);
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
      this.email = '';
      this.name = '';
      this.emailErrors = [];
      this.nameErrors = [];
    },
    status400(errors) {

      // if no path pass to post request
      // just log error message for debug purpose
      if (errors.Path) { 
        console.error(errors.Path);
        return;
      }

      // show error message
      if (errors.Email) { this.emailErrors = errors.Email; }
      if (errors.Name) { this.nameErrors = errors.Name; }

      // popup error message
      this.$store.commit("dialog/showErrDialog", 'Invalid Field Detected.');
    }
  }
}
</script>

<style>

</style>