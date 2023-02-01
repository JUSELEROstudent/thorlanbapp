import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import PofileUser from '@/views/Profile.vue'
import StreamToAll from '@/views/StreamtoAllView.vue'
import PlayGround from '@/views/PlayGround.vue'
import LoginForm from '@/components/Login.vue'
import CurrentState from '@/views/CurrentState.vue'
import AutoMove from '@/views/AutomaticMovement.vue'
import ShareAll from '@/views/ShareAll.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView
  },
  {
    path: '/login',
    name: 'login',
    component: LoginForm
  },
  {
    path: '/about',
    name: 'about',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/AboutView.vue')
  },
  {
    path: '/profile',
    name: 'profile',
    component: PofileUser
  },
  // {
  //   path: '/streamall',
  //   name: 'streamall',
  //   component: StreamToAll
  // },
  {
    path: '/streamall',
    name: 'streamall',
    component: ShareAll
  },
  {
    path: '/playground',
    name: 'playground',
    component: PlayGround
  },
  {
    path: '/currentstate',
    name: 'currentstate',
    component: CurrentState
  },
  {
    path: '/automatic',
    name: 'atimatic',
    component: AutoMove
  }
]

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
})

export default router
