import Vue from 'vue'
import VueRouter from 'vue-router'
import Home from '../views/Home.vue'
import Login from '../views/Login.vue'
import ResetPassword from '../views/ResetPassword.vue'
import ChangePassword from '../views/ChangePassword.vue'
import Profile from '../views/Profile.vue'
import ImageCapture from '../views/ImageCapture.vue'
import EmptyMessage from '../views/EmptyMessage.vue'
import EmptyAccount from '../views/EmptyAccount.vue'
import Private from '../views/Private.vue'
import PrivateInfo from '../views/PrivateInfo.vue'
import PrivateMessageInfo from '../views/PrivateMessageInfo.vue'
import Group from '../views/Group.vue'
import GroupInfo from '../views/GroupInfo.vue'
import GroupMessageInfo from '../views/GroupMessageInfo.vue'
import NewGroup from '../views/NewGroup.vue'
import Account from '../views/Account.vue'
import AccountCreate from '../views/AccountCreate.vue'
import AccountUpdate from '../views/AccountUpdate.vue'
import Meeting from '../views/Meeting.vue'
import NotFound from '../views/NotFound.vue'
import store from '../store'

Vue.use(VueRouter);

const routes = [
  { path: '/login'          , name: 'Login'         , component: Login                     },
  { path: '/reset-password' , name: 'ResetPassword' , component: ResetPassword             },
  { path: '/'               , component: Home, children: [
      { path: ''                         , name: 'Home'            , component: EmptyMessage        , meta: { requireAuth: true }},
      { path: 'profile'                  , name: 'Profile'         , component: Profile             , meta: { requireAuth: true }},
      { path: 'profile/image-capture'    , name: 'ImageCapture'    , component: ImageCapture        , meta: { requireAuth: true }},
      { path: 'private/:privateId'       , name: 'Private'         , component: Private             , meta: { requireAuth: true }, props: true,  children: [
          { path: 'info'                         , name: 'PrivateInfo'         , component: PrivateInfo            , meta: { requireAuth: true }, props: true },
          { path: 'message/:messageId'           , name: 'PrivateMessageInfo'  , component: PrivateMessageInfo     , meta: { requireAuth: true }, props: true },
        ],
      },
      { path: 'group/:groupId'                 , name: 'Group'               , component: Group                , meta: { requireAuth: true }, props: true,  children: [
        { path: 'info'                         , name: 'GroupInfo'           , component: GroupInfo            , meta: { requireAuth: true }, props: true },
        { path: 'message/:messageId'           , name: 'GroupMessageInfo'    , component: GroupMessageInfo     , meta: { requireAuth: true }, props: true },
      ],
    },
      { path: 'new-group'             , name: 'NewGroup'      , component: NewGroup            , meta: { requireAuth: true }},
      { path: 'change-password'       , name: 'ChangePassword', component: ChangePassword      , meta: { requireAuth: true }},
    ]
  },
  { path: '/account', component: Account, children: [
      { path: ''                , name: 'Account'             , component: EmptyAccount     , meta: { requireAuth: true }},
      { path: 'create'          , name: 'AccountCreate'       , component: AccountCreate    , meta: { requireAuth: true }},
      { path: ':id'             , name: 'AccountUpdate'       , component: AccountUpdate    , meta: { requireAuth: true } , props: true },
    ] 
  },
  { path: '/meeting/:meetId'    , name: 'Meeting' , component: Meeting                 , meta: { requireAuth: true }, props: true },
  { path: '/:catchAll(.*)'      , name: 'NotFound', component: NotFound               },
  // {
  //   path: '/about',
  //   name: 'About',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/About.vue')
  // }
];

const router = new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes
});

// Global Navigation Route BeforeEach
router.beforeEach(async (to, from, next) => {

  // cancel all the speechsynthesis queue every route visit
  // this is because speechSynthesis will still play eventhough the browser is fresh
  window.speechSynthesis.cancel();

  // remove overlay whenever a page is navigated
  // this is to prevent overlay will still appear when a user is navigate away
  store.commit('dialog/toggleOverlay', false);

  let getCookie = (cname) => {
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

  // if route requireAuth, and there is no jwtToken in cookie
  // redirect them to login page
  if (to.meta.requireAuth && getCookie("jwt_token") == null) {
    next({ name: 'Login' });
  }
  else {
    next();
  }
});

export default router;
