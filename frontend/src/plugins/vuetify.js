import Vue from 'vue';
import Vuetify from 'vuetify/lib/framework';

Vue.use(Vuetify);

export default new Vuetify({
  theme: { 
    dark: localStorage.getItem('light') ? false : true,
    themes: {
      light: {
        panel: '#075E54',  // whatsapp green panel
        list: '#F6F6F6', // Whatsapp grey chat
        receiverPanel: '#FFFFFF',
        senderPanel: '#DCF8C6',
        message: '#424242',
        time: '#8B9D7D',
        messageInfo: '#E1F3FB',
        chatBackground: '#E5DDD5',
        url: '#039BE5',
        name: '#94AE32',
      },
      dark: {
        panel: "#2a2f32", // whatsapp panel dark
        list: '#131C21',  // whatsapp chat dark
        receiverPanel: '#262D31',
        senderPanel: '#056162',
        message: '#D3D4D5',
        time: '#A6A9AB',
        messageInfo: '#262D31',
        chatBackground: '#212121',
        url: '#68BBE4',
        name: '#83AB01',
      },
    },
  },
  breakpoint: {
    scrollBarWidth: 4,
  },
});
