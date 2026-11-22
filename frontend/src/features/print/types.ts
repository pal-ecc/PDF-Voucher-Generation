export type PrintSelection =
  | { mode: 'today' }
  | { mode: 'voucher'; voucherNo: string }
  | { mode: 'month'; month: number; year: number };

export type PrintPayload = { invoiceId: number } | PrintSelection;

export interface ResponseDto<T> {
  result: { data: T; totalRecords: number | null } | null;
  message: string;
  status: number;
}

export interface PdfDownloadData {
  fileName: string;
  contentType: string;
  contentBase64: string;
}
export type PrintResult = {
  count: number;
  fallback: boolean;
  folderName?: string;
  fileNames: string[];
};

export type PrintProgress = {
  phase: 'choosing' | 'generating' | 'downloading' | 'saving';
  saved: number;
  total?: number;
  folderName?: string;
  fileName?: string;
};

// Kept local because the picker is not included in TypeScript's standard DOM declarations.
export interface PdfWritable {
  write(data: Uint8Array): Promise<void>;
  close(): Promise<void>;
  abort(): Promise<void>;
}

export interface PdfDirectory {
  readonly name: string;
  getFileHandle(name: string, options?: { create: boolean }): Promise<{
    createWritable(): Promise<PdfWritable>;
  }>;
}

export type PdfPickerWindow = Window & {
  showDirectoryPicker?: (options: { id: string; mode: 'readwrite' }) => Promise<PdfDirectory>;
};
