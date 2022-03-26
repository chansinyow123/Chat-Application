<template>
  <div 
    class="d-flex justify-center align-center back" 
    style="height: 100%; position: relative;"
  >
    <v-card 
      outlined
      width="90%"
      max-width="400px"
      :loading="submitLoading"
    >
      <div class="pa-5">
        <v-card-title class="justify-center text-h5">Reset Password</v-card-title>

        <v-card-text class="pb-0">

          <v-divider></v-divider>

          <v-form
            class="mt-5" 
            autocomplete="off"
            @submit.prevent="submit">
            
            <v-text-field
              label="New Password"
              outlined
              dense
              prepend-inner-icon="mdi-lock"
              :disabled="submitLoading"
              :type="showPassword ? 'text' : 'password'"
              :append-icon="showPassword ? 'mdi-eye' : 'mdi-eye-off'"
              @click:append="showPassword = !showPassword"
              v-model="password"
              :error-messages="passwordErrors"
              @input="passwordInput()"
              @blur="passwordInput()"
            ></v-text-field>

            <v-text-field
              label="Confirm Password"
              outlined
              dense
              prepend-inner-icon="mdi-lock"
              :disabled="submitLoading"
              :type="showCfmPassword ? 'text' : 'password'"
              :append-icon="showCfmPassword ? 'mdi-eye' : 'mdi-eye-off'"
              @click:append="showCfmPassword = !showCfmPassword"
              v-model="cfmPassword"
              :error-messages="cfmPasswordErrors"
              @input="cfmPasswordInput()"
              @blur="cfmPasswordInput()"
            ></v-text-field>

            <v-btn
              block
              small
              color="primary"
              depressed
              :loading="submitLoading"
              type="submit"
            >
              Reset Password
            </v-btn>
          </v-form>
        </v-card-text>

        <v-card-actions class="justify-center">
          <v-btn
            plain
            small
            :to="{ name: 'Login' }"
          >
            Login Here
          </v-btn>
        </v-card-actions>
      </div>
    </v-card>

    <v-switch
      class="theme-switch"
      v-model="$vuetify.theme.dark"
      @change="$setTheme($vuetify.theme.dark)"
      label="Dark Mode"
    ></v-switch>

  </div>
</template>

<script>
import { required, minLength, sameAs } from 'vuelidate/lib/validators'

export default {
  name: 'ResetPassword',
  data: () => ({
    password: '',
    cfmPassword: '',
    showPassword: false,
    showCfmPassword: false,
    passwordErrors: [],
    cfmPasswordErrors: [],
    submitLoading: false,
    token: '',
    uid: '',
  }),
  validations() {
    return {
      password: { required, minLength: minLength(6) },
      cfmPassword: { required, sameAs: sameAs('password') },
    }
  },
  created() {
    // get the query from route
    this.token = this.$route.query.token ?? 'token';
    this.uid = this.$route.query.uid ?? 'uid';
  },
  methods: {
    passwordInput() {
      this.$v.password.$touch();
      this.passwordErrors = [];
      if (!this.$v.password.$dirty) return;
      !this.$v.password.required && this.passwordErrors.push('New password is required.');
      !this.$v.password.minLength && this.passwordErrors.push('New password must above 6 characters long.');
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
      this.passwordInput();
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

        // axios post request to create account
        const response = await this.$axios.post('/password/reset-password', {
          token: this.token,
          userId: this.uid,
          password: this.password,
        });

        // show success message
        this.$store.commit("dialog/showSuccessDialog", 'Password has been set!');
        // navigate user to login page
        this.$router.push({ name: 'Login' });
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
            case 404: 
              this.$store.commit("dialog/showErrDialog", 'Reset password link is invalid/expired.');
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
    status400(errors) {

      // show error message
      if (errors.Password) { this.passwordErrors = errors.Password; }

      // popup error message
      this.$store.commit("dialog/showErrDialog", 'Invalid Field Detected.');
    }
  },
}
</script>

<style scoped>
.back {
	background: linear-gradient(-45deg, #ee7752, #e73c7e, #23a6d5, #23d5ab);
	background-size: 400% 400%;
	animation: gradient 15s ease infinite;
	height: 100vh;
}

@keyframes gradient {
	0% {
		background-position: 0% 50%;
	}
	50% {
		background-position: 100% 50%;
	}
	100% {
		background-position: 0% 50%;
	}
}

.theme-switch {
  position: absolute;
  margin: 0;
  top: 5px;
  right: 8px;
}

</style>