// for admin to manage accounts
export default {
  namespaced: true,
  state: () => ({ 
    initLoading: true,
    accounts: [
      // Here is just a sample field for account
      { id: '1' , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: '<h1>cool</h1>' , email: 'chansy-wm18@student.tarc.edu.my', online: 0, needNotification: false },
      { id: '2' , image: 'https://cdn.vuetifyjs.com/images/lists/2.jpg', name: 'Teh Wan Theng' , email: 'tehwt-wm18@student.tarc.edu.my' , online: 0, needNotification: false },
      { id: '3' , image: 'https://cdn.vuetifyjs.com/images/lists/3.jpg', name: 'Ong Yi Pin'    , email: 'ongyp-wm18@student.tarc.edu.my' , online: 0, needNotification: false },
      { id: '4' , image: 'https://cdn.vuetifyjs.com/images/lists/4.jpg', name: 'Liaw Chun Voon', email: 'liawcv@staff.tarc.edu.my'       , online: 0, needNotification: false },
      { id: '5' , image: 'https://cdn.vuetifyjs.com/images/lists/5.jpg', name: 'Wong Ket Yee'  , email: 'wongky-wm18@student.tarc.edu.my', online: 0, needNotification: false },
      { id: '6' , image: 'https://cdn.vuetifyjs.com/images/lists/1.jpg', name: 'Chan Sin Yow'  , email: 'chansy-wm18@student.tarc.edu.my', online: 0, needNotification: false },
      { id: '7' , image: 'https://cdn.vuetifyjs.com/images/lists/2.jpg', name: 'Teh Wan Theng' , email: 'tehwt-wm18@student.tarc.edu.my' , online: 0, needNotification: false },
      { id: '8' , image: 'https://cdn.vuetifyjs.com/images/lists/3.jpg', name: 'Ong Yi Pin'    , email: 'ongyp-wm18@student.tarc.edu.my' , online: 0, needNotification: false },
    ],
  }),
  mutations: {
    // Set Accounts
    setAccounts(state, accounts) {
      state.accounts = accounts;
      state.initLoading = false;
    },
    // Add Accounts
    addAccounts(state, accounts) {
      state.accounts.push(accounts);
    },
    // Update Account
    updateAccount(state, account) {
      const index = state.accounts.findIndex(a => a.id == account.id);
      state.accounts.splice(index, 1, account);
    },
    // Delete Account
    deleteAccount(state, account) {
      const index = state.accounts.findIndex(a => a.id == account.id);
      state.accounts.splice(index, 1);
    },
    // Reset Account
    resetAccount(state) {
      state.accounts = [];
      state.initLoading = true;
    }
  },
  getters: {
    getImage: (state) => (id) => {
      let image = state.accounts.find(x => x.id == id)?.image;

      if (image) return image
      else return require(`@/assets/empty.png`);
    },
    getName: (state) => (id) => {
      return state.accounts.find(x => x.id == id)?.name;
    },
    getEmail: (state) => (id) => {
      return state.accounts.find(x => x.id == id)?.email;
    },
  }
}