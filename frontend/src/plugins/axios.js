import Vue from 'vue';
import axios from 'axios'
import axiosRetry from 'axios-retry';
import router from '../router'
import store from '../store'

const http = {
  install (Vue) {
    // Set up axios default
    axios.defaults.baseURL = `${window.location.origin}/api`;

    // set up axiosRetry with default 0 retry
    // and default 2 second retry interval
    axiosRetry(axios, { 
      retries: 0,
      retryDelay: (retryCount) => {
        console.log('retrying', retryCount);
        return 2000;
      }
    });

    // Axios response interceptor
    axios.interceptors.response.use((response) => {
      // Any status code that lie within the range of 2xx cause this function to trigger
      // Do something with response data
      return response;
    }, (error) => {
      // Any status codes that falls outside the range of 2xx cause this function to trigger
      // Do something with response error
      if (error.response) {
        // The request was made and the server responded with a status code
        // that falls out of the range of 2xx
        switch (error.response.status) {
          case 401: // Unauthorized
            // show error and logout user to login page
            store.commit("dialog/showErrDialog", 'Unauthorized Access');
            Vue.prototype.$logout();
            router.push({ name: 'Login' });
            break;
          case 403: // Forbidden
            // show error and navigate user to home page
            store.commit("dialog/showErrDialog", 'Forbid Access');
            router.push({ name: 'Home' });
            break;
        }

        // if server error, popup error message
        if (error.response.status >= 500) {
          store.commit("dialog/showRefreshDialog", 'Unexpected error occured, Refresh the page.');
        }
      }

      return Promise.reject(error);
    });

    // Add axios to global variable
    Vue.prototype.$axios = axios;
  }
}

// Apply axios as plugin
Vue.use(http) 