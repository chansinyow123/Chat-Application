// for chats
export default {
  namespaced: true,
  state: () => ({ 
    initLoading: true,
    chats: [
      {
        notificationCount: 0,
        groupId: 1,
        userId: '',
        messages: [
          {
            id: null,
            failed: null,
            fileObject: null,
            senderId: null,
            info: null,
            message: null,
            location: null,
            file: null,
            contentType: null,
            fileName: null,
            fileSize: null,
            onCreate: new Date().toISOString(),
            onDelete: null,
            onSeen: false,
          }
        ]
      }
    ]
  }),
  mutations: {
    // Signalr -------------------------------------------------------------------------
    setChats(state, chats) {
      state.chats = chats;
      state.initLoading = false;
    },
    resetChats(state) {
      state.chats = [];
      state.initLoading = true;
    },
    // Private Chat --------------------------------------------------------------------
    limitPrivateChat(state, receiverId) {
      // get chat
      let chat = state.chats.find(c => c.userId == receiverId);

      // if no chat, then return
      if (!chat) return;

      // limit 30 chat to store on cache
      let messages = chat.messages;
      let limit = 30
      if (messages.length > limit) {
        chat.messages = messages.slice(messages.length - limit, messages.length);
      }
    },
    prependPrivateChat(state, { receiverId, messages }) {
      // load previous chat
      const chat = state.chats.find(c => c.userId == receiverId);
      chat.messages.unshift(...messages);
    },
    appendPrivateMessage(state, { receiverId, message }) {
      // get chat by index
      let i = state.chats.findIndex(c => c.userId == receiverId);

      // if no index found, prepend private chat and add the message
      if (i == -1) {
        state.chats.unshift({
          notificationCount: 0,
          group: null,
          userId: receiverId,
          messages: [message]
        });

        return;
      }

      // else push the message
      state.chats[i].messages.push(message);

      // if the index is not 0, then move the message to 0 index
      if (i != 0) {
        let chat = state.chats[i];
        state.chats.splice(i, 1);
        state.chats.unshift(chat);
      }

    },
    incrementPrivateNotification(state, receiverId) {
      let chat = state.chats.find(c => c.userId == receiverId);
      chat.notificationCount++;
    },
    removePrivateNotification(state, receiverId) {
      let chat = state.chats.find(c => c.userId == receiverId);
      chat.notificationCount = 0;
    },
    privateSeen(state, receiverId) {
      // assign all onSeen to true
      let chat = state.chats.find(c => c.userId == receiverId);
      chat.messages = chat.messages.map(m => { 
        return { ...m, onSeen: true }
      });
    },
    removePrivateChat(state, { receiverId, index }) {
      // remove chat
      state.chats.find(c => c.userId == receiverId).messages.splice(index, 1);
    },
    removePendingPrivateChat(state, { receiverId, message }) {
      // remove pending private chat by comparing message object references
      let chat = state.chats.find(c => c.userId == receiverId);
      let index = chat.messages.findIndex(m => m === message);

      chat.messages.splice(index, 1);
    },
    updatePrivateMessage(state, { receiverId, message }) {
      // update private chat by replacing message.
      let chat = state.chats.find(c => c.userId == receiverId);
      let index = chat.messages.findIndex(m => m.id == message.id);

      // if cannot find the message, then do nothing
      if (index == -1) return;

      // update message
      chat.messages.splice(index, 1, message);
    },
    // Group Chat ------------------------------------------------------------------------
    limitGroupChat(state, groupId) {
      // get chat
      let chat = state.chats.find(c => (c.groupId == groupId));

      // if no chat, then return
      if (!chat) return;

      // limit 30 chat to store on cache
      let messages = chat.messages;
      let limit = 30
      if (messages.length > limit) {
        chat.messages = messages.slice(messages.length - limit, messages.length);
      }
    },
    removeGroupNotification(state, groupId) {
      let chat = state.chats.find(c => c.groupId == groupId);
      chat.notificationCount = 0;
    },
    appendGroupMessage(state, { groupId, message }) {
      // get chat by index
      let i = state.chats.findIndex(c => c.groupId == groupId);

      // if no index found, prepend group chat and add the message
      if (i == -1) {
        state.chats.unshift({
          notificationCount: 0,
          groupId: groupId,
          userId: null,
          messages: [message]
        });

        return;
      }

      // else push the message
      state.chats[i].messages.push(message);

      // if the index is not 0, then move the message to 0 index
      if (i != 0) {
        let chat = state.chats[i];
        state.chats.splice(i, 1);
        state.chats.unshift(chat);
      }
    },
    clearGroupMessage(state, groupId) {
      // get chat by index
      let i = state.chats.findIndex(c => c.groupId == groupId);

      // if no index found, then do nothing
      if (i == -1) return;
      
      // else clear the message
      state.chats[i].messages = [];
    },
    removePendingGroupChat(state, { groupId, message }) {
      // remove pending group chat by comparing message object references
      let chat = state.chats.find(c => c.groupId == groupId);
      let index = chat.messages.findIndex(m => m === message);

      chat.messages.splice(index, 1);
    },
    removeGroupChat(state, { groupId, index }) {
      // remove chat
      state.chats.find(c => c.groupId == groupId).messages.splice(index, 1);
    },
    prependGroupChat(state, { groupId, messages }) {
      // load previous chat
      const chat = state.chats.find(c => c.groupId == groupId)
      chat.messages.unshift(...messages);
    },
    groupSeen(state, groupId) {
      // assign all onSeen to true
      let chat = state.chats.find(c => c.groupId == groupId);
      chat.messages = chat.messages.map(m => { 
        return { ...m, onSeen: true }
      });
    },
    incrementGroupNotification(state, groupId) {
      let chat = state.chats.find(c => c.groupId == groupId);
      chat.notificationCount++;
    },
    updateGroupMessage(state, { groupId, message }) {
      // update group chat by replacing message.
      let chat = state.chats.find(c => c.groupId == groupId);
      let index = chat.messages.findIndex(m => m.id == message.id);

      // if cannot find the message, then do nothing
      if (index == -1) return;

      // update message
      chat.messages.splice(index, 1, message);
    },
  },
}