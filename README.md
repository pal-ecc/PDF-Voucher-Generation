# PDF workspace

.NET 10 controller API using PdfSharpCore/MigraDocCore, with a React 17 + TypeScript + Create React App frontend and the requested KendoReact 5 packages. This workspace was initially empty; no existing database or application conventions were available. npm is used with an included lockfile. No test projects are included.

## Run locally

Requirements: .NET 10 SDK, Node.js and npm. This implementation was built with Node 24.14.1; the requested CRA toolchain reports Node deprecation warnings.

From the repository root:

```powershell
dotnet restore PdfBatch.slnx
dotnet run --project backend/PdfBatch.Api --launch-profile http
```

In another terminal:

```powershell
cd frontend
npm ci
npm start
```

Open http://localhost:3000. CRA proxies `/api` to http://localhost:5080 using `frontend/package.json`. No CORS configuration is needed. The environment groups used by the supplied scripts are in `frontend/.env-cmdrc.json`. `npm run start:prod` also starts a development server; use `npm run build` and serve the `build` directory for deployment.

Build checks:

```powershell
dotnet build PdfBatch.slnx
cd frontend
npm run typecheck
npm run build
```

## Controller and generator service

`PdfController` receives a scoped `IPdfGeneratorService` through constructor injection. `PdfGeneratorService` mirrors the three application entry points in the supplied service example:

| Selection | Service method |
| --- | --- |
| Today | `GeneratePdfForNewVouchersAsync(CancellationToken)` |
| Voucher number | `GeneratePdfForGivenVoucherAsync(string, CancellationToken)` |
| Month/year | `GeneratePdfVoucherForGivenMonthYearAsync(int, int, CancellationToken)` |

The controller validates requests and returns `Task<ResponseDto>` using the application's `ResultDto`/`ResponseDto` convention. The service loads invoice data through `IInvoiceRepository`, creates both documents, logs invoice ids only, and builds a ZIP with sanitized, unique entry names. Generation errors propagate instead of being swallowed or reported as success. Data access and ZIP entry writes are asynchronous, with cancellation passed through. Document rendering is synchronous CPU work; cancellation is observed between renders, not during an individual render.

The same scoped implementation is registered as `IPdfBatchService` to preserve the original `CreateZipAsync(PrintRequest, CancellationToken)` contract. Single-invoice export remains available through the API and the reusable `PrintButton` component.

The supplied example referenced ERP-specific read services, a database connection, an attachment store, and persistent retry/audit records. Those do not exist in this workspace. They were not fabricated: `DevelopmentInvoiceRepository` remains an explicit in-memory stub. Today's method includes all invoices dated today, including previously exported ones; it does not claim to track new/unprocessed vouchers. The current PDF templates use MigraDocCore document objects rather than copying the sample's incomplete ERP HTML and unavailable data fields. Attachment merging and persistent failed-generation retries are not implemented.

## API and sample data

- `POST /api/pdf/batch` with `{ "invoiceId": 1 }` exports one invoice and its voucher.
- `POST /api/pdf/batch/selection` with `{ "mode": "today" }` exports today's matches.
- The selection endpoint accepts `{ "mode": "voucher", "voucherNo": "V-1001" }`.
- It also accepts `{ "mode": "month", "month": 11, "year": 2026 }`.

Endpoints return `application/json`. Success wraps a ZIP containing `Invoice-{invoiceNo}.pdf` and `Voucher-{invoiceNo}.pdf` for each matching invoice:

```json
{
  "result": {
    "data": {
      "fileName": "documents-20261122-120000.zip",
      "contentType": "application/zip",
      "contentBase64": "<Base64 ZIP bytes>"
    },
    "totalRecords": 2
  },
  "message": "PDFs generated successfully.",
  "status": 200
}
```

`TotalRecords` counts PDF files, not invoices. The HTTP status and envelope `Status` match. Invalid inputs return 400, no matches return 404, and unhandled generation failures return a safe 500 message, all using `ResponseDto`. Automatic model-binding/validation failures also use this envelope, with field errors in `Result.Data`. Responses use ASP.NET Core's default camelCase JSON names. There are no shared mutable response fields in the controller.

The frontend decodes `Result.Data.ContentBase64` to ZIP bytes before extracting PDFs or starting the fallback ZIP download, and displays errors from `Message`. JSON Base64 adds roughly 33% to ZIP size and increases peak memory use. This replaces the earlier binary ZIP response contract; external callers must parse the envelope too. ZIP download filenames use UTC timestamps. Today uses the API server's local date.

Sample invoice ids 1 and 2 are dated today; id 3 is dated one month earlier. Invoice numbers are 1001–1003; voucher numbers are V-1001–V-1003. Voucher amounts equal the full invoice total, and payment method is unspecified because payment data is unavailable. Money uses decimal and tax rounds away from zero. `IAmountInWordsFormatter` can be replaced for lakh/crore grouping.

