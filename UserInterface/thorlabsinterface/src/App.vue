<template>
  <v-app>
    <v-main>
      <v-toolbar
  elevation="5"
  outlined
  prominent
  rounded
>
<v-app-bar-nav-icon @click="drawer = true"></v-app-bar-nav-icon>
</v-toolbar>
<v-navigation-drawer
      v-model="drawer"
      absolute
      temporary
    >
      <v-list
        nav
        dense
      >
        <v-list-item-group
          v-model="group"
          active-class="text--accent-4"
          v-for="item in itemslist" :key="item.dir"
        >
          <v-list-item>
            <v-list-item-icon @click="$router.push(item.dir);">
              <v-icon>{{item.icon}}</v-icon>
            </v-list-item-icon>
            <v-list-item-title>{{item.title}}</v-list-item-title>
          </v-list-item>

        </v-list-item-group>
      </v-list>
    </v-navigation-drawer>
      <router-view/>
    </v-main>
  </v-app>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
  name: 'App',

  data () {
    return {
      //
      drawer: false,
      group: null,
      itemslist: [{ title: 'Home', icon: 'mdi-home', dir: 'profile' },
        { title: 'Vision Macro', icon: 'mdi-magnify-scan', dir: 'about' },
        { title: 'Visitante', icon: 'mdi-monitor-multiple', dir: 'streamall' },
        { title: 'Thorlabs', icon: 'mdi-seesaw', dir: 'playground' }]
    }
  }
})
</script>
