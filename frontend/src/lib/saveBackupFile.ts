/**
 * Guarda JSON no disco local: pede local/nome ao utilizador (File System Access API)
 * ou transfere para a pasta de transferências predefinida.
 */
type SavePickerOptions = {
  suggestedName: string
  types: { description: string; accept: Record<string, string[]> }[]
}

type WindowWithSavePicker = Window & {
  showSaveFilePicker?: (options: SavePickerOptions) => Promise<FileSystemFileHandle>
}

export async function saveBackupJsonToDisk(
  suggestedFileName: string,
  jsonContent: string,
): Promise<'saved' | 'downloaded' | 'cancelled'> {
  const blob = new Blob([jsonContent], { type: 'application/json;charset=utf-8' })

  const showSaveFilePicker = (globalThis as unknown as WindowWithSavePicker).showSaveFilePicker
  if (typeof showSaveFilePicker === 'function') {
    try {
      const handle = await showSaveFilePicker({
        suggestedName: suggestedFileName,
        types: [
          {
            description: 'Ficheiro JSON',
            accept: { 'application/json': ['.json'] },
          },
        ],
      })
      const writable = await handle.createWritable()
      await writable.write(blob)
      await writable.close()
      return 'saved'
    } catch (e) {
      if (e instanceof DOMException && e.name === 'AbortError') return 'cancelled'
      throw e
    }
  }

  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = suggestedFileName
  a.rel = 'noopener'
  a.click()
  URL.revokeObjectURL(url)
  return 'downloaded'
}
