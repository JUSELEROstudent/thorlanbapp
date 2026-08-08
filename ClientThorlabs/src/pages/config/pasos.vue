<template>
  <div class="flex flex-col gap-4">

    <!-- Selección del combo -->
    <div class="card bg-base-100 shadow">
      <div class="card-body gap-3">
        <h2 class="card-title text-base">Paso real del motor</h2>
        <p class="text-sm text-gray-500 max-w-3xl">
          Mide cuántos nanómetros avanza realmente la platina por cada paso, con un pie de
          rey. El sistema usa hoy un valor nominal de 30&nbsp;nm que nadie ha verificado, y de
          ese número depende cuántas imágenes se toman para cubrir el área que se pide en
          milímetros.
        </p>

        <div class="flex flex-wrap gap-3 items-end">
          <label class="form-control w-full max-w-md">
            <span class="label-text text-xs">Combo de calibración</span>
            <select v-model="selectedGroupId" class="select select-bordered select-sm" @change="loadCalibrations">
              <option value="">Seleccione un combo…</option>
              <option v-for="g in groups" :key="g.groupCailbrationId" :value="g.groupCailbrationId">
                {{ g.aditionalInfo || 'Sin descripción' }} — {{ g.cameraName || '?' }} · {{ g.increaseName || '?' }}
              </option>
            </select>
          </label>

          <button class="btn btn-sm btn-outline" :disabled="!selectedGroupId || isLoading" @click="loadCalibrations">
            <span v-if="isLoading" class="loading loading-spinner loading-xs"></span>
            Actualizar
          </button>
        </div>
      </div>
    </div>

    <template v-if="selectedGroupId">

      <!-- Sin caracterización: crear o copiar -->
      <div v-if="!active" class="card bg-base-100 shadow">
        <div class="card-body gap-4">
          <h3 class="font-semibold text-sm">Este combo todavía no tiene el paso medido</h3>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
            <label class="form-control">
              <span class="label-text text-xs">Dispositivo KIM</span>
              <select v-model="form.kimDeviceId" class="select select-bordered select-sm">
                <option value="">Seleccione…</option>
                <option v-for="d in kimDevices" :key="d" :value="d">{{ d }}</option>
              </select>
            </label>
            <label class="form-control">
              <span class="label-text text-xs">StepRate (pasos/s)</span>
              <input
                v-model.number="form.stepRate"
                type="number"
                :min="LIMITS.stepRate.min"
                :max="LIMITS.stepRate.max"
                class="input input-bordered input-sm"
                :class="{ 'input-error': !inRange('stepRate') }"
                @change="clampField('stepRate')"
              />
              <span class="label-text-alt text-xs" :class="inRange('stepRate') ? 'text-gray-400' : 'text-error'">
                Máximo {{ LIMITS.stepRate.max }}
              </span>
            </label>
            <label class="form-control">
              <span class="label-text text-xs">StepAcceleration</span>
              <input
                v-model.number="form.stepAcceleration"
                type="number"
                :min="LIMITS.stepAcceleration.min"
                :max="LIMITS.stepAcceleration.max"
                class="input input-bordered input-sm"
                :class="{ 'input-error': !inRange('stepAcceleration') }"
                @change="clampField('stepAcceleration')"
              />
              <span class="label-text-alt text-xs" :class="inRange('stepAcceleration') ? 'text-gray-400' : 'text-error'">
                Máximo {{ LIMITS.stepAcceleration.max.toLocaleString() }}
              </span>
            </label>
            <label class="form-control">
              <span class="label-text text-xs">Nota (opcional)</span>
              <input v-model="form.aditionalInfo" type="text" class="input input-bordered input-sm" />
            </label>
          </div>

          <div class="alert alert-info text-xs">
            <span>
              Estos parámetros deben ser los mismos con los que se hará el recorrido: en un
              actuador piezoeléctrico inercial el tamaño de paso cambia con la velocidad y la
              aceleración, así que medirlo accionando de otro modo daría un número que no aplica.
            </span>
          </div>

          <div class="flex flex-wrap gap-2">
            <button class="btn btn-primary btn-sm" :disabled="!canCreate || isBusy" @click="createCalibration">
              Iniciar medición
            </button>

            <div class="divider divider-horizontal">o</div>

            <div class="flex gap-2 items-end">
              <label class="form-control">
                <span class="label-text text-xs">Copiar desde otro combo</span>
                <select v-model="copySourceId" class="select select-bordered select-sm">
                  <option value="">Seleccione origen…</option>
                  <option v-for="c in copyCandidates" :key="c.motorCalibrationId" :value="c.motorCalibrationId">
                    {{ c.groupLabel }} — {{ c.summary }}
                  </option>
                </select>
              </label>
              <button class="btn btn-outline btn-sm" :disabled="!copySourceId || isBusy" @click="copyFrom">
                Copiar
              </button>
            </div>
          </div>
          <p class="text-xs text-gray-500 max-w-3xl">
            Copiar solo tiene sentido si el montaje mecánico es el mismo y únicamente cambió
            el objetivo: el paso del motor no depende de la óptica.
          </p>
        </div>
      </div>

      <!-- Caracterización activa -->
      <template v-else>
        <div class="card bg-base-100 shadow">
          <div class="card-body gap-3">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h3 class="font-semibold text-sm">
                  Medición
                  <span class="badge badge-sm ml-2" :class="active.status === 'complete' ? 'badge-success' : 'badge-warning'">
                    {{ active.status === 'complete' ? 'Completa' : 'En progreso' }}
                  </span>
                  <span v-if="active.acepted === 1" class="badge badge-sm badge-primary ml-1">Vigente</span>
                </h3>
                <p class="text-xs text-gray-500">
                  KIM {{ active.kimDeviceId }} · StepRate {{ active.stepRate }} ·
                  StepAcceleration {{ active.stepAcceleration }}
                </p>
              </div>
              <div class="flex gap-2">
                <button
                  v-if="active.status === 'complete' && active.acepted !== 1"
                  class="btn btn-primary btn-sm" :disabled="isBusy" @click="accept"
                >
                  Marcar como vigente
                </button>
                <button class="btn btn-ghost btn-sm" :disabled="isBusy" @click="discard">Descartar</button>
              </div>
            </div>

            <!-- Plan de medición -->
            <div class="flex flex-wrap gap-3 items-end">
              <label class="form-control">
                <span class="label-text text-xs">Pasos por parada</span>
                <input
                  v-model.number="plan.stepsPerStop" type="number" min="100" step="1000"
                  class="input input-bordered input-sm w-36" :disabled="planLocked"
                />
              </label>
              <label class="form-control">
                <span class="label-text text-xs">Número de paradas</span>
                <input
                  v-model.number="plan.stopCount" type="number" min="3" max="20"
                  class="input input-bordered input-sm w-32" :disabled="planLocked"
                />
              </label>
              <div class="text-xs text-gray-500 pb-2">
                Recorrido estimado
                <span class="font-mono">{{ estimatedTravelMm }}&nbsp;mm</span> ·
                <span class="font-mono">{{ estimatedMinutes }}&nbsp;min</span> por travesía
              </div>
            </div>
            <p v-if="planLocked" class="text-xs text-gray-500">
              El plan se deduce de las lecturas ya registradas y no se puede cambiar a mitad
              de una travesía: los puntos deben pertenecer a la misma serie.
            </p>
          </div>
        </div>

        <!-- Un panel por eje -->
        <div v-for="axis in active.axes" :key="axis.axisStepCalibrationId" class="card bg-base-100 shadow">
          <div class="card-body gap-3">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <h3 class="font-semibold text-sm">
                Eje {{ axis.axisName.toUpperCase() }}
                <span class="text-xs font-normal text-gray-500">
                  ({{ axis.axisName === 'x' ? 'Channel1' : 'Channel2' }})
                </span>
              </h3>
              <span class="badge badge-sm" :class="axis.status === 'complete' ? 'badge-success' : 'badge-ghost'">
                {{ axis.status === 'complete' ? 'Medido' : 'Pendiente' }}
              </span>
            </div>

            <!-- Resultados -->
            <div v-if="axis.stepSizeNm" class="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
              <div>
                <p class="text-xs text-gray-400">Ida</p>
                <p class="font-mono">{{ fmt(axis.stepSizeNmForward) }} nm</p>
              </div>
              <div>
                <p class="text-xs text-gray-400">Vuelta</p>
                <p class="font-mono">{{ fmt(axis.stepSizeNmBackward) }} nm</p>
              </div>
              <div>
                <p class="text-xs text-gray-400">Media (la que se usa)</p>
                <p class="font-mono font-semibold">{{ fmt(axis.stepSizeNm) }} nm</p>
              </div>
              <div>
                <p class="text-xs text-gray-400">Error relativo</p>
                <p class="font-mono">± {{ fmt(axis.relativeErrorPct) }} %</p>
              </div>
            </div>

            <div v-if="axis.hysteresisWarning" class="alert alert-warning text-xs">
              <span>
                Histéresis del {{ fmt(axis.hysteresisPct) }} % entre ida y vuelta. El recorrido
                mueve el eje Y alternando el sentido en cada columna, así que una diferencia
                así hace que columnas contiguas avancen distancias distintas. No bloquea nada:
                todo sigue funcionando igual.
              </span>
            </div>

            <!-- Travesías -->
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <div v-for="dir in directions" :key="dir.key" class="rounded border border-base-200 p-3">
                <div class="flex items-center justify-between mb-2">
                  <h4 class="text-xs font-semibold uppercase tracking-wide text-gray-500">{{ dir.label }}</h4>
                  <span class="text-xs text-gray-400">
                    {{ readingsFor(axis, dir.key).length }} / {{ plan.stopCount + 1 }} lecturas
                  </span>
                </div>

                <!-- Dos columnas distintas a propósito. «Posición» es adónde va el motor:
                     en la ida sube desde 0 y en la vuelta baja desde el extremo. «Δ pasos»
                     es el desplazamiento acumulado desde el inicio de la travesía, que
                     crece igual en ambos sentidos y es lo que consume la regresión. Antes
                     solo se mostraba Δ, y las dos travesías parecían idénticas. -->
                <table class="table table-xs">
                  <thead>
                    <tr>
                      <th>Parada</th>
                      <th class="text-right">Posición</th>
                      <th class="text-right">Δ pasos</th>
                      <th>Lectura (mm)</th>
                      <th class="text-right">Δ (mm)</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="stop in stopsFor(dir.key)" :key="`${dir.key}-${stop.sequence}`">
                      <td>{{ stop.sequence }}</td>
                      <td class="text-right font-mono">{{ stop.absolute.toLocaleString() }}</td>
                      <td class="text-right font-mono text-gray-500">{{ stop.steps.toLocaleString() }}</td>
                      <td>
                        <input
                          v-model="readingInputs[inputKey(axis, dir.key, stop.sequence)]"
                          type="number" step="0.01"
                          class="input input-bordered input-xs w-24 font-mono"
                          :placeholder="stop.sequence === 0 ? 'partida' : '—'"
                          @change="saveReading(axis, dir.key, stop)"
                        />
                      </td>
                      <td class="text-right font-mono text-gray-500">
                        {{ displacementOf(axis, dir.key, stop.sequence) }}
                      </td>
                    </tr>
                  </tbody>
                </table>

                <div class="flex flex-wrap gap-2 mt-2 items-center">
                  <button
                    class="btn btn-xs btn-outline"
                    :disabled="isMoving || isBusy"
                    @click="moveTo(axis, dir.key, nextStop(axis, dir.key))"
                  >
                    Mover a la parada {{ nextStop(axis, dir.key) }}
                    <span class="font-mono opacity-70">
                      (pos. {{ absoluteOf(dir.key, nextStop(axis, dir.key)).toLocaleString() }})
                    </span>
                  </button>
                  <span v-if="isMoving && movingLabel === dir.key + axis.axisName" class="text-xs text-gray-500 flex items-center gap-2">
                    <span class="loading loading-spinner loading-xs"></span>
                    Moviendo… {{ movingElapsed }}s de ~{{ secondsPerStop }}s
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup lang="ts">
/**
 * Medición del tamaño real del paso del motor con pie de rey.
 *
 * El procedimiento es una travesía con paradas: se mueve el motor un número fijo de
 * pasos, el operador lee el instrumento, y con todos los puntos se ajusta una regresión
 * por el origen —la misma que usa la calibración óptica—. Diez paradas dan mejor
 * precisión que repetir tres veces la misma medida, porque el error del instrumento se
 * reparte entre todos los puntos en lugar de recaer sobre uno.
 *
 * Cada lectura se guarda en el servidor en cuanto se introduce. La medición dura unos
 * veinte minutos por eje y no siempre se termina de una sentada, así que el estado vive
 * en la base y no en el navegador: al volver, la vista se reconstruye sola.
 */
