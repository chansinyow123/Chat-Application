<template>
  <v-row 
    no-gutters 
    v-resize="onResize"
    class="fill-height"
  >
    <v-col
      cols="12"
      :lg="showInner ? 7 : 12"
      v-show="showOuter"
      class="fill-height"
      style="border-right: 1px solid rgba(128, 128, 128, 0.5);"
    >

      <GroupMessage :groupId="groupId" />
      
    </v-col>
    <v-col
      cols="12"
      lg="5"
      v-show="showInner"
      class="fill-height"
    >
      <router-view :key="$route.fullPath"></router-view> 
    </v-col>
  </v-row>
</template>

<script>
import GroupMessage from '../components/GroupMessage.vue'

export default {
  name: 'Group',
  props: {
    groupId: {
      required: true,
    }
  },
  components: {
    GroupMessage
  },
  data: () => ({
    showOuter: true,
    showInner: true,
    routeName: 'Group',
  }),
  created() {
    // whenever a user enter the page, assign the destination route name
    this.routeName = this.$route.name;
  },
  methods: {
    onResize() {
      // if screen is larger than md, then always showOuter, and showInner if there is inner route
      // else, either showOuter or showInner depends on whether there is inner route
      if (window.innerWidth >= this.$vuetify.breakpoint.thresholds.md) {
        this.showOuter = true;
        this.showInner = this.routeName != 'Group' ? true : false;
      }
      else {
        this.showOuter = this.routeName == 'Group' ? true : false;
        this.showInner = this.routeName != 'Group' ? true : false;
      }
    },
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

