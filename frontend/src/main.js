import Vue from 'vue'
import App from './App.vue'
import './registerServiceWorker'
import store from './store'
import router from './router'
import vuetify from './plugins/vuetify'
import './plugins/methods'
import './plugins/axios'
import './mixins/back'
import './assets/global.css'
import Vuelidate from 'vuelidate'

Vue.config.productionTip = false;

// use Vuelidate for client side validation
Vue.use(Vuelidate);

new Vue({
  router,
  store,
  vuetify,
  render: h => h(App)
}).$mount('#app');
