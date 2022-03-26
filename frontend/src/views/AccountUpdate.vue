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

      <v-toolbar-title class="px-2">Edit Account</v-toolbar-title>

    </v-app-bar>

    <v-card
      tile
      flat
      :loading="submitLoading"
      class="flex-grow-1 overflow-y-auto"
    >
      <div 
        v-if="initLoading"
        class="text-center py-3"
      >
        <div>Getting Account Info...</div>
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
      </div>

      <div 
        v-else-if="!this.account"
        class="text-center py-3"
      >
        No account found.
      </div>

      <div 
        v-else
        class="pa-3"
      >
        <div class="d-flex justify-center mt-2">
          <v-avatar size="80" style="border: 1px solid grey;">
            <v-img 
              :src="image ? image : require(`@/assets/empty.png`)" 
            ></v-img>
          </v-avatar>
        </div>

        <v-form
          autocomplete="off"
          @submit.prevent="submit"
        >
          <v-text-field
            label="Email"
            v-model.trim="email"
            required
            outlined
            disabled
            class="mt-5" 
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
            :autofocus="$vuetify.breakpoint.lgAndUp"
          ></v-text-field>

          <div class="mt-4">
            <v-btn
              color="primary"
              depressed
              :loading="submitLoading"
              type="submit"
            >
              Edit
            </v-btn>

            <v-btn
              color="error"
              depressed
              :loading="submitLoading"
              class="ml-4"
              @click="confirmDialog = true"
            >
              Delete
            </v-btn>
          </div>
        </v-form>
      </div>
    </v-card>

    <v-dialog
      v-model="confirmDialog"
      max-width="300"
    >
      <v-card outlined>
        <v-card-title>
          <v-icon
            left
            color="red"
          >
            mdi-alert
          </v-icon>
          <span>Delete Account?</span>
        </v-card-title>

        <v-card-text>
          Are you sure you want to delete {{ name }}'s account?
        </v-card-text>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn
            color="green darken-1"
            text
            @click="remove()"
          >
            Yes
          </v-btn>
          <v-btn
            color="green darken-1"
            text
            @click="confirmDialog = false"
          >
            No
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script>
import { required, maxLength } from 'vuelidate/lib/validators'

export default {
  name: 'AccountUpdate',
  props: {
    id: {
      type: String,
      required: true,
    },
  },
  data: () => ({
    image: '',              // show profile image
    email: '',              // v-model email
    name: '',               // v-model name
    submitLoading: false,   // show submitLoading state
    nameErrors: [],         // show name errors
    confirmDialog: false,
  }),
  validations() {
    return {
      name: { required, maxLength: maxLength(30) },
    }
  },
  computed: {
    initLoading() {
      return this.$store.state.accounts.initLoading;
    },
    account() {
      // search accounts with email and name, order by name
      // and apply text highlighting for search
      let account = this.$store.state.accounts.accounts.find(a => a.id == this.id);
      if (account) {
        this.image = account.image;
        this.email = account.email;
        this.name = account.name;
      }
      return account;
    }
  },
  methods: {
    nameInput() {
      this.$v.name.$touch();
      this.nameErrors = [];
      if (!this.$v.name.$dirty) return;
      !this.$v.name.maxLength && this.nameErrors.push('Name must below 30 characters long.');
      !this.$v.name.required && this.nameErrors.push('Name is required.');
    },
    async submit() {

      // validate all field
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

        // axios put request to edit account
        const response = await this.$axios.put(`/account/${this.id}`, {
          name: this.name,
        });

        // get the response data
        const data = response.data;

        // edit account into vuex
        this.$store.commit('accounts/updateAccount', data);
        // show success message
        this.$store.commit("dialog/showSuccessDialog", `${this.email}'s account edited!`);
      }
      catch (error) {

        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = error.response.data;
          const errStatus = error.response.status;

          switch (errStatus) {
            case 400: // if field has error
              this.status400(errResponse.errors);
              break;
            case 404: // if id is not exist
              this.status404();
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
    // Remove Account -------------------------------------------------------------------------------
    async remove() {

      // close confirm dialog
      this.confirmDialog = false;
      // show submitLoading state
      this.submitLoading = true;

      try {

        // axios delete request to delete account
        const response = await this.$axios.delete(`/account/${this.id}`);

        // get the response data
        const data = response.data;

        // delete account into vuex
        this.$store.commit('accounts/deleteAccount', data);
        // show success message
        this.$store.commit("dialog/showSuccessDialog", `${this.email}'s account deleted!`);
        // navigate away, if is still on this page
        if (this.$route.name == "AccountUpdate") {
          this.$router.replace({ name: 'Account' });
        }
      }
      catch (error) {

        if (error.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = error.response.data;
          const errStatus = error.response.status;

          switch (errStatus) {
            case 400: // if field has error
              this.status400(errResponse.errors);
              break;
            case 404: // if id is not exist
              this.status404();
              break;
            case 406: // Not allow admin delete himself
              this.$store.commit('dialog/showErrDialog', 'Not allow to delete your own account.');
              break;
            case 405: // Not allow to delete account that already have private chat / group
              this.$store.commit('dialog/showErrDialog', 'Cannot delete this account as this account already have private chat / group chat available.')
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
      if (errors.Name) { this.nameErrors = errors.Name; }
      // popup error message
      this.$store.commit("dialog/showErrDialog", 'Invalid Field Detected.');
    },
    status404() {
      // show error message
      this.$store.commit("dialog/showErrDialog", "No account found with this id.");
    },
  },
}
</script>

<style>

</style>