const { InjectManifest } = require('workbox-webpack-plugin')

// optional, but InjectManifest didn't respect the {mode: 'development'} config
// process.env.NODE_ENV = 'development'

module.exports = {
  configureWebpack: {
    // mode: 'development',
    plugins: [
      // new GenerateSW({
      //   navigateFallback: 'index.html',
      //   skipWaiting: true,
      // })

      // you have to comment this when you are in development mode
      new InjectManifest({
        swSrc: './src/service-worker.js',
        swDest: './service-worker.js',
        maximumFileSizeToCacheInBytes: 10000000, // (10mb)
      })
    ]
  },

  // Webpack Configuration
  // configureWebpack: {
  //   optimization: {
  //     splitChunks: {
  //       mixSize: 10000,
  //       maxSize: 250000,
  //     }
  //   }
  // }

  // enable HTTPS certificate
  // devServer: {
  //   open: process.platform === 'darwin',
  //   host: '0.0.0.0',
  //   port: 8080, // CHANGE YOUR PORT HERE!
  //   https: true,
  //   hotOnly: false,
  // },

  // when running npm run build, drop all the compile file inside wwwroot
  outputDir: '../backend/wwwroot',

  // //pwa
  // pwa: {
  //   name: 'ChatPWA',
  //   // appleMobileWebAppCapable: 'yes',
  //   // appleMobileWebAppStatusBarStyle: 'black',
  //   // manifestOptions: {
  //   //   start_url: '/login'
  //   // }

  //   // configure the workbox plugin
  //   // workboxPluginMode: 'InjectManifest',
  //   // workboxOptions: {
  //   //   // swSrc is required in InjectManifest mode.
  //   //   swSrc: 'src/service-worker.js',
  //   //   // ...other Workbox options...
  //   // }

  //   // // configure the workbox plugin
  //   // workboxPluginMode: 'GenerateSW',
  //   // workboxOptions: {
  //   //   navigateFallback: 'index.html',
  //   //   skipWaiting: true,
  //   //   // ...other Workbox options...
  //   // }
  // },

  // IE11 and Safari9 Browser Support for Vuetify
  transpileDependencies: [
    'vuetify'
  ]
}
