import { http } from './http'

/**
 * Downloads a protected file and hands it to the browser.
 *
 * A plain `<a href>` cannot be used: every content route requires the bearer token, and an
 * anchor sends no `Authorization` header. Putting the token in the query string instead would
 * write it into browser history, proxy logs and the `Referer` of anything the page later loads.
 * So the file is fetched with the normal axios instance — which already attaches the token and
 * refreshes it on 401 — and then handed over as an object URL.
 *
 * The server sets `Content-Disposition`, so the name it chose is preferred over anything the
 * caller guesses.
 */
export async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const response = await http.get<Blob>(path, { responseType: 'blob' })

  const name = fileNameFrom(response.headers['content-disposition']) ?? fallbackName
  const url = URL.createObjectURL(response.data)

  try {
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = name
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    // Revoking immediately would race the download in some browsers; a tick is enough.
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
}

/**
 * Reads the file name out of a `Content-Disposition` header.
 *
 * `filename*` (RFC 5987) is preferred when present because it survives non-ASCII names — a
 * Turkish file name loses its accents in the plain `filename` parameter.
 */
function fileNameFrom(header: unknown): string | null {
  if (typeof header !== 'string') return null

  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header)
  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1])
    } catch {
      // A malformed header is not worth failing the download over.
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(header)
  return plain?.[1]?.trim() || null
}
