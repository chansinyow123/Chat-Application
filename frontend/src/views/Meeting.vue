<template>
  <div>
    <div 
      v-if="initLoading"
      class="fill-height d-flex flex-column justify-center align-center"
    >
      <v-progress-circular
        indeterminate
        color="primary"
      ></v-progress-circular>  
      <div class="mt-3">Loading Meet...</div>
    </div>

    <div 
      v-else-if="errorOccured"
      class="fill-height d-flex justify-center align-center"
    >
      <div>
        <span>An unexpected error occured.</span>
        <span class="text-decoration-underline light-blue--text pa-1" style="cursor: pointer;" @click="$_back({ name: 'Home' })">back</span>
      </div>
    </div>

    <div 
      v-else-if="notFound"
      class="fill-height d-flex justify-center align-center"
    >
      <div>
        <span>This meeting is invalid.</span>
        <span class="text-decoration-underline light-blue--text pa-1" style="cursor: pointer;" @click="$_back({ name: 'Home' })">back</span>
      </div>
    </div>

    <div 
      v-else-if="full"
      class="fill-height d-flex justify-center align-center"
    >
      <div>
        <span>This meeting is full.</span>
        <span class="text-decoration-underline light-blue--text pa-1" style="cursor: pointer;" @click="$_back({ name: 'Home' })">back</span>
      </div>
    </div>

    <div 
      v-else-if="notPermitted"
      class="fill-height d-flex justify-center align-center"
    >
      <div>
        <span>You have to allow microphone and camera.</span>
        <span class="text-decoration-underline light-blue--text pa-1" style="cursor: pointer;" @click="$_back({ name: 'Home' })">back</span>
      </div>
    </div>

    <div 
      v-else
      class="fill-height d-flex flex-column"
    >
      <v-app-bar
        color="panel"
        flat
        tile
        dark
        class="flex-grow-0 flex-shrink-0"
      >
        <v-toolbar-title class="text-subtitle-1">{{ ownAccount.name }}</v-toolbar-title>

        <v-spacer></v-spacer>

        <v-menu
          @input="$store.commit('dialog/toggleOverlay', $event)"
          left 
          origin="top right"
          transition="scale-transition"
          min-width="150px"
        >
          <template v-slot:activator="{ on, attrs }">
            <v-btn
              icon
              v-bind="attrs"
              v-on="on"
            >
              <v-icon>mdi-dots-vertical</v-icon>
            </v-btn>
          </template>
          
          <v-list class="pa-0">
            <v-list-item @click="requestPictureInPicture()">
              <v-list-item-title>Picture-in-Picture</v-list-item-title>
            </v-list-item>
            <v-list-item @click="changeTheme()">
              <v-list-item-content>
                <v-list-item-title>Dark Mode</v-list-item-title>
              </v-list-item-content>

              <v-list-item-action>
                <v-switch
                  class="pl-3"
                  v-model="$vuetify.theme.dark"
                  disabled
                ></v-switch>
              </v-list-item-action>
            </v-list-item>
            <v-list-item @click="$refresh()">
              <v-list-item-title>Refresh</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </v-app-bar>
      <div 
        class="flex-grow-1 overflow-y-auto"
        style="position: relative;"
      >
        <div 
          class="fill-height d-flex justify-center align-center"
          style="position: relative; background-color: #202124; border: 1px solid grey;"
        >
          <div style="position: absolute; bottom: 10px; left: 10px;">
            {{ otherAccount.name }}
          </div>

          <div 
            class="text--h6"
            v-show="!otherAccount.id"
          >
            No one here yet.
          </div>

          <video 
            style="width: 100%; height: 100%;"
            ref="otherCamera"
            v-show="otherAccount.id && otherAccount.camera"
          >
          </video>

          <v-avatar 
            v-show="otherAccount.id && !otherAccount.camera" 
            size="200"
          >
            <v-img 
              :src="otherAccount.image ? otherAccount.image : require(`@/assets/empty.png`)" 
            ></v-img>
          </v-avatar>

          <div
            style="position: absolute; top: 10px; right: 10px;"
            v-show="otherAccount.id"
          >
            <v-icon>
              {{ otherAccount.microphone ? 'mdi-microphone' : 'mdi-microphone-off' }}
            </v-icon>
          </div>
        </div>

        <div 
          style="position: absolute; bottom: 5px; right: 5px; background-color: #202124; border: 1px solid grey;"
          :style="{ width: $vuetify.breakpoint.smAndDown ? '100px' : '200px', height: $vuetify.breakpoint.smAndDown ? '100px' : '200px' }"
          class="d-flex justify-center align-center"
        >
          <video
            style="height: 100%; width: 100%;"
            ref="ownCamera"
            muted
            v-show="camera"
          >
          </video>

          <v-avatar 
            v-show="!camera" 
            :size="$vuetify.breakpoint.smAndDown ? '50' : '150'"
          >
            <v-img 
              :src="ownAccount.image ? ownAccount.image : require(`@/assets/empty.png`)" 
            ></v-img>
          </v-avatar>

          <div style="position: absolute; top: 10px; right: 10px;">
            <v-icon>
              {{ microphone ? 'mdi-microphone' : 'mdi-microphone-off' }}
            </v-icon>
          </div>
        </div>
      </div>
      <div class="flex-shrink-1 text-center panel py-2">

        <v-btn
          class="mx-2"
          fab
          dark
          :color="microphone ? '' : 'error'"
          :outlined="microphone"
          depressed
          @click="toggleMicrophone()"
        >
          <v-icon dark>
            {{ microphone ? 'mdi-microphone' : 'mdi-microphone-off' }}
          </v-icon>
        </v-btn>

        <v-btn
          class="mx-2"
          fab
          dark
          :color="camera ? '' : 'error'"
          :outlined="camera"
          depressed
          @click="toggleCamera()"
        >
          <v-icon dark>
            {{ camera ? 'mdi-video' : 'mdi-video-off' }}
          </v-icon>
        </v-btn>

        <v-btn
          class="mx-2"
          fab
          dark
          color="red"
          depressed
          @click="$_back({ name: 'Home' })"
        >
          <v-icon dark>
            mdi-phone-hangup
          </v-icon>
        </v-btn>
      </div>
    </div>
  </div>
