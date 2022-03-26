
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'

export default {
  data: () => ({
    connection: null
  }),
  async created() {
    // Configure signalr connection -------------------------------------------------------
    let connection = new HubConnectionBuilder()
      .withUrl(`${this.$axios.defaults.baseURL}/chat-hub`)
      // .withAutomaticReconnect({
      //   nextRetryDelayInMilliseconds: retryContext => {
      //     // Retry every 5 seconds
      //     console.log("Retrying", retryContext.previousRetryCount, retryContext.elapsedMilliseconds);
      //     return 5000;
      //   }
      // })
      //.configureLogging(LogLevel.Information)
      .build();

    this.connection = connection;
 
    // start the connection ---------------------------------------------------------------
    const start = async (callback) => {
      try {
        
        // start the connection
        await this.connection.start();
        console.log('SignalR Connected.');

        // call the callback function
        await callback();

      } catch (err) {
        // if statusCode is 401 (Unauthorized)
        // show error and logout user to login page
        if (err.statusCode && err.statusCode == 401) {
          this.$store.commit('dialog/showErrDialog', 'Unauthorized Access');
          this.$logout();
          this.$router.push({ name: 'Login' });
          return;
        }

        // else start again
        console.log('retrying signalr');
        await this.$timeout(5000);
        await start(callback);
      }
    };

    // prepare a callback to call after the connection is start
    let callback = async () => {

      // OnReconnecting Callback ------------------------------------------------------------
      this.connection.onreconnecting(error => {
        console.log('SignalR Reconnecting.');
      });

      // OnReconnected Callback -------------------------------------------------------------
      this.connection.onreconnected(connectionId => {
        console.log('Signalr Reconnected.');
      });   

      // Close Connection Callback ----------------------------------------------------------
      this.connection.onclose(err => {
        // If the signalr close due to error, required user to refresh the page
        if (err) {
          this.$store.commit('dialog/showRefreshDialog', 'Connection close, required to refresh.');
        }
        console.log('ChatHub SignalR Closed.', err);
      });

      // First load all the contacts -------------------------------------------------------
      try {
        let accounts = await this.connection.invoke('GetContact');
        this.$store.commit('accounts/setAccounts', accounts);
      }
      catch(err) {
        this.$store.commit('dialog/showErrDialog', 'Unexpected Error Occured in getting contact.');
        return;
      }

      // Signalr that update accounts ------------------------------------------------------
      this.connection.on('AddAccount', (account) => {
        this.$store.commit('accounts/addAccounts', account);
      });

      this.connection.on('UpdateAccount', (account) => {
        this.$store.commit('accounts/updateAccount', account);
      });

      // Load Group Info -------------------------------------------------------------------
      try {
        let groups = await this.connection.invoke('GetGroupInfo');
        this.$store.commit('groups/setGroups', groups);
      }
      catch(err) {
        this.$store.commit('dialog/showErrDialog', 'Unexpected Error Occured in getting groups info.');
        return;
      }

      // Load Contact and Chat -------------------------------------------------------------
      try {
        let chats = await this.connection.invoke('GetRecentChat');
        this.$store.commit('chats/setChats', chats);
      }
      catch(err) {
        this.$store.commit('dialog/showErrDialog', 'Unexpected Error Occured in getting chats.');
        return;
      }

      // Signalr that handle private message -----------------------------------------------
      this.connection.on('AppendPrivateMessage', (userId, message) => {
        this.$store.commit('chats/appendPrivateMessage', {
          receiverId: userId,
          message: message
        });
      });

      this.connection.on('IncrementPrivateNotification', (receiverId) => {
        this.$store.commit('chats/incrementPrivateNotification', receiverId);
      });

      this.connection.on('PrivateSeen', (receiverId) => {
        this.$store.commit('chats/privateSeen', receiverId);
      });

      this.connection.on('UpdatePrivateMessage', (receiverId, message) => {
        this.$store.commit('chats/updatePrivateMessage', {
          receiverId: receiverId,
          message: message
        });
      });

      // Signalr that handle group message -----------------------------------------------
      this.connection.on('AppendGroupMessage', (groupId, message) => {
        this.$store.commit('chats/appendGroupMessage', {
          groupId: groupId,
          message: message
        });
      });

      this.connection.on('CreateGroupChat', (groups) => {
        this.$store.commit('groups/addGroups', groups);
      });

      this.connection.on('UpdateGroup', (group) => {
        this.$store.commit('groups/updateGroup', group);
      });

      this.connection.on('GroupSeen', (groupId) => {
        this.$store.commit('chats/groupSeen', groupId);
      });

      this.connection.on('IncrementGroupNotification', (groupId) => {
        this.$store.commit('chats/incrementGroupNotification', groupId);
      });

      this.connection.on('UpdateGroupMessage', (groupId, message) => {
        this.$store.commit('chats/updateGroupMessage', {
          groupId: groupId,
          message: message
        });
      });

      this.connection.on('ClearGroupMessage', (groupId) => {
        this.$store.commit('chats/clearGroupMessage', groupId);
      });
    }

    // start the connection and pass the call back
    await start(callback);
  },
  beforeDestroy() {
    // stop signalr connection
    if (this.connection) {
      this.connection.stop();
    }
    // reset account data
    this.$store.commit('accounts/resetAccount');
    // reset chats data
    this.$store.commit('chats/resetChats');
    // reset groups data
    this.$store.commit('groups/resetGroups');
  }
}