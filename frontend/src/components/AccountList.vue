<template>
  <v-card
    tile
    flat
    class="d-flex flex-column fill-height"
  >
    <v-app-bar
      color="panel"
      flat
      tile
      dark
      class="flex-grow-0 flex-shrink-0"
    >
      <v-text-field
        hide-details
        prepend-icon="mdi-close"
        label="Search..."
        placeholder="Search..."
        single-line
        v-if="isSearching"
        autofocus
        @click:prepend="closeSearch"
        v-model="search"
      ></v-text-field>

      <v-toolbar-title 
        class="amber--text"
        v-if="!isSearching"
      >
        Admin
      </v-toolbar-title>

      <v-spacer v-if="!isSearching"></v-spacer>

      <v-btn 
        icon 
        v-if="!isSearching"
        @click="isSearching = true"
      >
        <v-icon>mdi-magnify</v-icon>
      </v-btn>

      <v-btn 
        icon 
        v-if="!isSearching"
        :to="{ name: 'AccountCreate' }"
      >
        <v-icon>mdi-plus</v-icon>
      </v-btn>

      <v-menu
        @input="$store.commit('dialog/toggleOverlay', $event)"
        left 
        origin="top right"
        transition="scale-transition"
        min-width="150px"
        v-if="!isSearching"
      >
        <template v-slot:activator="{ on, attrs }">
          <v-btn
            icon
            v-bind="attrs"
            v-on="on"
          >
            <v-icon>mdi-dots-vertical</v-icon>
          </v-btn>
        </template>
        
        <v-list class="pa-0">

          <v-list-item @click="changeTheme()">

            <v-list-item-content>
              <v-list-item-title>Dark Mode</v-list-item-title>
            </v-list-item-content>

            <v-list-item-action>
              <v-switch
                class="pl-3"
                v-model="$vuetify.theme.dark"
                disabled
              ></v-switch>
            </v-list-item-action>
          </v-list-item>
          <v-list-item :to="{ name: 'Home' }" exact>
            <v-list-item-title class="font-weight-bold">Chat</v-list-item-title>
          </v-list-item>
          <v-list-item @click="$refresh()">
            <v-list-item-title>Refresh</v-list-item-title>
          </v-list-item>
          <v-list-item @click="logout()">
            <v-list-item-title>Log out</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-menu>
    </v-app-bar>

    <div class="flex-grow-1 overflow-y-auto">
      <div 
        v-if="initLoading"
        class="text-center py-4"
      >
        <div>Getting Accounts...</div>
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>
      </div>

      <div
        v-else-if="accountFilter.length == 0"
        class="text-center py-4"
      >
        <span v-if="search">No Search Records Found.</span>
        <span v-else>No Records, Add Accounts Now!</span>
      </div>

      <v-list 
        v-else
        two-line 
        class="pa-0 pb-4"
      >
        <v-list-item-group>
          <div
            v-for="a in accountFilter"
            :key="a.id"
          >
            <v-list-item :to="{ name: 'AccountUpdate', params: { id: a.id } }">
              <v-list-item-avatar style="border: 1px solid grey;">
                <v-img
                  :src="a.image ? a.image : require(`@/assets/empty.png`)" 
                ></v-img>
              </v-list-item-avatar>
              
              <v-list-item-content>
                <v-list-item-title v-html="a.name"></v-list-item-title>
                <v-list-item-subtitle v-html="a.email"></v-list-item-subtitle>
              </v-list-item-content>
            </v-list-item>

            <v-divider></v-divider>
          </div>
          
        </v-list-item-group>
      </v-list>
    </div>
  </v-card>
</template>

<script>
export default {
  name: 'AccountList',
  data: () => ({
    search: '',           // search text
    isSearching: false,   // show searching field
    source: null,         // to cancel axios operation
  }),
  async created() {

    // initialize cancelToken source
    this.source = this.$axios.CancelToken.source();

    try {
      // axios get request with infinite retry ------------------------------------------
      // apply cancelToken as well
      const response = await this.$axios.get('/account', {
        'axios-retry': { retries: Infinity },
        cancelToken: this.source.token,
      });

      // get the response data ----------------------------------------------------------
      const data = response.data;

      // assign to accounts object 
      // and remove loading state
      this.$store.commit('accounts/setAccounts', data);
    }
    catch (error) {

      // if operation is cancel, no need do error handling
      if (this.$axios.isCancel(error)) {
        console.log('Request cancelled');
        return;
      }
    }
  },
  beforeDestroy() {
    // cancel any axios operation
    this.source.cancel();
    // reset accounts data
    this.$store.commit('accounts/resetAccount');
  },
  computed: {
    initLoading () {
      return this.$store.state.accounts.initLoading;
    },
    // search accounts with email and name, order by name
    // and apply text highlighting for search
    accountFilter() {
      const accounts = this.$store.state.accounts.accounts;
      const value = this.search.toLowerCase();
      return accounts
        .filter(a => a.name.toLowerCase().includes(value) || a.email.toLowerCase().includes(value))
        .map(a => { 
          return { 
            id: a.id, 
            image: a.image, 
            name: this.$highlightSearch(this.search, a.name),
            email: this.$highlightSearch(this.search, a.email),
          } 
        })
        .sort((x, y) => {
          let a = x.name.toUpperCase();
          let b = y.name.toUpperCase();
          return a == b ? 0 : a > b ? 1 : -1;
        });
    }
  },
  methods: {
    // close the search button
    closeSearch() {
      this.search = '';
      this.isSearching = false;
    },
    // change global theme
    changeTheme() {
      this.$vuetify.theme.dark = !this.$vuetify.theme.dark
      this.$setTheme(this.$vuetify.theme.dark);
    },
    logout() {
      this.$logout();
      this.$router.push({ name: 'Login' });
    }
  }
}
</script>

<style>

</style>