</template>

<script>
import Peer from 'peerjs';
import { HubConnectionBuilder } from '@microsoft/signalr'

export default {
  name: 'Meeting',
  data: () => ({
    connection: null,                  // videoHub SignalR connection
    // error state
    initLoading: true,                 // initLoading to check is the meetingId valid or not
    errorOccured: false,               // to check if there is any error
    notFound: false,                   // check if the meeting exist or not
    full: false,                       // check if the meeting is full or not
    notPermitted: false,               // user have to allow microphone and camera
    // toggle
    camera: true,                      // toggle camera
    microphone: true,                  // toggle microphone
    // peerjs
    peer: null,                        // peer
    conn: null,                        // peer connection
    call: null,                        // for media calling
    mediaStream: null,        
    // my own data        
    ownAccount: {
      id: null,
      name: '',
      image: '',
      camera: true,
      microphone: true,
    },
    // other data
    otherAccount: {
      id: null,
      name: '',
      image: '',
      camera: true,
      microphone: true,
    },
  }),
  props: {
    meetId: {
      type: String,
      required: true,
    }
  },
  async mounted() {

    // Configure signalr connection -------------------------------------------------------
    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.$axios.defaults.baseURL}/video-hub`)
      // .withAutomaticReconnect({
      //   nextRetryDelayInMilliseconds: retryContext => {
      //     // Retry every 5 seconds
      //     console.log("Retrying", retryContext.previousRetryCount, retryContext.elapsedMilliseconds);
      //     return 5000;
      //   }
      // })
      //.configureLogging(LogLevel.Information)
      .build();
 
    // start the connection ---------------------------------------------------------------
    const start = async () => {
      try {
        await this.connection.start();
        console.log('SignalR Connected.');
        return true;
      } catch (err) {
        // if statusCode is 401 (Unauthorized)
        // show error and logout user to login page
        if (err.statusCode && err.statusCode == 401) {
          this.$store.commit('dialog/showErrDialog', 'Unauthorized Access');
          this.$logout();
          this.$router.push({ name: 'Login' });
          return false;
        }
        setTimeout(start, 5000);
      }
    };

    // if error occur for starting signalr, do nothing
    const isStarted = await start();
    if (!isStarted) return;

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
      console.log('VideoHub SignalR Closed.', err);
    });

    // check if the meeting is valid or not -----------------------------------------------
    let isValidMeet = await this.connection.invoke('CheckMeeting', this.meetId);

    // if it is return null, means the meeting is not exist
    // set notfound to true
    if (isValidMeet == null) {
      this.notFound = true;
      this.initLoading = false;
      return;
    }

    // if it is return false, means the meeting is full
    // set full to true
    if (isValidMeet == false) {
      this.full = true;
      this.initLoading = false;
      return;
    }

    // get this user account info first ---------------------------------------------------
    try { 
      let response = await this.$axios.get('/account/profile'); 
      this.ownAccount = response.data;
      this.ownAccount.camera = this.camera;
      this.ownAccount.microphone = this.microphone;
    }
    catch(err) {
      this.errorOccured = true;
      this.initLoading = false;
      console.log(err);
      return;
    }

    // get user media ----------------------------------------------------------------------
    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ 
        video: this.camera, audio: this.microphone
      });
    }
    catch(err) {
      this.notPermitted = true;
      this.initLoading = false;
      console.log(err.name);
      console.log(err);
      // if it is a not readable error, display error message
      if (err.name == 'NotReadableError') {
        this.$store.commit('dialog/showErrDialog', `Your camera or microphone is not readable, please change them and refresh the page.`);
      }
      return;
    }

    // start a peer ------------------------------------------------------------------------
    this.peer = new Peer(undefined, { 
      debug: 2,
    });

    // the open event will trigger after the peer is started
    // it will provide peerId for this user
    this.peer.on('open', async (peerId) => {
      console.log('My peer ID is: ' + peerId);

      // send the peerId to other user in this group
      // if got error, assign errorOccured to true, then log out the error -----------------
      try { await this.connection.send('SendPeerId', peerId); }
      catch(err) { this.errorOccured = true; console.err(err); }

      // initLoading to false
      this.initLoading = false;

      // play camera in video --------------------------------------------------------------
      this.$nextTick(() => {
        this.$refs.ownCamera.srcObject = this.mediaStream;
        this.$refs.ownCamera.play();
      });
    });

    // Receive peerId from new comer from signalr ------------------------------------------
    this.connection.on('ReceivePeerId', async (peerId) => {
      // connect peer to the new comer
      console.log('ReceivePeerId', peerId);

      // connect peer data connection -----------------------------------------------------
      let conn = this.peer.connect(peerId, {
        reliable: true
      });

      // handle peer data connection ------------------------------------------------------
      this.handleConnection(conn);

      // call peer media connection -------------------------------------------------------
      // and handle call media connection
      let call = this.peer.call(peerId, this.mediaStream);
      this.handleCall(call);
    });

    // when other peer connected ----------------------------------------------------------
    this.peer.on('connection', this.handleConnection);

    // handle peer call event -------------------------------------------------------------
    this.peer.on('call', (call) => {
      call.answer(this.mediaStream);
      this.handleCall(call);
    })

    // handle peer error event ------------------------------------------------------------
    this.peer.on('error', (err) => {
      console.log('Peer Error', err);
      this.errorOccured = true;
    });

    // Setup Picture-in-Picture variable -------------------------------------------------
    navigator.mediaSession.setMicrophoneActive(this.microphone);
    navigator.mediaSession.setCameraActive(this.camera);

    // Picture-in-Picture Toggle Microphone ----------------------------------------------
    try { navigator.mediaSession.setActionHandler('togglemicrophone', this.toggleMicrophone); } 
    catch(err) { console.log('Warning! The "togglemicrophone" media session action is not supported.'); }

    // Picture-in-Picture Toggle Camera -------------------------------------------------
    try { navigator.mediaSession.setActionHandler('togglecamera', this.toggleCamera); } 
    catch(err) { console.log('Warning! The "togglecamera" media session action is not supported.'); }

    // Picture-in-Picture Hangup --------------------------------------------------------
    try {
      navigator.mediaSession.setActionHandler("hangup", () => {
        // go to chat page and exit picture-in-picture mode
        this.$_back({ name: 'Home' })
        document.exitPictureInPicture();
      });
    } catch (err) {
      console.log('Warning! The "hangup" media session action is not supported.');
    }
  },
  async beforeDestroy() {
    // stop signalr connection ------------------------------------------------------------
    if (this.connection) {
      this.connection.stop();
    }
    // destroy the peer ------------------------------------------------------------------
    if (this.peer) {
      this.peer.destroy();
    }

    // destroy all the stream -----------------------------------------------------------
    if (this.$refs.ownCamera && this.$refs.ownCamera.srcObject) {
      this.$refs.ownCamera.srcObject.getVideoTracks().forEach(track => track.stop());
      this.$refs.ownCamera.srcObject.getAudioTracks().forEach(track => track.stop());
    }

    // exit picture in picture element, if any ------------------------------------------
    if (document.pictureInPictureElement) {
      try { await document.exitPictureInPicture(); }
      catch(err) { console.log(err); }
    }
  },
  methods: {
    changeTheme() {
      this.$vuetify.theme.dark = !this.$vuetify.theme.dark;
      this.$setTheme(this.$vuetify.theme.dark);
    },
    async requestPictureInPicture() {
      try { 
        await this.$refs.ownCamera.requestPictureInPicture(); 
      }
      catch(err) {
        this.$store.commit("dialog/showErrDialog", 'Picture-in-Picture is not supported in your browser.');
        console.log(err); 
      }
    },
    handleConnection(conn) {
      console.log('connection', conn);

      // handle incoming connection  ---------------------------------------------------
      conn.on('open', () => {
        console.log('Conn Open');

        // Receive messages ------------------------------------------------------------
        conn.on('data', this.handleDataConnection);

        // When connection close -------------------------------------------------------
        conn.on('close', this.closeConnection);

        // When connection error occured -----------------------------------------------
        conn.on('error', (err) => {
          console.log('Conn Error', err);
          this.errorOccured = true;
          this.conn = null;
        });

        // Send this user account to other people --------------------------------------
        // this is a setup type
        conn.send({
          type: 'edit',
          data: this.ownAccount
        });

        // add to connection if it is empty -------------------------------------------
        if (!this.conn) {
          this.conn = conn;
        }
      });
    },
    handleDataConnection(data) {
      // Receive and handle connection data -------------------------------------------
      console.log('Received', data);

      switch (data.type) {
        case 'edit':
          // edit otherAccount
          this.otherAccount = data.data;
          break;
        default: 
          console.log('none of the above', data);
      }
    },
    handleCall(call) {
      // handle stream/close/error call event ---------------------------------------
      call.on('stream', this.handleMediaConnection);
      call.on('close', () => console.log('call close'));
      call.on('error', (err) => {
        console.log('Call Error', err)
        this.errorOccured = true;
      });

      // assign this call ------------------------------------------------------------
      if (!this.call) {
        this.call = call;
      }
    },
    handleMediaConnection(stream) {
      // show otherAccount camera ----------------------------------------------------
      this.$refs.otherCamera.srcObject = stream;
      this.$refs.otherCamera.play();
    },
    closeConnection() {
      console.log('Conn Close');
      // reset all variable ----------------------------------------------------------
      this.conn = null;
      this.otherAccount.id = null;
      this.otherAccount.name = '';
      this.otherAccount.image = '';
      this.otherAccount.camera = false;
      this.otherAccount.microphone = false;
      
      // close the call, if available ------------------------------------------------
      if (this.call) {
        this.call.close();
        this.call = null;
      }
    },
    toggleMicrophone() {
      // toggle microphone
      this.microphone = !this.microphone;
      this.ownAccount.microphone = this.microphone;

      // mute the audio and send data to update mic status
      this.mediaStream.getAudioTracks()[0].enabled = this.microphone;
      this.sendEditAccount();

      // set the microphone in Picture-in-Picture
      navigator.mediaSession.setMicrophoneActive(this.microphone);
    },
    toggleCamera() {
      // toggle camera
      this.camera = !this.camera;
      this.ownAccount.camera = this.camera; 

      // hide the video and send data to update camera status
      this.mediaStream.getVideoTracks()[0].enabled = this.camera;
      this.sendEditAccount();

      // set the camera in Picture-in-Picture
      navigator.mediaSession.setCameraActive(this.camera);
    },
    sendEditAccount() {
      if (this.conn) {
        this.conn.send({
          type: 'edit',
          data: this.ownAccount,
        });
      }
    }
  }
}
</script>

<style>

</style>