import { ref, reactive, computed, onMounted, onBeforeUnmount } from 'vue'
import { alertsClient } from '~/stores/alerts'

const alertStore = alertsClient()
const api = useApi()
const config = useRuntimeConfig()

const directions = [
  { key: 'forward', label: 'Ida (sentido positivo)' },
  { key: 'backward', label: 'Vuelta (sentido negativo)' }
]

const groups = ref<any[]>([])
const kimDevices = ref<string[]>([])
const selectedGroupId = ref('')
const calibrations = ref<any[]>([])
const allCalibrations = ref<any[]>([])
const isLoading = ref(false)
const isBusy = ref(false)
const isMoving = ref(false)
const movingLabel = ref('')
const movingElapsed = ref(0)
const copySourceId = ref('')

const form = reactive({
  kimDeviceId: '',
  stepRate: 200,
  stepAcceleration: 100,
  aditionalInfo: ''
})

/**
 * Rangos admitidos por el controlador. El atributo max de HTML solo gobierna las
 * flechas del campo: no impide teclear un número mayor, así que el valor se acota
 * además al salir del campo y se vuelve a comprobar antes de enviar.
 */
const LIMITS = {
  stepRate: { min: 1, max: 500 },
  stepAcceleration: { min: 1, max: 100000 }
} as const

