import { useRef, useState } from 'react';
import { ProgressBar } from '@progress/kendo-react-progressbars';
import { PdfSaveError, printToFolder } from './printToFolder';
import type { PrintProgress, PrintSelection } from './types';

type PrintButtonProps = ({ invoiceId: number; selection?: never } | { invoiceId?: never; selection: PrintSelection }) & { disabled?: boolean; onBusyChange?: (busy: boolean) => void };

export function PrintButton({ invoiceId, selection, disabled = false, onBusyChange }: PrintButtonProps) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [progress, setProgress] = useState<PrintProgress | null>(null);
  const [outcome, setOutcome] = useState<'success' | 'error' | null>(null);
  const [savedFiles, setSavedFiles] = useState<string[]>([]);
  const running = useRef(false);

  const stageText = progress?.phase === 'choosing' ? 'Choose a folder to save your PDFs…'
    : progress?.phase === 'generating' ? 'Generating your PDFs. Please keep this page open…'
    : progress?.phase === 'downloading' ? 'Preparing your ZIP download…'
    : progress ? `Saving ${progress.saved} of ${progress.total} PDFs to “${progress.folderName}”…`
    : '';

  async function handleClick() {
    if (running.current) return;
    running.current = true;
    setBusy(true);
    onBusyChange?.(true);
    setMessage('');
    setOutcome(null);
    setSavedFiles([]);
    try {
      // No await before this call: printToFolder opens the picker synchronously up to its first await.
      const result = await printToFolder(selection ?? { invoiceId: invoiceId! }, setProgress);
      if (result) {
        setOutcome('success');
        setSavedFiles(result.fileNames);
        setMessage(result.fallback
          ? 'ZIP download started. Check your browser’s downloads to find it.'
          : `Saved ${result.count} PDF ${result.count === 1 ? 'file' : 'files'} to “${result.folderName}”. Your files are ready.`);
      }
    } catch (error) {
      setOutcome('error');
      if (error instanceof PdfSaveError) {
        setSavedFiles(error.savedFiles);
        setMessage(`${error.message} ${error.savedFiles.length} PDF files were saved to “${error.folderName}” before it stopped. You can retry; existing files will be kept.`);
      } else {
        setMessage(`${error instanceof Error ? error.message : 'PDF export failed.'} No PDF files were saved by this export. Please try again.`);
      }
    } finally {
      setProgress(null);
      setBusy(false);
      onBusyChange?.(false);
      running.current = false;
    }
  }

  return (
    <div className="print-action" aria-busy={busy}>
      <button className="primary-button" type="button" disabled={disabled || busy} onClick={handleClick}>
        {busy && <span className="spinner" aria-hidden="true" />}
        {progress?.phase === 'saving' ? 'Saving PDFs…' : progress?.phase === 'choosing' ? 'Choosing folder…' : busy ? 'Generating…' : 'Print PDFs'}
      </button>
      <p className="hint">You’ll choose a folder first. Use a subfolder if your browser blocks Downloads or Desktop. On browsers without folder access, a ZIP download starts instead.</p>
      <div role="status" aria-live="polite" aria-atomic="true" className={`export-status ${outcome ?? (progress ? 'working' : '')}`}>
        {(stageText || message) && <p>{outcome === 'success' ? '✓ ' : outcome === 'error' ? 'Export stopped: ' : ''}{stageText || message}</p>}
        {progress?.total ? <div aria-label="PDF export progress"><ProgressBar min={0} max={progress.total} value={progress.saved} /></div> : progress ? <div className="indeterminate-track" aria-label="Preparing PDFs"><span /></div> : null}
        {progress?.phase === 'saving' && progress.fileName && <p className="export-file">{progress.fileName}</p>}
      </div>
      {savedFiles.length > 0 && <details className="saved-files">
        <summary>View saved files ({savedFiles.length})</summary>
        <ul>{savedFiles.map(name => <li key={name}>{name}</li>)}</ul>
      </details>}
    </div>
  );
}
