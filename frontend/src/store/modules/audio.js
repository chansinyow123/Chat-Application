// for chats
export default {
  namespaced: true,
  state: () => ({ 
    audioElement: null,
  }),
  mutations: {
    setAudio(state, { audio, title, image }) {

      // if the reference of the audioElement is the same as the audio pass here
      // do nothing
      if (state.audioElement === audio) return;
      
      // if audioElement is still here, pause and reset it
      if (state.audioElement) {
        state.audioElement.pause();
        state.audioElement.currentTime = 0;
      }

      // assign audio element
      state.audioElement = audio;

      // if mediaSession is not supported in this browser
      // do nothing
      if (!'mediaSession' in navigator) return;

      // prepare a function for update mediaSession API whenver an audio property is changed
      const updatePositionState = () => {

        // if duration is infinity, then do nothing
        if (!isFinite(audio.duration)) return;

        // set the position state of mediaSession
        navigator.mediaSession.setPositionState({
          duration: audio.duration,
          playbackRate: audio.playbackRate,
          position: audio.currentTime
        });
      };

      // event listener for mediaSession API
      const actionsAndHandlers = [
        ['play', () => {
          audio.play();
          updatePositionState();
        }],
        ['pause', () => { 
          audio.pause(); 
        }],
        // ['seekbackward', (details) => {
        //   audio.currentTime = audio.currentTime - (details.seekOffset || 10);
        //   updatePositionState();
        // }],
        // ['seekforward', (details) => {
        //   audio.currentTime = audio.currentTime + (details.seekOffset || 10);
        //   updatePositionState();
        // }],
        ['seekto', (details) => {
          if (details.fastSeek && 'fastSeek' in audio) {
            audio.fastSeek(details.seekTime);
            updatePositionState();
            return;
          }
          audio.currentTime = details.seekTime;
          updatePositionState();
        }],
        ['stop', () => {
          audio.pause();
          audio.currentTime = 0;
        }],
        // ['previoustrack', () => {  }],
        // ['nexttrack', () => {  }],
        // ['skipad', () => {  }],
      ];

      // prepare mediaSession API metadata
      navigator.mediaSession.metadata = new MediaMetadata({
        title: title,
        artwork: [
          { src: image, sizes: '96x96'  , type: 'image/png' },
          { src: image, sizes: '128x128', type: 'image/png' },
          { src: image, sizes: '192x192', type: 'image/png' },
          { src: image, sizes: '256x256', type: 'image/png' },
          { src: image, sizes: '384x384', type: 'image/png' },
          { src: image, sizes: '512x512', type: 'image/png' },
        ]
      });

      // declare event listener for mediaSession API
      // and check if some handler is not supported
      for (const [action, handler] of actionsAndHandlers) {
        try {
          navigator.mediaSession.setActionHandler(action, handler);
        } catch (error) {
          console.log(`The media session action, ${action}, is not supported`);
        }
      }
    },
  },
}