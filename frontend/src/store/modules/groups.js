// for handle group
export default {
  namespaced: true,
  state: () => ({ 
    initLoading: true,
    groups: [
      // Here is just a sample field for group
      { id: 1 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 2 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 3 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 4 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 5 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 6 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 7 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
      { id: 8 , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Group Name' , isExit: false, },
    ],
  }),
  mutations: {
    // Set Groups
    setGroups(state, groups) {
      state.groups = groups;
      state.initLoading = false;
    },
    // Add Groups
    addGroups(state, groups) {
      state.groups.push(groups);
    },
    // Update Group
    updateGroup(state, group) {
      const index = state.groups.findIndex(a => a.id == group.id);
      state.groups.splice(index, 1, group);
    },
    // Delete Group
    deleteGroup(state, group) {
      const index = state.groups.findIndex(a => a.id == group.id);
      state.groups.splice(index, 1);
    },
    // Reset Group
    resetGroups(state) {
      state.groups = [];
      state.initLoading = true;
    }
  },
  getters: {
    getImage: (state) => (id) => {
      let image = state.groups.find(x => x.id == id)?.image;

      if (image) return image
      else return require(`@/assets/empty-group.png`);
    },
    getName: (state) => (id) => {
      return state.groups.find(x => x.id == id)?.name;
    },
    getIsExit: (state) => (id) => {
      return state.groups.find(x => x.id == id)?.isExit;
    },
  }
}