<template>
  <div >
    <div class="row">
      <div class="col-5">
        <div class="card bg-tres">
          <h3 class="card-header">{{name}}</h3>
          <div class="card-body bg-cinco">
            <div class="card-text"> Email: {{email}}</div>
            <div class="card-text"> Edad: {{age}}</div>
            <div class="card-text">Identificacion: {{id}}</div>
          </div>
        </div>
      </div>
    </div>
  <chat-body class="col-5"></chat-body>
  </div>
</template>

<script>
// import ChatBody from '@/components/components/Chat'

export default ({
  name: 'profileUser',
  // components: { ChatBody },
  data () {
    return {
      Userclass: [],
      name: null,
      email: null,
      age: null,
      id: null
    }
  },
  methods: {
    getprofileinfo: function (dataProfile) {
      console.log(dataProfile)
      this.id = dataProfile[0].userId
      this.name = dataProfile[0].name
      this.email = dataProfile[0].email
      this.age = dataProfile[0].age
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

    fetch('https://localhost:7166/api/login', requestOptions)
      .then(response => response.json())
      .then(data => this.getprofileinfo(data))
      .catch(error => console.log('error', error))
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