type LimitedField = keyof typeof LIMITS

const inRange = (field: LimitedField) => {
  const value = Number(form[field])
  return Number.isFinite(value) && value >= LIMITS[field].min && value <= LIMITS[field].max
}

const clampField = (field: LimitedField) => {
  const { min, max } = LIMITS[field]
  const value = Number(form[field])

  if (!Number.isFinite(value)) {
    form[field] = min
    return
  }

  const clamped = Math.min(max, Math.max(min, Math.round(value)))
  if (clamped !== value) {
    form[field] = clamped
    alertStore.NewAlert({
      type: 'warning',
      tittle: 'Valor ajustado',
      data: `${field === 'stepRate' ? 'StepRate' : 'StepAcceleration'} admite entre ${min} y ${max}. Se ajustó a ${clamped}.`
    })
  }
}

const plan = reactive({ stepsPerStop: 12000, stopCount: 10 })

/** Lecturas tecleadas, indexadas por eje+sentido+parada. */
const readingInputs = reactive<Record<string, string>>({})

let movingTimer: ReturnType<typeof setInterval> | null = null

const active = computed<any | null>(() => calibrations.value[0] ?? null)

const canCreate = computed(() =>
  !!selectedGroupId.value && !!form.kimDeviceId && inRange('stepRate') && inRange('stepAcceleration'))

