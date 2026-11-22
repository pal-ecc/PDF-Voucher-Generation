import { unzipSync } from 'fflate';
import type { PdfDirectory, PdfPickerWindow, PrintPayload, PrintProgress, PrintResult, PdfDownloadData, ResponseDto } from './types';

export class PdfSaveError extends Error {
  readonly savedFiles: string[];
  readonly folderName: string;

  constructor(message: string, savedFiles: string[], folderName: string) {
    super(message);
    this.name = 'PdfSaveError';
    this.savedFiles = savedFiles;
    this.folderName = folderName;
  }
}

function hasErrorName(error: unknown, name: string): boolean {
  return typeof error === 'object' && error !== null && 'name' in error && (error as { name: unknown }).name === name;
}

async function availableName(directory: PdfDirectory, original: string): Promise<string> {
  const stem = original.slice(0, -4);
  const extension = original.slice(-4);
  for (let number = 1; ; number++) {
    const candidate = number === 1 ? original : `${stem} (${number})${extension}`;
    try {
      // No create option: probing must never open an existing file for writing.
      await directory.getFileHandle(candidate);
    } catch (error) {
      if (hasErrorName(error, 'NotFoundError')) return candidate;
      // A directory with this name is also a collision.
      if (!hasErrorName(error, 'TypeMismatchError')) throw error;
    }
  }
}

/** Called directly during the click action: the picker must precede every await/network call.
 * Returns null on picker cancellation. Progress is reported only for actual work stages.
 */
export async function printToFolder(
  payload: PrintPayload,
  onProgress: (progress: PrintProgress) => void = () => {},
): Promise<PrintResult | null> {
  let directory: PdfDirectory | undefined;
  const pickerWindow = window as PdfPickerWindow;
  if (typeof pickerWindow.showDirectoryPicker === 'function') {
    try {
      onProgress({ phase: 'choosing', saved: 0 });
      directory = await pickerWindow.showDirectoryPicker({ id: 'pdf-export', mode: 'readwrite' });
    } catch (error) {
      if (hasErrorName(error, 'AbortError')) return null;
      throw error;
    }
  }

  onProgress({ phase: 'generating', saved: 0, folderName: directory?.name });
  const endpoint = Object.prototype.hasOwnProperty.call(payload, 'invoiceId') ? '/api/pdf/batch' : '/api/pdf/batch/selection';
  const response = await fetch(endpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  const body: (ResponseDto<PdfDownloadData> & { title?: unknown; detail?: unknown }) | null = await response.json().catch(() => null);
  if (!response.ok || (typeof body?.status === 'number' && body.status !== 200)) {
    const problemMessage = [body?.title, body?.detail].filter(value => typeof value === 'string' && value.trim()).join(': ');
    const message = typeof body?.message === 'string' && body.message.trim() ? body.message : problemMessage;
    throw new Error(message || `PDF generation failed (${response.status})`);
  }

  const download = body?.result?.data;
  if (body?.status !== 200 || typeof download?.fileName !== 'string' ||
      download.contentType !== 'application/zip' || typeof download.contentBase64 !== 'string' || !download.contentBase64) {
    throw new Error('PDF generation returned an invalid response. Please try again.');
  }
  let zipBytes: Uint8Array;
  try {
    zipBytes = Uint8Array.from(atob(download.contentBase64), character => character.charCodeAt(0));
  } catch {
    throw new Error('The returned ZIP could not be read. Please try again.');
  }

  if (!directory) {
    onProgress({ phase: 'downloading', saved: 0 });
    const url = URL.createObjectURL(new Blob([zipBytes], { type: download.contentType }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = download.fileName.split(/[/\\]/).pop() || 'documents.zip';
    document.body.append(anchor);
    try {
      anchor.click();
    } finally {
      anchor.remove();
      // Give the browser time to start consuming the download before revoking its URL.
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    }
    return { count: 0, fallback: true, fileNames: [] };
  }

  const entries = unzipSync(zipBytes);
  const pdfs: [string, Uint8Array][] = [];
  for (const [entryName, bytes] of Object.entries(entries)) {
    const name = entryName.split(/[/\\]/).pop();
    if (!name || name === '.' || name === '..' || !name.toLowerCase().endsWith('.pdf')) continue;
    pdfs.push([name, bytes]);
  }
  if (pdfs.length === 0) throw new Error('No PDF files were returned. Nothing was saved.');

  const target = directory;
  const fileNames: string[] = [];
  async function saveFiles(): Promise<PrintResult> {
    try {
      for (const [original, bytes] of pdfs) {
        const name = await availableName(target, original);
        onProgress({ phase: 'saving', saved: fileNames.length, total: pdfs.length, folderName: target.name, fileName: name });
        const file = await target.getFileHandle(name, { create: true });
        const writable = await file.createWritable();
        try {
          await writable.write(bytes);
          await writable.close();
        } catch (error) {
          await writable.abort().catch(() => {});
          throw error;
        }
        // Count only files whose writable has successfully closed.
        fileNames.push(name);
        onProgress({ phase: 'saving', saved: fileNames.length, total: pdfs.length, folderName: target.name, fileName: name });
      }
    } catch (error) {
      const reason = error instanceof Error ? error.message : 'Check folder permissions and available disk space.';
      throw new PdfSaveError(`Could not finish saving PDFs. ${reason}`, [...fileNames], target.name);
    }
    return { count: fileNames.length, fallback: false, folderName: target.name, fileNames };
  }

  onProgress({ phase: 'saving', saved: 0, total: pdfs.length, folderName: target.name });
  // Serialize exports across this app's tabs/buttons so their name checks cannot race.
  // Browsers do not offer an atomic create-if-absent API; other apps can still modify the folder.
  const locks = (navigator as Navigator & { locks?: { request<T>(name: string, callback: () => Promise<T>): Promise<T> } }).locks;
  return locks
    ? locks.request('pdf-export-save', saveFiles)
    : saveFiles();
}
