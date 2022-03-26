// Show Success and Error Dialog
export default {
  namespaced: true,
  state: () => ({ 
    errDialog: false,           // to show error dialog
    errDialogText: '',          // display error text in error dialog
    successDialog: false,       // to show success dialog
    successDialogText: '',      // display success text in success dialog
    isLoading: false,           // to show loading dialog
    loadingText: '',            // display loading text in loading dialog
    refreshDialog: false,       // to show refresh dialog with refresh button
    refreshDialogText: '',      // display refresh text in refresh dialog
    showOverlay: false,         // to show background overlay
  }),
  mutations: {
    // Toggle overlay
    toggleOverlay(state, boolean) {
      // console.log('cool')
      state.showOverlay = boolean;
    },
    // how refresh dialog
    showRefreshDialog(state, text) {
      state.refreshDialogText = text;
      state.refreshDialog = true;
    },
    // Show Loading
    showLoading(state, text) {
      state.loadingText = text;
      state.isLoading = true;
    },
    // Hide Loading
    hideLoading(state) {
      state.isLoading = false;
      state.loadingText = '';
    },
    // Show Error Dialog
    showErrDialog(state, text) {
      state.errDialogText = text;
      state.errDialog = true;
    },
    // Close Error Dialog
    closeErrDialog(state) {
      state.errDialog = false;
      state.errDialogText = '';
    },
    // Show Success Dialog
    showSuccessDialog(state, text) {
      state.successDialogText = text;
      state.successDialog = true;
    },
    // Close Success Dialog
    closeSuccessDialog(state) {
      state.successDialog = false;
      state.successDialogText = '';
    },
  },
}