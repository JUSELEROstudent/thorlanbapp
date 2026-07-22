<template>
  <v-container fluid="true">
    <v-card :elevation="5">
      <v-card-title><h1>Stitching de recorridos</h1></v-card-title>
      <v-card-text>
        <v-row>
          <v-col cols="8">
            <v-select
              v-model="selectedTourId"
              label="Selecciona un tour"
              item-title="label"
              item-value="IdTour"
              :items="tourItems"
              :loading="loadingTours"
              required
            ></v-select>
          </v-col>
          <v-col cols="4" align-self="center">
            <v-btn
              color="success"
              block
              large
              :disabled="!selectedTourId || running"
              :loading="running"
              @click="runStitching"
            >
              Ejecutar stitching
            </v-btn>
          </v-col>
        </v-row>

        <v-row v-if="selectedTour">
          <v-col cols="12">
            <p>
              Carpeta: <strong>{{ selectedTour.nameFolder }}</strong> —
              Estado del tour:
              <span :class="statusClass(selectedTour.endStatus)">{{ selectedTour.endStatus || 'desconocido (null)' }}</span>
            </p>
          </v-col>
        </v-row>

        <v-divider class="my-4"></v-divider>

        <v-row v-if="resultImageUrl">
          <v-col cols="12">
            <p>Resultado ({{ imagesUsed }} imágenes usadas):</p>
            <img :src="resultImageUrl" alt="Mosaico stitcheado" class="result-image" />
            <div class="mt-2">
              <a :href="resultImageUrl" target="_blank" download>Descargar imagen</a>
            </div>
          </v-col>
        </v-row>

        <v-row v-else-if="!running">
          <v-col cols="12">
            <p class="text-medium-emphasis">
              Selecciona un tour ya tomado y presiona "Ejecutar stitching" para ver el resultado aquí.
            </p>
          </v-col>
        </v-row>

        <v-divider class="my-4"></v-divider>

        <v-row>
          <v-col cols="12">
            <h3>Tours guardados</h3>
            <v-table density="compact" v-if="tours.length > 0">
              <thead>
                <tr>
                  <th>Id</th>
                  <th>Fecha</th>
                  <th>Carpeta</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="t in tours" :key="t.idTour">
                  <td>{{ t.idTour }}</td>
                  <td>{{ t.date }}</td>
                  <td>{{ t.nameFolder }}</td>
                  <td><span :class="statusClass(t.endStatus)">{{ t.endStatus || 'desconocido (null)' }}</span></td>
                  <td>
                    <v-btn
                      icon="mdi-delete"
                      size="small"
                      color="error"
                      variant="text"
                      :loading="deletingId === t.idTour"
                      @click="askDelete(t)"
                    ></v-btn>
                  </td>
                </tr>
              </tbody>
            </v-table>
            <p v-else-if="!loadingTours" class="text-medium-emphasis">No hay tours guardados todavía.</p>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-dialog v-model="deleteDialogOpen" max-width="440">
      <v-card v-if="tourToDelete">
        <v-card-title>Borrar tour</v-card-title>
        <v-card-text>
          ¿Seguro que quieres borrar el tour #{{ tourToDelete.idTour }} ({{ tourToDelete.nameFolder }})?
          Esto borra el registro y las imágenes guardadas en disco de ese tour. No se puede deshacer.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn @click="deleteDialogOpen = false">Cancelar</v-btn>
          <v-btn color="error" :loading="deleting" @click="confirmDelete">Borrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script>
import EnviromentApp from '../store/enviroment'

