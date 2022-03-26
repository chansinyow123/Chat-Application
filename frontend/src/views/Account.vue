<template>
  <v-row 
    no-gutters 
    v-resize="onResize"
  >
    <v-col
      cols="12"
      md="4"
      lg="3"
      v-show="showOuter"
      class="fill-height"
      style="border-right: 1px solid rgba(128, 128, 128, 0.5);"
    >

      <AccountList />
      
    </v-col>
    <v-col
      cols="12"
      md="8"
      lg="9"
      v-show="showInner"
      class="fill-height"
    >
      <router-view :key="$route.fullPath"></router-view>
    </v-col>
  </v-row>
</template>

<script>
import AccountList from '../components/AccountList.vue'

export default {
  name: 'Account',
  components: {
    AccountList
  },
  data: () => ({
    showOuter: true,
    showInner: true,
    routeName: 'Account'
  }),
  created() {
    // whenever a user enter the page, assign the destination route name
    this.routeName = this.$route.name;
  },
  methods: {
    onResize() {
      // if window width is below the sm thresholds, then show all container
      if (window.innerWidth >= this.$vuetify.breakpoint.thresholds.sm) {
        this.showOuter = true;
        this.showInner = true;
      }
      else {
        // if it is the Account Route (Default Route), then only show outer
        // if it is not the Account Route (Default Route), then only show inner
        this.showOuter = this.routeName == 'Account' ? true : false;
        this.showInner = this.routeName != 'Account' ? true : false;
      }
    }
  },
  beforeRouteUpdate(to, from, next) {
    // whenever a user switching page, assign the destination route name
    // then resize the page
    this.routeName = to.name;
    this.onResize();
    next();
  }
}
</script>

<style>

</style>