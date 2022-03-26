<template>
  <div class="flex-grow-1 overflow-y-auto">
    <div 
      v-if="initLoading"
      class="text-center py-4"
    >
      <div>Getting Recent Chats...</div>
      <v-progress-circular
        indeterminate
        color="primary"
      ></v-progress-circular>
    </div>

    <div
      v-else-if="chats.length == 0"
      class="text-center py-4"
    >
      <span>No Recent Chats</span>
    </div>

    <v-list 
      v-else
      two-line 
      class="pa-0 pb-4"
    >
      <v-list-item-group>

        <div
          v-for="c in chats"
        >

          <v-list-item
            :to="(c.userId) 
              ? { name: 'Private', params: { privateId: c.userId }} 
              : { name: 'Group', params: { groupId: c.groupId}} "
          >
            
            <v-badge
              bordered
              bottom
              :value="!c.groupId"
              :color="getOnlineStatus(c)"
              offset-x="27"
              offset-y="26"
              dot
            >
              <v-list-item-avatar style="border: 1px solid grey;">
                <v-img
                  :src="getImage(c)" 
                ></v-img>
              </v-list-item-avatar>
            </v-badge>            
            <v-list-item-content>
              <v-list-item-title> {{ getName(c) }}</v-list-item-title>
              <v-list-item-subtitle> {{ getMessage(c.messages.at(-1)) }} </v-list-item-subtitle>
            </v-list-item-content>

            <v-list-item-action>
              <v-list-item-action-text :class=" { 'green--text': c.notificationCount } ">
                {{ getDate(c.messages.at(-1).onCreate) }}
              </v-list-item-action-text>

              <div v-show="c.notificationCount">
                <v-avatar
                  color="green"
                  size="20"
                >
                  <span class="text-caption white--text"> {{ c.notificationCount }} </span>
                </v-avatar>
              </div>
            </v-list-item-action>

          </v-list-item>

          <v-divider></v-divider>
        </div>
        
      </v-list-item-group>
    </v-list>
  </div>
</template>

<script>

export default {
  name: 'Recent',
  data: () => ({
    
  }),
  computed: {
    initLoading () {
      return this.$store.state.chats.initLoading;
    },
    chats () {
      return this.$store.state.chats.chats;
    },
  },
  methods: {
    getOnlineStatus(c) {

      // if there is a group, do nothing
      if (c.groupId) return;

      // if is account, then check if it is online, then show online status
      let online = this.$store.state.accounts.accounts.find(a => a.id == c.userId).online;
      
      // green color if online, red color if offline
      if (online) return "green darken-1";
      else return "red";
    },
    getName(c) {
      // if is group, display group name, else display receiver name
      if (c.groupId) return this.$store.state.groups.groups.find(a => a.id == c.groupId).name;
      else return this.$store.state.accounts.accounts.find(a => a.id == c.userId).name;
    },
    getDate(date) {
      const createDate = new Date(date)
      const today = new Date()

      // if is today date, show time only, else show date
      if (createDate.toLocaleDateString() == today.toLocaleDateString()) {
        return this.$getTime(createDate);
      }
      else {
        return createDate.toLocaleDateString();
      }
    },
    getMessage(message) {
      
      // if has info, then return info
      if (message.info) {
        return message.info;
      } 

      // prepare variable
      const uid = this.$getCookie('uid');
      let text = ''

      // if the message is send by this user, append 'You: '
      if (uid == message.senderId) {
        text += 'You: '
      } 

      // if OnDelete is not null, show deleted message
      if (message.onDelete) {
        text += "Message Deleted"; 
        return text;
      }

      // if there is text message, then return text
      if (message.message) {
        text += message.message;
        return text;
      }

      // if have file
      if (message.fileName) {
        if (this.$checkImageExtension(message.fileName)) {
          text += "Sent an image";
          return text;
        }
        if (this.$checkAudioExtension(message.fileName)) {
          text += "Sent an audio";
          return text;
        }
        if (this.$checkVideoExtension(message.fileName)) {
          text += "Sent a video";
          return text;
        }
        
        text += "Sent a document";
        return text;
      }
      
      if (message.location) {
        // if have location
        text += "Sent a location";
        return text;
      }
      
      // if none of the above, show error message
      return 'Error Occured';
    },
    getImage(c) {
      // image variable
      let image = "";

      // if is group, store group image, else store receiver image
      // if dont have image, display default image
      if (c.groupId) image = this.$store.state.groups.groups.find(a => a.id == c.groupId).image  ?? require(`@/assets/empty-group.png`); 
      else image = this.$store.state.accounts.accounts.find(a => a.id == c.userId).image ?? require(`@/assets/empty.png`);

      // return image
      return image;
    }
  }
}
</script>

<style>

</style>