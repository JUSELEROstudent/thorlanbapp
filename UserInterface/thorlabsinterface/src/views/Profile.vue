<template>
  <div >
  <widget-motion></widget-motion>
  </div>
</template>

<script>
// import ChatBody from '@/components/components/Chat'
import WidgetMotion from '@/components/MovilitywidgetThotlab.vue'
export default ({
  name: 'profileUser',
  components: { WidgetMotion },
  data () {
    return {
      Userclass: [],
      listdevices: [],
      name: null,
      email: null,
      age: null,
      id: null,
      drawer: null,
      itemslist: [{ title: 'lista row 1', icon: 'mdi-arrow-up' }, { title: 'lista row 1', icon: 'mdi-share-all' }]
    }
  },
  methods: {
    getprofileinfo: function (dataProfile) {
      console.log(dataProfile)
      this.id = dataProfile[0].userId
      this.name = dataProfile[0].name
      this.email = dataProfile[0].email
      this.age = dataProfile[0].age
    },
    listdeviceson: function (deviceson) {
      console.log(deviceson)
      this.listdevices = deviceson
    }
  },
  mounted () {
    const myHeaders = new Headers()
    myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))

    const requestOptions = {
      method: 'GET',
      headers: myHeaders,
      redirect: 'follow'
    }

    fetch('https://localhost:7166/home/devices', requestOptions)
      .then(response => response.json())
      .then(data => this.listdeviceson(data))
      .catch(error => console.log('errror', error))
    console.log(this.listdevices)
  }

})
</script>

<style scoped>
.card .card-header {
  color: var(--bg-cinco);
}
.card-body{
  display: inline-block;
}
</style>
