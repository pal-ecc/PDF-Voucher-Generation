import { useState } from 'react';
import { TabStrip, TabStripTab } from '@progress/kendo-react-layout';
import { Input } from '@progress/kendo-react-inputs';
import { DropDownList } from '@progress/kendo-react-dropdowns';
import { PrintButton } from './features/print/PrintButton';
import type { PrintSelection } from './features/print/types';

const months = Array.from({ length: 12 }, (_, index) => ({
  text: new Intl.DateTimeFormat('en', { month: 'long' }).format(new Date(2000, index, 1)),
  value: index + 1,
}));

export default function App() {
  const [today] = useState(() => new Date());
  const [selected, setSelected] = useState(0);
  const [voucherNo, setVoucherNo] = useState('');
  const [month, setMonth] = useState(months[today.getMonth()]);
  const [year, setYear] = useState(today.getFullYear());
  const [busy, setBusy] = useState(false);
  const years = Array.from({ length: 11 }, (_, i) => today.getFullYear() - 5 + i);
  const selection: PrintSelection = selected === 0 ? { mode: 'today' }
    : selected === 1 ? { mode: 'voucher', voucherNo: voucherNo.trim() }
    : { mode: 'month', month: month.value, year };
  const summary = selected === 0 ? 'All invoices dated today'
    : selected === 1 ? (voucherNo.trim() ? `Voucher ${voucherNo.trim()}` : 'Enter a voucher number above')
    : `${month.text} ${year}`;

  return (
    <div className="workspace">
      <header className="topbar">
        <a className="brand" href="/" aria-label="PDF workspace home"><span className="brand-icon" aria-hidden="true">P</span> PDF workspace</a>
        <span className="privacy-badge"><span aria-hidden="true">●</span> Saved to your device</span>
      </header>
      <main>
        <div className="intro">
          <div><p className="eyebrow">DOCUMENT EXPORT</p><h1>Your documents.<br /><span>Ready in a few clicks.</span></h1>
            <p className="intro-copy">Choose the invoices you need. We’ll prepare an invoice and a payment voucher for each one.</p></div>
          <div className="document-art" aria-hidden="true"><div className="paper back"><b>VOUCHER</b><i /><i /><i /></div><div className="paper front"><span>PDF</span><b>INVOICE</b><i /><i /><i /><strong>✓</strong></div></div>
        </div>

        <ol className="steps" aria-label="How to export"><li><span>1</span><div><b>Choose your invoices</b><small>Today, voucher, or month</small></div></li><li><span>2</span><div><b>Pick a folder</b><small>You choose where files go</small></div></li><li><span>3</span><div><b>Save your PDFs</b><small>Follow the progress below</small></div></li></ol>

        <section className="export-card" aria-labelledby="export-heading">
          <div className="card-heading"><div><p className="eyebrow">LET’S GET STARTED</p><h2 id="export-heading">Which invoices do you need?</h2></div><span className="format-badge">PDF documents</span></div>
          <TabStrip selected={selected} onSelect={event => { if (!busy) setSelected(event.selected); }} animation={false}>
            <TabStripTab title="Today" disabled={busy && selected !== 0}>
              <div className="tab-body"><div className="tab-icon" aria-hidden="true">☀</div><div className="tab-copy"><h3>Everything from today</h3><p>Export all invoices dated today and their payment vouchers in one go.</p><span className="date-pill">{today.toLocaleDateString('en', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}</span><p className="field-help">Invoice dates are matched using the server’s local date.</p></div></div>
            </TabStripTab>
            <TabStripTab title="Voucher number" disabled={busy && selected !== 1}>
              <div className="tab-body"><div className="tab-icon" aria-hidden="true">#</div><div className="tab-copy"><h3>Find a specific voucher</h3><p>Enter the voucher number to save its invoice and payment voucher.</p>
                <label className="field-label" htmlFor="voucher-number">Voucher number</label>
                <Input id="voucher-number" value={voucherNo} onChange={event => setVoucherNo(String(event.value ?? ''))} maxLength={100} placeholder="e.g. V-1001" disabled={busy} aria-describedby="voucher-help" />
                <p id="voucher-help" className="field-help">Use the full voucher number shown on your document.</p>
                <div className="quick-options"><span>Try a sample:</span>{['V-1001', 'V-1002', 'V-1003'].map(value => <button key={value} className="sample-chip" disabled={busy} onClick={() => setVoucherNo(value)}>{value}</button>)}</div>
              </div></div>
            </TabStripTab>
            <TabStripTab title="Month & year" disabled={busy && selected !== 2}>
              <div className="tab-body"><div className="tab-icon" aria-hidden="true">▦</div><div className="tab-copy"><h3>A month of documents, together</h3><p>Select a month and year. We’ll include every matching invoice and voucher.</p>
                <div className="filters"><div><label className="field-label" htmlFor="export-month">Month</label><DropDownList id="export-month" data={months} textField="text" dataItemKey="value" value={month} onChange={event => setMonth(event.value)} disabled={busy} /></div><div><label className="field-label" htmlFor="export-year">Year</label><DropDownList id="export-year" data={years} value={year} onChange={event => setYear(event.value)} disabled={busy} /></div></div>
                <div className="quick-options"><span>Quick select:</span><button className="sample-chip" disabled={busy} onClick={() => { setMonth(months[today.getMonth()]); setYear(today.getFullYear()); }}>This month</button><button className="sample-chip" disabled={busy} onClick={() => { const previous = new Date(today.getFullYear(), today.getMonth() - 1, 1); setMonth(months[previous.getMonth()]); setYear(previous.getFullYear()); }}>Last month</button></div>
              </div></div>
            </TabStripTab>
          </TabStrip>

          <div className="export-bottom"><div className="selection-summary"><div><span className="eyebrow">YOUR SELECTION</span><strong>{summary}</strong></div><span className="document-count">Invoice + Voucher <small>2 PDFs per invoice</small></span></div>
            <PrintButton key={JSON.stringify(selection)} selection={selection} disabled={selected === 1 && !voucherNo.trim()} onBusyChange={setBusy} />
          </div>
        </section>
        <div className="reassurance"><span aria-hidden="true">✓</span><p><b>Your existing files stay safe.</b> If a name already exists, we save a numbered copy.</p></div>
        <details className="sample-data"><summary>Using the development sample?</summary><p>V-1001 and V-1002 are dated today. V-1003 is dated last month. Select a sample voucher or use “Last month” to try each export option.</p></details>
        <footer className="page-footer">Invoice & voucher export · PDF workspace</footer>
      </main>
    </div>
  );
}
