import { useMutation } from 'react-query';
import type { UseMutationOptions } from 'react-query';
import { postWithPathQuery } from '../../core';
import { PdfApiConstants } from '../../constants/web-resource-key-constants';
import type { PdfDownloadData, PrintPayload, ResponseDto } from './types';

export async function generatePdfBatch(data: PrintPayload): Promise<PdfDownloadData> {
  const endpoint = 'invoiceId' in data ? PdfApiConstants.PDF_BATCH : PdfApiConstants.PDF_BATCH_SELECTION;
  const body = await postWithPathQuery<ResponseDto<PdfDownloadData> | null>(endpoint, undefined, data);
  if (typeof body?.status === 'number' && body.status !== 200) {
    throw new Error(body.message || `PDF generation failed (${body.status})`);
  }
  const download = body?.result?.data;
  if (body?.status !== 200 || typeof download?.fileName !== 'string' ||
      download.contentType !== 'application/zip' || typeof download.contentBase64 !== 'string' || !download.contentBase64) {
    throw new Error('PDF generation returned an invalid response. Please try again.');
  }
  return download;
}

type MutationOptions = UseMutationOptions<PdfDownloadData, Error, PrintPayload>;

export function usePdfBatchMutation(
  successHandler?: MutationOptions['onSuccess'],
  errorHandler?: MutationOptions['onError'],
) {
  return useMutation<PdfDownloadData, Error, PrintPayload>(
    ['PdfBatchAdd'],
    generatePdfBatch,
    { onSuccess: successHandler, onError: errorHandler, retry: false },
  );
}
