<template>
  <v-card
    flat
    tile
    height="100%"
    color="chat"
    class="d-flex flex-column justify-center"
  >
    <v-card-title class="justify-center">404 Page Not Found</v-card-title>
    <v-card-text class="text-center">
      <router-link :to="{ name: 'Home' }">Back to home page</router-link>
    </v-card-text>
  </v-card>
</template>

<script>
export default {
  name: 'NotFound',
}
</script>

<style>

</style>