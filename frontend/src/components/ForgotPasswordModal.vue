<template>
  <div>
    <v-card-actions class="justify-center">
      <v-btn
        plain
        small
        @click="reveal = true"
      >
        Forgot Password?
      </v-btn>
    </v-card-actions>

    <!-- Forgot Password Model ========================================================================================================= -->
    <v-expand-transition>
      <v-card
        :loading="submitLoading"
        v-show="reveal"
        class="transition-fast-in-fast-out v-card--reveal"
        style="height: 100%;"
      >

        <div class="pa-5">
          <v-card-title class="justify-center text-h5">Forgot Password</v-card-title>

          <v-card-text>
            <v-divider></v-divider>
            <p class="mt-3">Provide your account's email here, we will send you a reset password link to your email!</p>

            <v-form
              class="mt-5" 
              autocomplete="off"
              @submit.prevent="submit()"
            >
              <v-text-field
                label="Email"
                outlined
                prepend-inner-icon="mdi-email"
                :disabled="submitLoading"
                v-model.trim="email"
                :error-messages="emailErrors"
                @input="emailInput()"
                @blur="emailInput()"
                :autofocus="$vuetify.breakpoint.lgAndUp"
                dense
              ></v-text-field>

              <v-btn
                block
                color="purple"
                dark
                small
                depressed
                type="submit"
                :loading="submitLoading"
              >
                Send Link
              </v-btn>
            </v-form>
          </v-card-text>

          <v-card-actions class="pt-0">
            <v-btn
              text
              color="teal accent-4"
              @click="closeModal()"
              small
            >
              Back to login
            </v-btn>
          </v-card-actions>
        </div>
      </v-card>
    </v-expand-transition>
  </div>
</template>

<script>
import { required, email } from 'vuelidate/lib/validators'

export default {
  name: 'ForgotPasswordModal',
  data: () => ({
    email: '',
    emailErrors: [],
    reveal: false,
    submitLoading: false,
  }),
  validations() {
    return {
      email: { required, email },
    }
  },
  methods: {
    emailInput() {
      this.$v.email.$touch();
      this.emailErrors = [];
      if (!this.$v.email.$dirty) return
      !this.$v.email.email && this.emailErrors.push('Must be valid e-mail.');
      !this.$v.email.required && this.emailErrors.push('E-mail is required.');
    },
    async submit() {

      // validate all field
      this.emailInput();

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

        // axios post request to send reset password link
        const response = await this.$axios.post('/password/forgot-password', {
          email: this.email,
          path: url
        });

        // clear all input and close modal
        this.closeModal();
        // show success message
        this.$store.commit("dialog/showSuccessDialog", 'If this account exist, the reset password link is sent to this email!');
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
            default:
              this.$store.commit("dialog/showErrDialog", 'Unhandle Response.');
          }
        } else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }
      finally {
        this.submitLoading = false;
      }
    },
    closeModal() {
      this.email = '';
      this.emailErrors = [];
      this.reveal = false;
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

      // popup error message
      this.$store.commit("dialog/showErrDialog", 'Invalid Field Detected.');
    }
  },
}
</script>

<style>
.v-card--reveal {
  bottom: 0;
  left: 0;
  opacity: 1 !important;
  position: absolute;
  width: 100%;
}
</style>