export default {
  app: 'stitching',
  data () {
    return {
      tours: [],
      loadingTours: false,
      selectedTourId: null,
      running: false,
      resultImageUrl: null,
      imagesUsed: 0,
      deleteDialogOpen: false,
      tourToDelete: null,
      deletingId: null,
      deleting: false
    }
  },
  computed: {
    tourItems: function () {
      return this.tours.map(t => ({
        IdTour: t.idTour,
        label: `#${t.idTour} - ${t.nameFolder} (${t.date}) - ${t.endStatus || 'sin terminar'}`
      }))
    },
    selectedTour: function () {
      return this.tours.find(t => t.idTour === this.selectedTourId) || null
    }
  },
  methods: {
    statusClass: function (status) {
      if (status === 'succes') return 'status-ok'
      if (status === 'error') return 'status-error'
      return 'status-unknown'
    },
    loadTours: function () {
      this.loadingTours = true
      const myHeaders = new Headers()
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
      fetch(`${EnviromentApp.URLSource}/api/Tour`, { method: 'GET', headers: myHeaders })
        .then(response => response.json())
        .then(data => { this.tours = data || [] })
        .catch(error => this.$store.dispatch('showAlert', { message: 'No se pudo cargar la lista de tours: ' + error, type: 'error', tittle: 'Error al cargar tours' }))
        .finally(() => { this.loadingTours = false })
    },
    runStitching: function () {
      if (!this.selectedTourId) return
      this.running = true
      this.resultImageUrl = null
      const myHeaders = new Headers()
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
      fetch(`${EnviromentApp.URLSource}/api/Stitching/${this.selectedTourId}`, { method: 'POST', headers: myHeaders })
        .then(async response => {
          const body = await response.json().catch(() => null)
          if (!response.ok) {
            const message = (body && (body.detail || body.title)) || body || `Error HTTP ${response.status}`
            throw new Error(message)
          }
          return body
        })
        .then(data => {
          this.imagesUsed = data.imagesUsed
          this.resultImageUrl = `${EnviromentApp.URLSource}${data.url}?t=${Date.now()}`
          this.$store.dispatch('showAlert', { message: `Stitching completado con ${data.imagesUsed} imágenes`, type: 'success', tittle: 'Listo' })
        })
        .catch(error => {
          this.$store.dispatch('showAlert', { message: error.message || error.toString(), type: 'error', tittle: 'Falló el stitching' })
        })
        .finally(() => { this.running = false })
    },
    askDelete: function (tour) {
      this.tourToDelete = tour
      this.deleteDialogOpen = true
    },
    confirmDelete: function () {
      if (!this.tourToDelete) return
      const idTour = this.tourToDelete.idTour
      this.deleting = true
      this.deletingId = idTour
      const myHeaders = new Headers()
      myHeaders.append('Authorization', 'Bearer ' + localStorage.getItem('stringjwt'))
      fetch(`${EnviromentApp.URLSource}/api/Tour/${idTour}`, { method: 'DELETE', headers: myHeaders })
        .then(response => {
          if (!response.ok && response.status !== 204) {
            throw new Error(`Error HTTP ${response.status}`)
          }
          this.tours = this.tours.filter(t => t.idTour !== idTour)
          if (this.selectedTourId === idTour) {
            this.selectedTourId = null
            this.resultImageUrl = null
          }
          this.$store.dispatch('showAlert', { message: `Tour #${idTour} borrado`, type: 'success', tittle: 'Listo' })
        })
        .catch(error => {
          this.$store.dispatch('showAlert', { message: 'No se pudo borrar el tour: ' + (error.message || error.toString()), type: 'error', tittle: 'Falló el borrado' })
        })
        .finally(() => {
          this.deleting = false
          this.deletingId = null
          this.deleteDialogOpen = false
          this.tourToDelete = null
        })
    }
  },
  mounted () {
    this.loadTours()
  }
}
</script>

<style scoped>
.result-image {
  max-width: 100%;
  border: 1px solid #ccc;
}
.status-ok {
  color: #1b8a3d;
  font-weight: bold;
}
.status-error {
  color: #c62828;
  font-weight: bold;
}
.status-unknown {
  color: #9e9e9e;
  font-style: italic;
}
</style>
