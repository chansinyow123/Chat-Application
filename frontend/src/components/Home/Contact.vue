<template>
  <div class="flex-grow-1 overflow-y-auto">

    <v-text-field
      label="Search contact..."
      v-model="search"
      hide-details
      clearable
      prepend-inner-icon="mdi-magnify"
      dense
      outlined
      class="mx-3 my-4"
    ></v-text-field>

    <div 
      v-if="initLoading"
      class="text-center py-4"
    >
      <div>Getting Accounts...</div>
      <v-progress-circular
        indeterminate
        color="primary"
      ></v-progress-circular>
    </div>

    <div
      v-else-if="accountFilter.length == 0"
      class="text-center py-4"
    >
      <span v-if="search">No Search Records Found.</span>
      <span v-else>No Records</span>
    </div>

    <v-list 
      v-else
      two-line 
      class="pa-0 pb-4"
    >
      <v-list-item-group>
        <div
          v-for="a in accountFilter"
          :key="a.id"
        >
          <v-list-item :to="{ name: 'Private', params: { privateId: a.id } }">

            <v-badge
              bordered
              bottom
              :color="a.online ? 'green darken-1' : 'red'"
              offset-x="27"
              offset-y="26"
              dot
            >
              <v-list-item-avatar style="border: 1px solid grey;">
                <v-img
                  :src="a.image ? a.image : require(`@/assets/empty.png`)" 
                ></v-img>
              </v-list-item-avatar>
            </v-badge>

            <v-list-item-content>
              <v-list-item-title v-html="a.name"></v-list-item-title>
              <v-list-item-subtitle v-html="a.email"></v-list-item-subtitle>
            </v-list-item-content>
          </v-list-item>

          <v-divider></v-divider>
        </div>
        
      </v-list-item-group>
    </v-list>
  </div>
</template>

<script>
export default {
  name: 'Contact',
  data: () => ({
    search: '',
  }),
  computed: {
    initLoading () {
      return this.$store.state.accounts.initLoading;
    },
    // search accounts with email and name, order by name
    // and apply text highlighting for search
    accountFilter() {
      const accounts = this.$store.state.accounts.accounts;
      const value = this.search.toLowerCase();
      return accounts
        .filter(a => a.name.toLowerCase().includes(value) || a.email.toLowerCase().includes(value))
        .map(a => { 
          return { 
            id: a.id, 
            image: a.image, 
            name: this.$highlightSearch(this.search, a.name),
            email: this.$highlightSearch(this.search, a.email),
            online: a.online,
          } 
        })
        .sort((x, y) => {
          let a = x.name.toUpperCase();
          let b = y.name.toUpperCase();
          return a == b ? 0 : a > b ? 1 : -1;
        });
    }
  },
}
</script>

<style>

</style>