The API refuses startup outside Development until the stub is replaced. Before deploying, register real authorized data access, scope lookups to the current user/tenant, and deploy the frontend/API behind the same origin. No database schema or migrations were added.

## Frontend

The screen has three Kendo tabs: Today, Voucher number, and Month & year. Sample voucher buttons and This month/Last month shortcuts update the current selection. A summary shows what will be exported. Filters and tab changes are disabled during an export.

The package versions supplied by the user are retained, with `fflate` added for ZIP extraction. TypeScript is strict. React 17 has no `useTransition`, so the export action uses explicit pending state and a synchronous duplicate-click guard. Kendo components are used for tabs, input, dropdowns and determinate progress; unused requested packages do not add unrelated application features.

`printToFolder` opens `showDirectoryPicker({ id: 'pdf-export', mode: 'readwrite' })` before any network request. It reports choosing/generating/saving stages, increments counts only after files close, and displays the destination and actual saved filenames. Partial failures report completed files. Picker cancellation is silent.

Existing names are preserved: duplicates receive `(2)`, `(3)`, etc. Path segments are stripped from ZIP entries and non-PDF/directory entries are skipped. Web Locks serialize this app's exports across same-origin tabs. Browser filesystem APIs have no atomic create-if-absent operation; other applications can still modify the destination concurrently. Exports are not transactional and a failed new file can leave an empty placeholder.

Chrome/Edge support folder selection on localhost/HTTPS. Protected folders may be blocked; choose a subfolder. Firefox/Safari download the ZIP instead, and the browser controls download naming. The UI reports that a download started because it cannot confirm a completed browser download.

## Dependencies and deployment limitations

- QuestPDF has been removed. PdfSharpCore and MigraDocCore are version 1.3.67.
- PdfSharpCore's original ImageSharp dependency has known vulnerabilities. ImageSharp 3.2.0 is explicitly referenced to pass NuGet's vulnerability audit without disabling checks. The templates are text/table only. Legacy PdfSharpCore image-loading APIs need an adapter before adding logos or other images with this major-version override.
- The installed ImageSharp package reports that Debug can continue without a license but Release builds require a valid SixLabors license. Configure the vendor-supported `SixLaborsLicenseKey`/`SixLaborsLicenseFile` externally; no credentials or license keys are stored here. Its warning has not been suppressed.
- KendoReact components require appropriate Telerik licensing. Supply your own license through the vendor-supported activation workflow. License files are ignored by Git.
- The user-requested frontend dependency tree reports 59 npm audit findings: 9 low, 6 moderate and 44 high. Exact requested versions were retained; no forced breaking upgrades were applied. Review before production use.
- CRA's production CSS optimizer reports an SVG parser warning for encoded SVGs in the requested Kendo theme. The build completes; the theme was visually checked in the development browser.
- ZIP/PDF/Base64 buffers live in memory and `unzipSync` runs on the browser main thread. Large exports need limits or background processing.
- The default font resolver uses installed fonts. For consistent deployment, configure `PdfFonts__RegularPath` and `PdfFonts__BoldPath` with licensed TrueType font paths. Both must be provided together. Non-Latin content requires suitable glyph coverage and shaping support.

## Verification

- Backend Debug build succeeds; ImageSharp reports a license warning. Release licensing remains unresolved.
- Live checks cover the ResponseDto success/error contract, all three service selections, the single-invoice endpoint, model validation, and malformed JSON. Frontend smoke checks use actual API envelopes for ZIP decoding, numbered file copies, saving progress, partial failure, picker cancellation, and the fallback download.
- `npm run typecheck` succeeds. `npm run build` succeeds with the Kendo-theme CSS warning described above.
- PdfSharpCore generated a ZIP containing both PDFs. `pdfinfo` confirmed A4 invoice and A5 landscape voucher; both were rendered with `pdftoppm` and visually inspected.
- Browser checks confirmed three tabs, disabled export for an empty voucher number, sample voucher selection, Last month selection, selection summaries, and visible choosing-folder feedback.
- Native folder saving/overwriting protection and Firefox ZIP download still require manual acceptance checks. Repeat an export into the same folder and confirm numbered copies and unchanged originals; try an invalid voucher and confirm inline feedback.

Official references: [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore), [Kendo TabStrip](https://www.telerik.com/kendo-react-ui/components/layout/tabstrip/tabs), [directory picker](https://developer.mozilla.org/en-US/docs/Web/API/Window/showDirectoryPicker), and [fflate](https://github.com/101arrowz/fflate). Context7 tools were unavailable in the implementation session.