/**
 * El plan se bloquea en cuanto hay lecturas: todos los puntos de una travesía tienen que
 * pertenecer a la misma serie, o la regresión mezclaría series distintas.
 */
const planLocked = computed(() =>
  (active.value?.axes || []).some((a: any) => (a.measurements || []).length > 0))

const secondsPerStop = computed(() =>
  Math.round(plan.stepsPerStop / Math.max(active.value?.stepRate || form.stepRate, 1)))

const estimatedMinutes = computed(() =>
  ((plan.stepsPerStop * plan.stopCount) / Math.max(active.value?.stepRate || form.stepRate, 1) / 60).toFixed(1))

/** Con el nominal de 30 nm, solo para dimensionar la travesía frente al recorrido útil. */
const estimatedTravelMm = computed(() =>
  ((plan.stepsPerStop * plan.stopCount * 30) / 1_000_000).toFixed(2))

const copyCandidates = computed(() =>
  allCalibrations.value
    .filter(c => c.groupCailbrationId !== selectedGroupId.value && c.status === 'complete')
    .map(c => ({
      motorCalibrationId: c.motorCalibrationId,
      groupLabel: groups.value.find(g => g.groupCailbrationId === c.groupCailbrationId)?.aditionalInfo || c.groupCailbrationId,
      summary: c.axes?.map((a: any) => `${a.axisName}: ${fmt(a.stepSizeNm)} nm`).join(' · ') || ''
    })))

