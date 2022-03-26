import { precacheAndRoute, createHandlerBoundToURL } from 'workbox-precaching';
// import { CacheFirst } from 'workbox-strategies';
// import { ExpirationPlugin } from 'workbox-expiration';
import { registerRoute, NavigationRoute } from 'workbox-routing';
import { setCacheNameDetails } from 'workbox-core';
import { googleFontsCache } from 'workbox-recipes';

// workbox.setConfig({
//   debug: false,
// });

/**
 * The workboxSW.precacheAndRoute() method efficiently caches and responds to
 * requests for URLs in the manifest.
 * See https://goo.gl/S9QRab
 */

// set precache name
setCacheNameDetails({prefix: "frontend"});

// 4.3.0 workbox
// self.__precacheManifest = [].concat(self.__precacheManifest || []);
// workbox.precaching.precacheAndRoute(self.__precacheManifest, {});
// registerNavigationRoute('index.html');
// 6.3.0 workbox
// precache build file
precacheAndRoute(self.__WB_MANIFEST);
// fallback to index.html
registerRoute(new NavigationRoute(createHandlerBoundToURL('/index.html')))

self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});

// Cache Google Font Api
googleFontsCache();
// registerRoute(
//   new RegExp("https://fonts.(?:googleapis|gstatic).com/(.*)"),
//   new CacheFirst({
//     cacheName: 'googleapis',
//     plugins: [
//       new ExpirationPlugin({
//         maxEntries: 30
//       })
//     ],
//     method: "GET",
//     cacheableResponse: { statuses: [0, 200] }
//   })
// );

// Web Push Notification
self.addEventListener("push", (e) => {

  // get the payload data -------------------------------------------------------------------------------
  let data = e.data.json();

  // assign open url and tag -----------------------------------------------------------------------------
  let tag = "";
  let openURL = "";
  if (data.PrivateId) {
    openURL = `${self.location.origin}/private/${data.PrivateId}`;
    tag = `private-${data.PrivateId}`;
  }
  else if (data.GroupId) {
    openURL = `${self.location.origin}/group/${data.GroupId}`;
    tag = `group-${data.GroupId}`;
  }
  else {
    openURL = self.location.origin;
    tag = "else";
  }

  // A function that check if the browser is currently focused ------------------------------------------
  const isClientFocused = (notificationTitle, options) => {
    return clients.matchAll({
      type: 'window',
      includeUncontrolled: true
    }).then((windowClients) => {
      // to detect is our website tab is focusing
      let clientIsFocused = false;
  
      // loop through the user tab (only can detect our origin tab)
      for (let i = 0; i < windowClients.length; i++) {
        // get the window client
        const windowClient = windowClients[i];

        // if our website tab is focusing, assign focus is true
        // so that it wont show the notification
        if (windowClient.focused) {
          clientIsFocused = true;
          break;
        }
      }
  
      // if is focusing, then dont show any notification
      if (clientIsFocused) {
        return;
      }

      // else show notification
      return self.registration.showNotification(notificationTitle, options);
    });
  }

  // Merge notification ---------------------------------------------------------------------------------
  const promiseChain = registration.getNotifications()
    .then(notifications => {
      
      // to get the notification that is available previously
      let currentNotification;

      // loop through the notification to check if there is the same openURL data 
      // if it is the same, then we assign that notification
      // this is to merge the notification later
      for(let i = 0; i < notifications.length; i++) {
        if (notifications[i].data &&
          notifications[i].data.openURL === openURL) {
          currentNotification = notifications[i];
        }
      }

      // return notification
      return currentNotification;

    }).then((currentNotification) => {

      // prepare notification options
      const options = {
        icon: './img/icons/android-chrome-512x512.png',
        // image: './img/icons/android-chrome-512x512.png',
        badge: './img/icons/android-chrome-512x512.png',
        vibrate: [200, 100, 200, 100, 200, 100, 200],
        tag: tag,
        // renotify: true, // very annoying, only for debug purpose
        data: {
          openURL: openURL,
          newMessageCount: 1,
        }
      };
      let notificationTitle = "";
    
      if (currentNotification) {
        // get the message count from the notification data
        const messageCount = currentNotification.data.newMessageCount + 1;
    
        // assign body and current message count
        options.body = `You have ${messageCount} new messages from ${data.SenderName}.`;
        options.data.newMessageCount = messageCount;

        // assign notification title
        notificationTitle = `New Messages from ${data.SenderName}`;
    
        // Remember to close the old notification.
        currentNotification.close();

      } else {
        options.body = `${data.Message}`;
        notificationTitle = `New Message from ${data.SenderName}`;
      }
      
      // Send notification by checking whether the website is on focus or not
      // if is on focus, dont send
      // else, send the notification
      return isClientFocused(notificationTitle, options);

      // for debugging purpose
      // return self.registration.showNotification(notificationTitle, options);
    });

  // send the notification
  e.waitUntil(promiseChain);
});


// After notification is clicked
self.addEventListener("notificationclick", (e) => {

  // get the notification data
  const openURL = e.notification.data.openURL;

  // get the notification and close the notification
  const clickedNotification = e.notification;
  clickedNotification.close();

  // open the url or focus the open tab whenever a user click the notification
  const promiseChain = clients.matchAll({
    type: 'window',
    includeUncontrolled: true
  }).then((windowClients) => {

    let matchingClient = null;
  
    // loop through the window clients tab (only our origin)
    for (let i = 0; i < windowClients.length; i++) {
      const windowClient = windowClients[i];

      const url = new URL(windowClient.url);

      // if the url is in the chatPage, assign client to matchingClient variable
      // so that it will focus that tab rather than open a new tab
      if (url.pathname === '/' || url.pathname.startsWith('/private') || url.pathname.startsWith('/group')) {
        matchingClient = windowClient;
        break;
      }
    }
    
    // if our chat window found, focus the tab, else open a new tab
    if (matchingClient) {
      return matchingClient.focus();
    } else {
      return clients.openWindow(openURL);
    }
  });
  
  e.waitUntil(promiseChain);
});