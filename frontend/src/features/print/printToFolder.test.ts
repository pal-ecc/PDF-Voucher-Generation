import { printToFolder } from './printToFolder';
import type { PdfDirectory, PdfPickerWindow } from './types';

const pickerWindow = window as PdfPickerWindow;
const originalPicker = pickerWindow.showDirectoryPicker;

afterEach(() => {
  pickerWindow.showDirectoryPicker = originalPicker;
});

it('opens the folder picker synchronously and waits before generating PDFs', async () => {
  let cancelPicker!: (error: Error) => void;
  const picker = jest.fn(() => new Promise<PdfDirectory>((_, reject) => { cancelPicker = reject; }));
  pickerWindow.showDirectoryPicker = picker;
  const generate = jest.fn();
  const result = printToFolder({ mode: 'today' }, generate);
  expect(picker).toHaveBeenCalledTimes(1);
  expect(generate).not.toHaveBeenCalled();
  cancelPicker(new DOMException('Cancelled', 'AbortError'));
  await expect(result).resolves.toBeNull();
  expect(generate).not.toHaveBeenCalled();
});

it('passes the selection to the mutation after the folder is chosen and propagates API errors', async () => {
  const directory = { name: 'Exports', getFileHandle: jest.fn() };
  pickerWindow.showDirectoryPicker = jest.fn().mockResolvedValue(directory);
  const generate = jest.fn().mockRejectedValue(new Error('No invoices found.'));
  const payload = { mode: 'voucher', voucherNo: 'V-1001' } as const;
  await expect(printToFolder(payload, generate)).rejects.toThrow('No invoices found.');
  expect(generate).toHaveBeenCalledWith(payload);
  expect(directory.getFileHandle).not.toHaveBeenCalled();
});