const fmt = (raw: string | null | undefined) => {
  if (raw === null || raw === undefined || raw === '') return '—'
  const n = Number(raw)
  return Number.isFinite(n) ? n.toFixed(2) : String(raw)
}

const inputKey = (axis: any, direction: string, sequence: number) =>
  `${axis.axisStepCalibrationId}|${direction}|${sequence}`

const readingsFor = (axis: any, direction: string) =>
  (axis.measurements || []).filter((m: any) => m.direction === direction)

/**
 * Posiciones de las paradas. En la ida el motor avanza desde el origen; en la vuelta
 * parte del extremo y regresa, de modo que la posición absoluta decrece mientras el
 * desplazamiento acumulado que se mide crece igual que en la ida.
 */
const stopsFor = (direction: string) => {
  const out: { sequence: number; steps: number; absolute: number }[] = []
  for (let i = 0; i <= plan.stopCount; i++) {
    out.push({
      sequence: i,
      steps: i * plan.stepsPerStop,
      absolute: direction === 'forward'
        ? i * plan.stepsPerStop
        : (plan.stopCount - i) * plan.stepsPerStop
    })
  }
  return out
}

/** Posición absoluta a la que se mueve el motor en una parada dada. */
const absoluteOf = (direction: string, sequence: number) =>
  stopsFor(direction).find(s => s.sequence === sequence)?.absolute ?? 0

const nextStop = (axis: any, direction: string) => {
  const done = readingsFor(axis, direction).map((m: any) => m.sequence)
  for (let i = 0; i <= plan.stopCount; i++) if (!done.includes(i)) return i
  return plan.stopCount
}

const displacementOf = (axis: any, direction: string, sequence: number) => {
  const m = readingsFor(axis, direction).find((x: any) => x.sequence === sequence)
  return m?.displacementMm ? Number(m.displacementMm).toFixed(3) : '—'
}

const loadGroups = async () => {
  try {
    groups.value = (await api.get<any[]>('/api/GroupCalibration')) || []
  } catch (e) {
    console.error('Error al cargar los combos:', e)
  }
}

const loadKimDevices = async () => {
  try {
    kimDevices.value = (await api.get<string[]>('/home/devices')) || []
  } catch (e) {
    console.error('Error al cargar los dispositivos KIM:', e)
  }
}

/** Se cargan también las de otros combos, para poder ofrecer la copia. */
const loadAllCalibrations = async () => {
  const result: any[] = []
  for (const g of groups.value) {
    try {
      const items = await api.get<any[]>(`/api/MotorCalibration/by-group/${g.groupCailbrationId}`)
      result.push(...(items || []))
    } catch { /* un combo sin caracterización no es un error */ }
  }
  allCalibrations.value = result
}

const syncInputs = () => {
  for (const key of Object.keys(readingInputs)) delete readingInputs[key]
  for (const axis of active.value?.axes || []) {
    for (const m of axis.measurements || []) {
      readingInputs[inputKey(axis, m.direction, m.sequence)] = m.caliperReadingMm
    }
  }
}

/** Deduce el plan de las lecturas ya guardadas, para poder retomar la medición. */
const derivePlan = () => {
  for (const axis of active.value?.axes || []) {
    const withSteps = (axis.measurements || []).filter((m: any) => m.sequence > 0)
    if (withSteps.length > 0) {
      plan.stepsPerStop = Math.round(withSteps[0].stepsCommanded / withSteps[0].sequence)
      const maxSeq = Math.max(...(axis.measurements || []).map((m: any) => m.sequence))
      if (maxSeq > plan.stopCount) plan.stopCount = maxSeq
      return
    }
  }
}

