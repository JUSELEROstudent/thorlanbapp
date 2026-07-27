/**
 * Acceso centralizado al backend.
 *
 * Antes cada componente resolvía por su cuenta `useRuntimeConfig().public.apiUrl`
 * y decidía si mandaba o no el token: algunas llamadas usaban $fetch sin
 * autenticación y otras fetch crudo con el Bearer puesto a mano. Eso significa
 * que el día que se active [Authorize] en los controladores, la mitad de la
 * aplicación deja de funcionar sin una causa evidente.
 *
 * Este composable unifica ambas cosas. Los componentes nuevos lo usan desde el
 * principio; los existentes se pueden ir migrando cuando se toquen por otra razón.
 */
export const useApi = () => {
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiUrl as string

  const token = (): string | null => {
    if (typeof window === 'undefined') return null
    return localStorage.getItem('stringjwt')
  }

  const buildHeaders = (body: unknown, extra?: Record<string, string>) => {
    const headers: Record<string, string> = { ...(extra || {}) }

    const jwt = token()
    if (jwt && !headers.Authorization) {
      headers.Authorization = `Bearer ${jwt}`
    }

    // FormData debe llevar su propio boundary: fijar Content-Type a mano
    // rompe la subida de archivos.
    const isFormData = typeof FormData !== 'undefined' && body instanceof FormData
    if (body !== undefined && body !== null && !isFormData && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json'
    }

    return headers
  }

  const request = <T = unknown>(path: string, options: Record<string, any> = {}): Promise<T> => {
    const url = path.startsWith('http') ? path : `${baseUrl}${path}`
    return $fetch<T>(url, {
      ...options,
      headers: buildHeaders(options.body, options.headers)
    })
  }

  const get = <T = unknown>(path: string, options: Record<string, any> = {}) =>
    request<T>(path, { ...options, method: 'GET' })

  const post = <T = unknown>(path: string, body?: unknown, options: Record<string, any> = {}) =>
    request<T>(path, { ...options, method: 'POST', body })

  const put = <T = unknown>(path: string, body?: unknown, options: Record<string, any> = {}) =>
    request<T>(path, { ...options, method: 'PUT', body })

  const del = <T = unknown>(path: string, options: Record<string, any> = {}) =>
    request<T>(path, { ...options, method: 'DELETE' })

  /**
   * Petición cruda con fetch nativo, para respuestas binarias (imágenes) donde
   * hace falta el Blob y no el parseo automático de $fetch.
   */
  const raw = (path: string, options: RequestInit = {}): Promise<Response> => {
    const url = path.startsWith('http') ? path : `${baseUrl}${path}`
    const headers: Record<string, string> = { ...((options.headers as Record<string, string>) || {}) }
    const jwt = token()
    if (jwt && !headers.Authorization) headers.Authorization = `Bearer ${jwt}`
    return fetch(url, { ...options, headers })
  }

  return { baseUrl, request, get, post, put, del, raw }
}
