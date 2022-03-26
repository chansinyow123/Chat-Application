import Vue from 'vue'

const back = {
  data: () => ({
    fromRoute: null
  }),
  beforeRouteEnter (to, from, next) {
    next(vm => {
      vm.fromRoute = from;
    })
  },
  methods: {
    /**
    * Handle Back
    * @desc Extends default router back functionality
    * @param {string} fallback - The fallback path if there is no history to use with $router.back(). This is usually the case if the page was visited directly or from another site
    **/
    $_back (fallback) {
      if (!this.fromRoute || !this.fromRoute.name) {
        this.$router.replace(fallback);
      } else {
        this.$router.back();
      }
    }
  }
}

// apply back methods to global vue
Vue.mixin(back)