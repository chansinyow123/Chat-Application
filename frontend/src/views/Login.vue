<template>
  <div 
    class="d-flex justify-center align-center back" 
    style="height: 100%; position: relative;"
  >
    <v-card 
      outlined
      width="90%"
      max-width="400px"
      :loading="loading"
    >
      <div class="pa-5">
        <v-card-title class="justify-center text-h5">Login</v-card-title>

        <v-card-text class="pb-0">

          <v-divider></v-divider>

          <v-form
            class="mt-5" 
            @submit.prevent="login">
            
            <v-text-field
              label="Email"
              outlined
              dense
              prepend-inner-icon="mdi-email"
              :autofocus="$vuetify.breakpoint.lgAndUp"
              v-model="email"
            ></v-text-field>

            <v-text-field
              label="Password"
              :type="showPassword ? 'text' : 'password'"
              :append-icon="showPassword ? 'mdi-eye' : 'mdi-eye-off'"
              @click:append="showPassword = !showPassword"
              outlined
              dense
              prepend-inner-icon="mdi-lock"
              v-model="password"
            ></v-text-field>

            <v-btn
              block
              small
              color="primary"
              depressed
              :loading="loading"
              type="submit"
            >
              Login
            </v-btn>
          </v-form>
        </v-card-text>

        <ForgotPasswordModel/>
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
import ForgotPasswordModel from '../components/ForgotPasswordModal.vue'

export default {
  name: 'Login',
  components: {
    ForgotPasswordModel
  },
  data: () => ({
    email: '',
    password: '',
    showPassword: false,
    loading: false,
  }),
  methods: {
    async login() {

      // logout first, just to reset all the data in the browser -----------------------------------
      this.$logout();

      // show loading state
      this.loading = true;

      try {
        // axios post request to login -------------------------------------------------------------
        const response = await this.$axios.post('/auth/login', {
          username: this.email,
          password: this.password,
        });

        // navigate to Home Page
        this.$router.push({ name: 'Home' });
      }
      catch (error) {
        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          this.$store.commit("dialog/showErrDialog", 'Invalid username and password.');
        } else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }

        // reset password input
        console.log(error);
        this.password = '';
      }
      finally {
        // remove loading state
        this.loading = false
      }
    },
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