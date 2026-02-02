import { defineStore } from 'pinia'

export interface CapturePair {
  id: number
  images: string[]
}

export const useCapturesStore = defineStore('capturesStore', () => {
  const maxPerPair = 2
  const pairs = ref<CapturePair[]>([])

  const addImage = (dataUrl: string) => {
    let activePair = pairs.value[pairs.value.length - 1]
    if (!activePair || activePair.images.length >= maxPerPair) {
      activePair = { id: Date.now(), images: [] }
      pairs.value.push(activePair)
    }
    activePair.images.push(dataUrl)
  }

  const removeImage = (pairIndex: number, imageIndex: number) => {
    const pair = pairs.value[pairIndex]
    if (!pair) {
      return
    }

    pair.images.splice(imageIndex, 1)
    if (pair.images.length === 0) {
      pairs.value.splice(pairIndex, 1)
    }
  }

  const clearAll = () => {
    pairs.value = []
  }

  return {
    maxPerPair,
    pairs,
    addImage,
    removeImage,
    clearAll
  }
})
