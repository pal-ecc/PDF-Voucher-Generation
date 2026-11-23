using PdfBatch.Api.Models.Print;
using System.Globalization;
using PdfSharpCore;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace PdfBatch.Api.Helpers.Pdf;

// Development demonstration of the supplied ECC template. Allocation and approval values are dummy data.
public sealed class VoucherDocument
{
    public byte[] Render(VoucherData voucher)
    {
        var invoiceFinalizeData = new
        {
            vendorName = voucher.PartyName,
            dueDate = voucher.Date.AddDays(14),
            processingStatus = "Approved (dummy data)",
            updatedBy = "Demo User",
            invoiceComment = "Dummy allocation and approval data. " + voucher.Narration,
            invoiceDate = voucher.Date,
            invoiceNo = voucher.InvoiceNo,
            invoicePeriodFrom = new DateOnly(voucher.Date.Year, voucher.Date.Month, 1),
            invoicePeriodTo = new DateOnly(voucher.Date.Year, voucher.Date.Month,
                DateTime.DaysInMonth(voucher.Date.Year, voucher.Date.Month)),
            invoiceAmount = voucher.Amount,
            invoiceCurrency = voucher.Currency,
            preparedBy = "Demo Preparer"
        };
        var firstAmount = decimal.Round(voucher.Amount * 0.6m, 2, MidpointRounding.AwayFromZero);
        var voucherFinalizeList = new[]
        {
            new Allocation("ECC.001.01", "ENG", "5100", 16m, firstAmount, 0m, "PO-2026-001", "Consulting services"),
            new Allocation("ECC.001.02", "OPS", "5200", 8m, voucher.Amount - firstAmount, 0m, "PO-2026-002", "Office supplies")
        };
        var pcsApprovals = new[] { new Approval("Demo Cost Specialist", voucher.Date) };
        var approvals = new[] { new Approval("Demo Approver", voucher.Date) };
        var apApprovals = new[] { new Approval("Demo AP Approver", voucher.Date) };
        decimal totalVoucherAmount = 0;
        decimal totalVoucherDebitAmount = 0;

        string htmlContent = @"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'/>
<title>Voucher</title>
<style>
  body { margin:18px; font-family:Arial; font-size:12px; color:#222; }
  .pdf-title-row { width:100%; border-collapse:collapse; margin:0; }
  .pdf-title-row td { padding:3px 4px; vertical-align:middle; }
  .pdf-title { margin:0; color:#2978a8; font-size:21px; line-height:1.15; }
  .pdf-logo-cell { text-align:right; padding-top:2px; padding-bottom:2px; }
  .pdf-logo { color:#2978a8; font-size:28px; font-weight:bold; }
  .pdf-header-table { width:100%; border-collapse:collapse; margin-top:8px; table-layout:fixed; }
  .pdf-header-table td { vertical-align:top; padding:2px 4px; border:1px solid #d9d9d9; word-wrap:break-word; line-height:1.25; }
  .pdf-header-label { font-weight:bold; padding-right:4px; }
  .pdf-header-value { font-weight:bold; }
  .pdf-table-layout { width:100%; border-collapse:collapse; table-layout:fixed; margin-top:10px; }
  .pdf-table-layout th { text-align:left; color:#2978a8; font-size:9px; padding:3px 4px; border:1px solid #d9d9d9; word-wrap:break-word; white-space:normal; }
  .pdf-table-layout td { font-size:8.5px; padding:3px 4px; border:1px solid #d9d9d9; word-wrap:break-word; white-space:normal; }
  .pdf-table-layout td.wbs-cell { padding-left:4px; text-indent:0; white-space:nowrap; }
  .pdf-table-layout tr { page-break-inside:avoid; }
  .pdf-bold-font { font-weight:bold; color:#2978a8; }
  .approval-heading { margin-top:14px; margin-bottom:6px; font-weight:bold; font-size:12px; }
  .approval-label { color:#2978a8; font-size:10px; font-weight:bold; }
  .approval-line { margin:0 0 4px 0; font-size:10px; color:#222; page-break-inside:avoid; }
</style>
</head>
<body>
<table class='pdf-title-row'>
  <tr>
    <td style='width:60%;'><h2 class='pdf-title'>ECC Invoice Allocation</h2></td>
    <td style='width:40%;' class='pdf-logo-cell'><span class='pdf-logo'>ECC</span></td>
  </tr>
</table>
<table class='pdf-header-table'>
<tr>
<td style='width:50%; font-weight:bold'>
<div><span class='pdf-header-label'>Voucher No:</span> <span class='pdf-header-value'>" + PdfLayout.Text(voucher.VoucherNo) + @"</span></div>
<div><span class='pdf-header-label'>Vendor:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.vendorName) + @"</span></div>
<div><span class='pdf-header-label'>Due Date:</span> <span class='pdf-header-value'>" + PdfLayout.Date(invoiceFinalizeData.dueDate) + @"</span></div>
<div><span class='pdf-header-label'>Processing Status:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.processingStatus) + @"</span></div>
<div><span class='pdf-header-label'>Updated By:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.updatedBy) + @"</span></div>
<div><span class='pdf-header-label'>Notes:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.invoiceComment) + @"</span></div>
</td>
<td style='width:50%; font-weight:bold'>
<div><span class='pdf-header-label'>Invoice Date:</span> <span class='pdf-header-value'>" + PdfLayout.Date(invoiceFinalizeData.invoiceDate) + @"</span></div>
<div><span class='pdf-header-label'>Invoice No.:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.invoiceNo) + @"</span></div>
<div><span class='pdf-header-label'>Invoice Period:</span> <span class='pdf-header-value'>From: " + PdfLayout.Date(invoiceFinalizeData.invoicePeriodFrom) + " To: " + PdfLayout.Date(invoiceFinalizeData.invoicePeriodTo) + @"</span></div>
<div><span class='pdf-header-label'>Invoice Amount:</span> <span class='pdf-header-value'>" + InvoiceDocument.Money(invoiceFinalizeData.invoiceAmount) + @"</span></div>
<div><span class='pdf-header-label'>Invoice Currency:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.invoiceCurrency) + @"</span></div>
<div><span class='pdf-header-label'>Prepared By:</span> <span class='pdf-header-value'>" + PdfLayout.Text(invoiceFinalizeData.preparedBy) + @"</span></div>
</td>
</tr>
</table>
<table class='pdf-table-layout' style='width:100%;'>
<colgroup>
<col style='width:20%;' /><col style='width:16%;' /><col style='width:8%;' /><col style='width:8%;' />
<col style='width:11%;' /><col style='width:11%;' /><col style='width:8%;' /><col style='width:18%;' />
</colgroup>
<tr>
<th>WBS</th><th>Org</th><th>Account No.</th><th>Subk Labor Hr</th>
<th>Amount To Pay</th><th>Amount Debit</th><th>PO</th><th>Item Desc</th>
</tr>";
        foreach (var voucherFinalize in voucherFinalizeList)
        {
            htmlContent += @"
<tr>
<td class='wbs-cell'>" + PdfLayout.Text(voucherFinalize.Wbs.Trim()) + @"</td>
<td>" + PdfLayout.Text(voucherFinalize.Org) + @"</td>
<td>" + PdfLayout.Text(voucherFinalize.AcctNo) + @"</td>
<td>" + voucherFinalize.WorkingHours.ToString(CultureInfo.InvariantCulture) + @"</td>
<td style='text-align:right;'>" + InvoiceDocument.Money(voucherFinalize.Amt) + @"</td>
<td style='text-align:right;'>" + InvoiceDocument.Money(voucherFinalize.DebitAmt) + @"</td>
<td>" + PdfLayout.Text(voucherFinalize.Po) + @"</td>
<td>" + PdfLayout.Text(voucherFinalize.ItemDescription) + @"</td>
</tr>";
            totalVoucherAmount += voucherFinalize.Amt;
            totalVoucherDebitAmount += voucherFinalize.DebitAmt;
        }
        htmlContent += @"
<tr>
<td></td><td></td><td></td>
<td class='pdf-bold-font' style='text-align:right;'>Total</td>
<td class='pdf-bold-font' style='text-align:right;'>" + InvoiceDocument.Money(totalVoucherAmount) + @"</td>
<td class='pdf-bold-font' style='text-align:right;'>" + InvoiceDocument.Money(totalVoucherDebitAmount) + @"</td>
<td></td><td></td>
</tr>
</table>
<p class='approval-label approval-heading'>Required Approval :</p>";
        foreach (var approval in pcsApprovals)
            htmlContent += ApprovalHtml(approval, "Project Cost Specialist");
        foreach (var approval in approvals)
            htmlContent += ApprovalHtml(approval, "Approver");
        foreach (var approval in apApprovals)
            htmlContent += ApprovalHtml(approval, "AP Approver");
        htmlContent += @"
</body>
</html>";

        var config = new PdfGenerateConfig { PageSize = PageSize.A4 };
        config.SetMargins(20);
        return PdfLayout.Render(htmlContent, config);
    }

    private static string ApprovalHtml(Approval approval, string role) =>
        @"<div class='approval-line'><span class='approval-label'>Approved By:</span> "
        + PdfLayout.Text(approval.Approver) + " [" + PdfLayout.Text(role) + "] on "
        + PdfLayout.Date(approval.Date) + "</div>";

    private sealed record Allocation(string Wbs, string Org, string AcctNo, decimal WorkingHours,
        decimal Amt, decimal DebitAmt, string Po, string ItemDescription);
    private sealed record Approval(string Approver, DateOnly Date);
}
