import Vue from 'vue';
// import router from '../router'

const methods = {
  install (Vue) {
    // Global Function: Set Theme --------------------------------------------------------------------
    Vue.prototype.$setTheme = (dark) => {
      if (!dark) localStorage.setItem('light', true);
      else localStorage.removeItem('light');
    };

    // Global Function: Get Cookie -------------------------------------------------------------------
    Vue.prototype.$getCookie = (cname) => {
      let name = cname + "=";
      let ca = document.cookie.split(';');
      for(let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
          c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
          return c.substring(name.length, c.length);
        }
      }
      return null;
    }

    // Global Function: Logout -----------------------------------------------------------------------
    Vue.prototype.$logout = () => {

      // remove cookie login info
      document.cookie = "jwt_token= ; expires = Thu, 01 Jan 1970 00:00:00 GMT";
      document.cookie = "name= ; expires = Thu, 01 Jan 1970 00:00:00 GMT";
      document.cookie = "uid= ; expires = Thu, 01 Jan 1970 00:00:00 GMT";
      document.cookie = "is_admin= ; expires = Thu, 01 Jan 1970 00:00:00 GMT";
      
    };

    // Global Function: Refresh ----------------------------------------------------------------------
    Vue.prototype.$refresh = () => window.location.reload();

    // Global Function: Timeout ----------------------------------------------------------------------
    Vue.prototype.$timeout = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    // Global Function: Html Encode ------------------------------------------------------------------
    const htmlEncode = (str) => {
      return String(str).replace(/[^\w. ]/gi, function(c){
        return '&#'+c.charCodeAt(0)+';';
      });
    }

    Vue.prototype.$htmlEncode = htmlEncode;

    // Global Function: Highlight Search ------------------------------------------------------------
    Vue.prototype.$highlightSearch = (search, content) => {
      return search == '' 
        ? htmlEncode(content) 
        : htmlEncode(content).replace(new RegExp(htmlEncode(search), "gi"), match => {
          return '<span class="yellow black--text">' + match + '</span>';
        });
    };

    // Global Function: File Extension Validation  --------------------------------------------------
    const isValidFileExtension = (arrayOfTypes, fileName) => {
      // only allow certain type
      // Regex: /(\.jpg|\.jpeg|\.png)$/i
      const extRegex = new RegExp('(\\' + arrayOfTypes.join('|\\') + ')$', 'i')
      if (extRegex.exec(fileName)) return true;
      else return false;
    }

    Vue.prototype.$isValidFileExtension = isValidFileExtension;

    // list of file extension supported -------------------------------------------------------------
    const image = ['.jpg', '.png', '.jpeg', '.gif'];
    const audio = ['.mp3', '.wav'];
    const video = ['.mp4'];
    const otherFiles = ['.pdf', '.zip', '.txt', '.doc', '.docx'];
    const fileExtension = image.concat(audio, video, otherFiles);

    Vue.prototype.$imageExtension = image;
    Vue.prototype.$audioExtension = audio;
    Vue.prototype.$videoExtension = video;
    Vue.prototype.$otherFilesExtension = otherFiles;
    Vue.prototype.$fileExtension = fileExtension;

    Vue.prototype.$checkImageExtension = (fileName) => isValidFileExtension(image, fileName);
    Vue.prototype.$checkAudioExtension = (fileName) => isValidFileExtension(audio, fileName);
    Vue.prototype.$checkVideoExtension = (fileName) => isValidFileExtension(video, fileName);
    Vue.prototype.$checkDocumentExtension = (fileName) => isValidFileExtension(otherFiles, fileName);
    Vue.prototype.$checkFileExtension = (fileName) => isValidFileExtension(fileExtension, fileName);

    // Global Function: get 12 hour ampm time with date object --------------------------------------
    Vue.prototype.$getTime = (date) => {
      let hours = date.getHours();
      let minutes = date.getMinutes();
      let ampm = hours >= 12 ? 'pm' : 'am';

      hours = hours % 12;
      hours = hours ? hours : 12; // the hour '0' should be '12'
      minutes = minutes < 10 ? '0'+minutes : minutes;

      return `${hours}:${minutes} ${ampm}`;
    }

    // Global Function: convert file object to base64 data url -------------------------------------
    Vue.prototype.$toBase64 = (file) => new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result);
      reader.onerror = error => reject(error);
    });

    // Global Function: Display formatted bytes value ----------------------------------------------
    Vue.prototype.$formatBytes = (bytes, decimals = 0) => {
      if (bytes === 0) return '0 Bytes';

      const k = 1024;
      const dm = decimals < 0 ? 0 : decimals;
      const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

      const i = Math.floor(Math.log(bytes) / Math.log(k));

      return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    };

    // Global Function: convert base64 data url to blob -------------------------------------------
    let urlToBlob = async (url) => await (await fetch(url)).blob();
    Vue.prototype.$urlToBlob = async (url) => await urlToBlob(url);

    // Global Function: convert base64 File Object ------------------------------------------------
    Vue.prototype.$urlToFile = async (url, fileName, contentType) => {
      let blob = await urlToBlob(url);
      return new File([blob], fileName,{ type: contentType })
    }

    // Global Function: convert base64 to Uint8Array ----------------------------------------------
    Vue.prototype.$urlBase64ToUint8Array = (base64String) => {
      let padding = '='.repeat((4 - base64String.length % 4) % 4);
      let base64 = (base64String + padding)
          .replace(/\-/g, '+')
          .replace(/_/g, '/');
  
      let rawData = window.atob(base64);
      let outputArray = new Uint8Array(rawData.length);
  
      for (let i = 0; i < rawData.length; ++i) {
          outputArray[i] = rawData.charCodeAt(i);
      }
      return outputArray;
    }

    // Global Function: convert arrayBuffer to base64 ----------------------------------------------
    Vue.prototype.$arrayBufferToBase64 = (buffer) => {
      let binary = '';
      let bytes = new Uint8Array(buffer);
      let len = bytes.byteLength;
      for (let i = 0; i < len; i++) {
          binary += String.fromCharCode( bytes[i] );
      }
      return window.btoa(binary);
    }

    // Gloal Function: to cancel speech synthesis -------------------------------------------------
    Vue.prototype.$cancelSpeech = () => window.speechSynthesis.cancel();
    
  }
}

// Apply global methods/functions as plugin
Vue.use(methods)