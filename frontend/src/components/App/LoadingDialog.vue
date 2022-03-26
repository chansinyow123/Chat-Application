<template>
  <v-overlay 
    :value="isLoading"
    :absolute="true"
  >
    <div class="py-3 px-4 panel d-flex justify-center align-center rounded-lg">
      {{ loadingText }}
      <v-progress-circular
        size="20"
        indeterminate
        class="ml-3"
      ></v-progress-circular>
    </div>
  </v-overlay>
</template>

<script>
export default {
  name: 'LoadingDialog',
  computed: {
    isLoading() {
      return this.$store.state.dialog.isLoading;
    },
    loadingText() {
      return this.$store.state.dialog.loadingText;
    }
  },
}
</script>

<style>

</style>