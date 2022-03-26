<template>
  <v-card
    tile
    flat
    class="fill-height"
  >
    <div 
      v-if="initLoading"
      class="fill-height d-flex flex-column justify-center align-center"
    >
      <v-progress-circular
        indeterminate
        color="primary"
      ></v-progress-circular>  
      <div class="mt-3">Getting Chats...</div>
    </div>

    <div
      v-else-if="!group"
      class="fill-height d-flex justify-center align-center"
    >
      <div>
        <span>This group is not available.</span>
        <span class="text-decoration-underline light-blue--text pa-1" style="cursor: pointer;" @click="$_back({ name: 'Home' })">back</span>
      </div>
    </div>

    <div 
      v-else
      class="d-flex flex-column fill-height"
    >
      <v-app-bar
        flat
        tile
        color="panel"
        dark
        class="flex-grow-0"
      >
        <v-btn icon @click="$_back({ name: 'Home' })">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>

        <div class="d-flex px-md-2 flex-grow-1" style="min-width: 0;">
          <div class="d-flex justify-center align-center">
            <v-avatar size="40" style="border: 1px solid grey;">
              <v-img
                :src="group.image ? group.image : require(`@/assets/empty-group.png`)" 
              ></v-img>
            </v-avatar>
          </div>
          <div class="d-flex flex-column pl-3" style="min-width: 0;">
            <div class="text-body-1 text-truncate" >{{ group.name }}</div>
            <div class="text-caption grey--text text--lighten-1 text-truncate">group</div>
          </div>
        </div>

        <v-btn 
          fab
          small
          v-if="isTakingSpeech" 
          @click="$cancelSpeech()"
          color="red"
        >
          <v-icon>mdi-stop</v-icon>
        </v-btn>
        
        <v-menu
          @input="$store.commit('dialog/toggleOverlay', $event)"
          left 
          origin="top right"
          transition="scale-transition"
          min-width="150px"
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
            <v-list-item v-if="!group.isExit" :to="{ name: 'GroupInfo' }">
              <v-list-item-title>Group Info</v-list-item-title>
            </v-list-item>
            <v-list-item v-if="!group.isExit" @click="exitGroup()">
              <v-list-item-title>Exit Group</v-list-item-title>
            </v-list-item>
            <v-list-item @click="$refresh()">
              <v-list-item-title>Refresh</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>

      </v-app-bar>

      <div 
        class="flex-grow-1 overflow-y-auto chatBackground d-flex flex-column justify-end"
        @drop.stop.prevent="uploadFiles($event.dataTransfer.files)"
        @dragover.stop.prevent="$event.dataTransfer.dropEffect = 'copy'"
      >
        <div 
          class="overflow-y-auto px-3 pb-3"
          v-scroll.self="onScroll" 
          ref="chatContainer" 
        >
          
          <div 
            v-show="haveMoreChat"
            class="text-center mt-3"
          >
            <v-progress-circular
              indeterminate
              color="primary"
            ></v-progress-circular>
          </div>
          
          <div class="d-flex justify-center" v-if="messages.length <= 0">
            <div class="messageInfo rounded-lg px-3 py-2 text-caption">
              There are no chat available, start to chat now!
            </div>
          </div>

          <div 
            v-else 
            v-for="(m, i) of messages"
          >

            <div class="d-flex justify-center mt-3" v-if="hasDifferentDate(i)">
              <div class="messageInfo rounded-lg px-3 py-2 text-caption">
                {{ (new Date(m.onCreate).toLocaleDateString() == new Date().toLocaleDateString()) ? "Today" : new Date(m.onCreate).toLocaleDateString() }}
              </div>
            </div>

            <div class="d-flex justify-center mt-3" v-if="m.info">
              <div class="messageInfo rounded-lg px-3 py-2 text-caption">
                {{ m.info }}
              </div>
            </div>

            <div 
              v-else
              class="d-flex flex-column"
              :class="[getMarginTop(i)]" 
            >
              <MessageBox :m="m" :show-name="showName(i)">
                <v-list-item :to="{ name: 'GroupMessageInfo', params: { groupId: groupId, messageId: m.id } }">
                  <v-list-item-title>Message Info</v-list-item-title>
                </v-list-item>
                <v-list-item v-if="!group.isExit && !m.onDelete && m.senderId == uid" @click="deleteMessage(m)">
                  <v-list-item-title>Delete Message</v-list-item-title>
                </v-list-item>
                <v-list-item v-if="m.message" @click="$store.dispatch('speech/openSpeechMessage', m.message)">
                  <v-list-item-title>Speech Message</v-list-item-title>
                </v-list-item>
              </MessageBox>

              <div v-if="m.failed" class="d-flex justify-end text-caption">
                <div>
                  <div> Message failed to send </div>
                  <div class="d-flex justify-end">
                    <span class="error--text pr-1" style="cursor: pointer;" @click="removeMessage(i)">Delete</span>
                    /
                    <span class="blue--text pl-1" style="cursor: pointer;" @click="tryAgain(m, i)">Try again</span>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>

      <TextArea 
        @upload-files="uploadFiles($event)" 
        @send-message="sendMessage($event)"
        v-if="!group.isExit"
      />
      <div
        v-else 
        class="panel py-3 d-flex justify-center align-center white--text"
      >
        You are no longer a member in this group
      </div>
    </div>
  </v-card>
