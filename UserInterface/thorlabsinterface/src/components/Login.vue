<template>
  <div class="bodyLogin">
    <h1 id="displayerrormsg">Titulo de login</h1>
    <v-form @submit.prevent="this.veryfylogin">
      <v-text-field
      v-model="user"
      label=" Usuario o contrasena">
     </v-text-field>

      <v-text-field
      :append-icon="passwordseen ? 'mdi-eye' : 'mdi-eye-off'"
      :type="passwordseen ? 'text' : 'password'"
      @click:append="passwordseen = !passwordseen"
      label=" inserte la conrasena"
      v-model="password" >
      </v-text-field>
      <v-btn
      @click="veryfylogin"
      color="success"
       >enviar</v-btn >
    </v-form>
  </div>
</template>

<script>
export default {
  name: 'UserLogin',
  data () {
    return {
      passwordseen: true,
      user: null,
      password: null
    }
  },
  methods: {
    async veryfylogin () {
      console.log('Entra A la PETICION')
      const myHeaders = new Headers()
      myHeaders.append('Content-Type', 'application/json')
      const raw = JSON.stringify({
        user: this.user,
        password: this.password
      })

      const requestOptions = {
        method: 'POST',
        headers: myHeaders,
        body: raw
      }
      const response = await fetch('https://localhost:7166/innerlogin', requestOptions)
      const data = await response.text()
      if (response.ok) {
        localStorage.setItem('stringjwt', data) // Guardado del token generado en la peticion para la autenticacion.
        console.log(data + 'ahora se hace DIFERENTE LA PETICION')
        window.location.href = 'http://192.168.1.37:4040/playground'
      } else {
        console.log('ocurrio un error haciendo la peticion')
        // document.getElementById('displayerrormsg')[0].innerHTML = 'nombre de usuario o contrasean incorrecta'
      }
    }
  }
}
</script>

<style scoped>
.bodyLogin {
  width: 60%;
  max-width: 300px;
  height: auto;
  margin: auto;
  background: rgb(198, 202, 176);
  margin-top: 200px;
  padding: 20px;
  border-radius: 10px;
}
form {
  text-align: left;
  background: rgb(10, 114, 211);
  border: rgb(52, 54, 52);
  border-radius: 8px;
  color: white;
  width: auto;
  margin: auto;
  padding: 20px;
  display: grid;
}
button {
  display: block;
  margin: 25px;
  border-radius: 9px;
  border: solid rgb(76, 212, 194) 1px;
}

</style>
