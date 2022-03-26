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
      <v-btn icon @click="$_back({ name: 'Private', params: { privateId: privateId } })">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>

      <v-toolbar-title class="px-2">Message Info</v-toolbar-title>

      <v-spacer></v-spacer>
      
    </v-app-bar>

    <v-card
      tile
      flat
      class="flex-grow-1 overflow-y-auto"
      color="chatBackground"
    >

      <div 
        v-if="initLoading"
        class="fill-height d-flex flex-column justify-center align-center"
      >
        <v-progress-circular
          indeterminate
          color="primary"
        ></v-progress-circular>  
        <div class="mt-3">Getting Message Info...</div>
      </div>

      <div
        v-else-if="!exist"
        class="fill-height d-flex justify-center align-center"
      >
        <div>
          <span>This message is not available.</span>
        </div>
      </div>

      <div v-else>
        <div class="d-flex flex-column px-3 py-4">

          <MessageBox :m="message" />

        </div>

        <v-divider></v-divider>

        <v-list two-line class="pa-0">
          <v-list-item>
            <v-list-item-content>
              <v-list-item-title>
                <v-icon small color="time">
                  mdi-check-all
                </v-icon>
                Sent
              </v-list-item-title>
              <v-list-item-subtitle>
                {{ getDateTime(message.onCreate) }}
              </v-list-item-subtitle>
            </v-list-item-content>
          </v-list-item>

          <v-divider></v-divider>

          <v-list-item>
            <v-list-item-content>
              <v-list-item-title>
                <v-icon small color="light-blue">
                  mdi-check-all
                </v-icon>
                Read
              </v-list-item-title>
              <v-list-item-subtitle>
                {{ getDateTime(onSeen) }}
              </v-list-item-subtitle>
            </v-list-item-content>
          </v-list-item>

          <v-divider></v-divider>
        </v-list>
      </div>
    </v-card>
  </v-card>
</template>

<script>
import MessageBox from '../components/MessageBox.vue'

export default {
  name: 'PrivateMessageInfo',
  components: {
    MessageBox
  },
  data: () => ({
    source: null,                      // to cancel axios operation
    initLoading: true,                 // to show initial loading state
    exist: false,                      // check if message exist or not
    // message data
    message: {
      senderId: null,
      message: null,
      location: null,
      file: null,
      fileName: null,
      fileSize: null,
      onCreate: null,
      onDelete: null,
      onSeen: null,
    },
    onSeen: null,
  }),
  props: {
    privateId: {
      required: true,
    },
    messageId: {
      required: true,
    }
  },
  async created() {
    // initialize cancelToken source
    this.source = this.$axios.CancelToken.source();

    try {
      // axios get request with infinite retry ------------------------------------------
      // apply cancelToken as well
      const response = await this.$axios.get('/private/message', {
        'axios-retry': { retries: Infinity },
        cancelToken: this.source.token,
        params: {
          privateId: this.privateId,
          messageId: this.messageId
        }
      });

      // get the response data
      let data = response.data;

      // assign data
      this.message = data.message;
      this.onSeen = data.onSeen;

      // show message info
      this.exist = true;
    }
    catch (error) {

      // if operation is cancel, no need do error handling
      if (this.$axios.isCancel(error)) {
        console.log('Request cancelled');
        return;
      }
      
      console.error(error);
    }
    finally {
      this.initLoading = false;
    }
  },
  beforeDestroy() {
    // cancel any axios operation
    this.source.cancel();
  },
  methods: {
    getDateTime(iso) {
      // if no date found, return dash
      if (!iso) return "-";

      // else return the date and time
      let date = new Date(iso);
      return date.toLocaleDateString() + " at " + this.$getTime(date);
    },
  }
}
</script>

<style>

</style>