const loadCalibrations = async () => {
  if (!selectedGroupId.value) { calibrations.value = []; return }
  isLoading.value = true
  try {
    calibrations.value = (await api.get<any[]>(`/api/MotorCalibration/by-group/${selectedGroupId.value}`)) || []
    derivePlan()
    syncInputs()
  } catch (e: any) {
    console.error('Error al cargar la caracterización:', e)
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: 'No se pudo cargar la medición del motor' })
  } finally {
    isLoading.value = false
  }
}

const createCalibration = async () => {
  // Última barrera: el usuario pudo pegar un valor con el ratón sin que se disparara
  // el evento change del campo.
  clampField('stepRate')
  clampField('stepAcceleration')

  isBusy.value = true
  try {
    await api.post('/api/MotorCalibration', {
      groupCailbrationId: selectedGroupId.value,
      kimDeviceId: form.kimDeviceId,
      stepRate: form.stepRate,
      stepAcceleration: form.stepAcceleration,
      aditionalInfo: form.aditionalInfo || null
    })
    await loadCalibrations()
    await loadAllCalibrations()
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo iniciar la medición' })
  } finally {
    isBusy.value = false
  }
}

const moveTo = async (axis: any, direction: string, sequence: number) => {
  const stop = stopsFor(direction).find(s => s.sequence === sequence)
  if (!stop) return

  isMoving.value = true
  movingLabel.value = direction + axis.axisName
  movingElapsed.value = 0
  movingTimer = setInterval(() => { movingElapsed.value += 1 }, 1000)

  try {
    await api.post(`/api/MotorCalibration/${active.value.motorCalibrationId}/move`, {
      axisName: axis.axisName,
      targetSteps: stop.absolute
    })
    alertStore.NewAlert({
      type: 'OK', tittle: 'Motor en posición',
      data: `Parada ${sequence}. Lea el pie de rey y anote el valor.`
    })
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo mover el motor' })
  } finally {
    isMoving.value = false
    movingLabel.value = ''
    if (movingTimer) { clearInterval(movingTimer); movingTimer = null }
  }
}

const saveReading = async (axis: any, direction: string, stop: { sequence: number; steps: number }) => {
  const raw = readingInputs[inputKey(axis, direction, stop.sequence)]
  if (raw === '' || raw === null || raw === undefined) return

  const value = Number(raw)
  if (!Number.isFinite(value)) {
    alertStore.NewAlert({ type: 'error', tittle: 'Lectura inválida', data: 'Introduzca un número en milímetros.' })
    return
  }

  isBusy.value = true
  try {
    await api.post(`/api/MotorCalibration/${active.value.motorCalibrationId}/measurement`, {
      axisName: axis.axisName,
      direction,
      sequence: stop.sequence,
      stepsCommanded: stop.steps,
      caliperReadingMm: value
    })
    await loadCalibrations()
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo guardar la lectura' })
  } finally {
    isBusy.value = false
  }
}

const accept = async () => {
  isBusy.value = true
  try {
    await api.post(`/api/MotorCalibration/${active.value.motorCalibrationId}/accept`)
    await loadCalibrations()
    alertStore.NewAlert({ type: 'OK', tittle: 'Listo', data: 'Medición marcada como vigente' })
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo marcar como vigente' })
  } finally {
    isBusy.value = false
  }
}

const discard = async () => {
  isBusy.value = true
  try {
    await api.del(`/api/MotorCalibration/${active.value.motorCalibrationId}`)
    await loadCalibrations()
    await loadAllCalibrations()
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo descartar' })
  } finally {
    isBusy.value = false
  }
}

const copyFrom = async () => {
  isBusy.value = true
  try {
    await api.post(`/api/MotorCalibration/${copySourceId.value}/copy-to/${selectedGroupId.value}`)
    copySourceId.value = ''
    await loadCalibrations()
    alertStore.NewAlert({ type: 'OK', tittle: 'Copiada', data: 'Revise el resultado y márquelo como vigente.' })
  } catch (e: any) {
    alertStore.NewAlert({ type: 'error', tittle: 'Error', data: e?.data || 'No se pudo copiar' })
  } finally {
    isBusy.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadGroups(), loadKimDevices()])
  await loadAllCalibrations()
})

onBeforeUnmount(() => {
  if (movingTimer) clearInterval(movingTimer)
})
</script>