</template>

<script>
import MessageBox from '../components/MessageBox.vue'
import TextArea from '../components/TextArea.vue'

export default {
  name: 'PrivateMessage',
  props: {
    groupId: {
      required: true,
    }
  },
  components: {
    MessageBox,
    TextArea,
  },
  data: () => ({
    uid: null,                         // get this user uid from cookie
    haveMoreChat: true,                // to determine whether all the previos chat has been loaded
    loadingChatHeight: null,           // record down the chatContainer height while loading previous chat
    source: null,                      // to cancel axios operation
    sizeLimit: 10485760,               // to validate file size (10 mb)
  }),
  computed: {
    initLoading() {
      // show init loading state
      return this.$store.state.chats.initLoading;
    },
    group() {
      return this.$store.state.groups.groups.find(a => a.id == this.groupId);
    },
    messages() {
      // get the chat ------------------------------------------------------------------------------------------------------------
      let chat = this.$store.state.chats.chats.find(c => c.groupId == this.groupId);

      // if there is no messages, assign haveMoreChat to false to prevent onScroll loading ---------------------------------------
      // then do nothing
      if (!chat) {
        this.haveMoreChat = false;
        return [];
      }

      // if there are notification, post request to seen the message -------------------------------------------------------------
      if (chat.notificationCount > 0) {
        this.seen();
        return chat.messages;
      }

      // get the chat container --------------------------------------------------------------------------------------------------
      let chatContainer = this.$refs.chatContainer;
      let isScrollBottom = false;

      // if chatContainer is empty, means it is first load, then scroll to bottom
      // else check if the scrollbar is at the bottom, then scroll to bottom
      if ((!chatContainer) || (chatContainer && chatContainer.scrollHeight - chatContainer.scrollTop <= chatContainer.clientHeight + 10)) {
        isScrollBottom = true;
      }
      
      // we use $nextTick is because we have to wait for DOM to update in order to scroll to bottom -----------------------------
      this.$nextTick(() => {

        if (this.loadingChatHeight) {
          // if loadingChatHeight has value, means previous chat loaded, and we do not allow the scroll bar to stick to top
          // then assign loadingChatHeight back to null
          this.$refs.chatContainer.scrollTop = this.$refs.chatContainer.scrollHeight - this.loadingChatHeight;
          this.loadingChatHeight = null;
        }
        else if (isScrollBottom) {
          // scroll to bottom
          this.$refs.chatContainer.scrollTop = this.$refs.chatContainer.scrollHeight;
        }

        // call onScroll method whenever a message is prepend or apppend
        this.onScroll();
      });

      // return array of messages
      return chat.messages;
    },
    isTakingSpeech() {
      return this.$store.state.speech.isTakingSpeech;
    },
  },
  created() {

    // get the user uid from cookie
    this.uid = this.$getCookie('uid');

    // initialize cancelToken source
    this.source = this.$axios.CancelToken.source();
  },
  beforeDestroy() {
    // cancel any axios operation
    this.source.cancel();

    // after leaving the page, limit chat caching for performance purpose
    if (this.messages) {
      this.$store.commit('chats/limitGroupChat', this.groupId);
    }
  },
  methods: {
    async onScroll() {
      // if scrollbar is not at the top, do nothing
      // if does not have more chat, do nothing
      // if loadingChatHeight has value, do nothing
      if (this.$refs.chatContainer.scrollTop > 50 || !this.haveMoreChat || this.loadingChatHeight) {
        return;
      }

      // record the chatContainer scroll height to later prevent infinite scroll top
      this.loadingChatHeight = this.$refs.chatContainer.scrollHeight;

      try {
        // axios get request with infinite retry ------------------------------------------
        // apply cancelToken as well
        const response = await this.$axios.get('/group/load-chat', {
          'axios-retry': { retries: Infinity },
          cancelToken: this.source.token,
          params: {
            count: this.messages.length,
            groupId: this.groupId
          }
        });

        // get the response data ----------------------------------------------------------
        const { haveMoreChat, messages } = response.data;

        // assign haveMoreChat
        this.haveMoreChat = haveMoreChat;

        // if messages length is 0, then do nothing
        if (!messages.length) return;

        // prepend private chat 
        this.$store.commit('chats/prependGroupChat', {
          groupId: this.groupId,
          messages: messages,
        });
        
      }
      catch (error) {

        // if operation is cancel, no need do error handling
        if (this.$axios.isCancel(error)) {
          console.log('Request cancelled');
          return;
        }

        console.log(error);
      }
    },
    hasDifferentDate(i) {
      // get the last message
      let previousMessage = this.messages[i-1];

      // if last message is null, then return true
      if (!previousMessage) return true;

      // get previous date and current date
      let previousDate = new Date(previousMessage.onCreate).toLocaleDateString();
      let currentDate = new Date(this.messages[i].onCreate).toLocaleDateString();

      // if not the same with previous date, then show date
      // else dont show
      if (currentDate != previousDate) return true;
      else return false;
    },
    showName(i) {

      // get this message
      let currentMessage = this.messages[i];

      // if currentMessage senderId is the same as this user's uid, dont show name
      if (currentMessage.senderId == this.uid) return false;

      // get the last message
      let previousMessage = this.messages[i-1];

      // if last message is null, then show name
      if (!previousMessage) return true;

      // if current message and the last message of uid is different, show name
      // else dont show name
      if (currentMessage.senderId != previousMessage.senderId) return true;
      else return false;
    },
    getMarginTop(i) {
      // get the last message
      let previousMessage = this.messages[i-1];

      // prepare largest margin top
      let largeMarginTop = 'mt-4';

      // if last message is null, then return mt-3;
      if (!previousMessage) return largeMarginTop;

      // get previous date and current date
      let previousDate = new Date(previousMessage.onCreate).toLocaleDateString();
      let currentDate = new Date(this.messages[i].onCreate).toLocaleDateString();

      // if not the same, then return largest margin top
      if (currentDate != previousDate) return largeMarginTop;

      // if last message of senderId is the same with current senderId, then return mt-1
      // else return largest margin top
      if (this.messages[i].senderId == previousMessage.senderId) return 'mt-1';
      else return largeMarginTop;
    },
    async seen() {
      // axios post request for seen ----------------------------------------------
      await this.$axios.post('/group/seen', {
        groupId: this.groupId
      });

      // remove notification count
      this.$store.commit('chats/removeGroupNotification', this.groupId);
    },
    async sendMessage({ textarea, file, location }) {

      // prepare message data
      let message = {
        id: null,
        failed: null,
        fileObject: (file) ? file : null,
        senderId: this.uid,
        info: null,
        message: textarea,
        location: (!file && location) ? location : null,
        file: (file) ? await this.$toBase64(file) : null,
        contentType: (file) ? file.type : null,
        fileName: (file) ? file.name : null,
        fileSize: (file) ? file.size : null,
        onCreate: new Date(),
        onDelete: null,
        onSeen: false,
      };

      // append group chat
      this.$store.commit('chats/appendGroupMessage', {
        groupId: this.groupId,
        message: message
      });

      // prepare formdata
      let formData = new FormData();
      formData.append("groupId", this.groupId);
      formData.append("message", textarea);
      if (file != null) {
        formData.append("file", file);
      }
      else if (location != null) {
        formData.append("location", location);
      }

      try {
        // send group message with post request
        let response = await this.$axios.post('/group/send-message', formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });

        // remove pending group chat by comparing message object references
        this.$store.commit('chats/removePendingGroupChat', {
          groupId: this.groupId,
          message: message
        });
      }
      catch(err) {
        // if send fail, apply failed to message
        message.failed = true;

        if (err.response) {
          // The request was made and the server responded with a status code
          // that falls out of the range of 2xx
          const errResponse = err.response.data;
          const errStatus = err.response.status;

          switch (errStatus) {
            case 400:
              // if data is invalid
              this.$store.commit('dialog/showErrDialog', errResponse.errors);
              break;
            case 404:
              // if receiverId is not found, or data passed wrong
              this.$store.commit('dialog/showErrDialog', errResponse.error);
              break;
          }
        }

        console.error(err);
      }

      // let the chatContainer scrollToBottom
      this.$refs.chatContainer.scrollTop = this.$refs.chatContainer.scrollHeight;
    },
    removeMessage(i) {
      // remove message
      this.$store.commit('chats/removeGroupChat', {
        groupId: this.groupId,
        index: i
      });
    },
    async tryAgain(m, i) {

      // remove message
      this.removeMessage(i);

      // and send again
      await this.sendMessage({
        textarea: m.message,
        file: m.fileObject,
        location: m.location
      });
    },
    async deleteMessage(m) {
      try {
        await this.$axios.delete(`group/delete/${m.id}`);
      }
      catch(err) {
        if (error.response) {
          // an unexpected error occur
          this.$store.commit("dialog/showErrDialog", 'Unexpected error occured.');
        } else if (error.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }
    },
    async uploadFiles(files) {

      // loop through files
      for (let i = 0; i < files.length; i++) {
        
        // get the file
        let file = files[i];

        // only allow certain type
        if (!this.$checkFileExtension(file.name)) {
          this.$store.commit('dialog/showErrDialog', `File type must be ${this.$fileExtension.join(", ")}.`);
          continue;
        }

        // file size validation
        if (file.size > this.sizeLimit) {
          this.$store.commit('dialog/showErrDialog', `File exceeded ${this.$formatBytes(this.sizeLimit)}.`);
          continue;
        }

        // send the file
        await this.sendMessage({
          textarea: '',
          file: file,
          location: null,
        });
      }
    },
    async exitGroup() {

      // show loading state
      this.$store.commit('dialog/showLoading', `Exiting Group`);

      try {
        await this.$axios.put(`/group/exit/${this.groupId}`);
      }
      catch(err) {
        if (err.response) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'Bad Request.');
        } else if (err.request) {
          // The request was made but no response was received
          this.$store.commit("dialog/showErrDialog", 'No Internet Connection.');
        }
      }

      // hide loading state
      this.$store.commit('dialog/hideLoading');
    }
  },

  
}
</script>

<style>

</style>