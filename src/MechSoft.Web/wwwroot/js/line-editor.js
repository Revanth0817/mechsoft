// Alpine.js components for the line-item tables shared by Purchase Orders,
// Delivery Challans, Billing (Invoices) and Quotes. Registered as globals so
// x-data="poLineEditor(...)" etc. can reference them before Alpine boots.

function fmtCurrency(n) {
    return '₹ ' + (Number(n) || 0).toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function blankSimpleLine() {
    return { partNo: '', description: '', gstPercent: 18, qty: 1, unit: 'NOS', amount: 0 };
}

/**
 * Shared editor for the "Part No / Description / GST% / Qty / Unit / Amount"
 * line shape used by Purchase Orders, Invoices and Quotes.
 */
function simpleLineEditor(initial) {
    return {
        lines: (initial && initial.length ? initial : [blankSimpleLine()]).map(l => ({
            partNo: l.partNo ?? '',
            description: l.description ?? '',
            gstPercent: l.gstPercent ?? 18,
            qty: l.qty ?? 1,
            unit: l.unit ?? 'NOS',
            amount: l.amount ?? 0,
        })),

        addRow() {
            this.lines.push(blankSimpleLine());
        },
        removeRow(index) {
            this.lines.splice(index, 1);
        },
        lineTotal(line) {
            const amount = Number(line.amount) || 0;
            const gst = Number(line.gstPercent) || 0;
            return amount * (1 + gst / 100);
        },
        grandTotal() {
            return this.lines.reduce((sum, line) => sum + this.lineTotal(line), 0);
        },
        fmt: fmtCurrency,
    };
}

/**
 * Invoice editor (service-invoice layout): each line is Qty x Rate with its own
 * tax %, and the footer adds a flat discount and shipping charge:
 *   Balance Due = Subtotal + Taxes - Discount + Shipping
 */
function invoiceEditor(initial, discount, shipping) {
    const blank = () => ({ partNo: '', description: '', gstPercent: 18, qty: 1, unit: 'NOS', rate: 0 });

    return {
        lines: (initial && initial.length ? initial : [blank()]).map(l => ({
            partNo: l.partNo ?? '',
            description: l.description ?? '',
            gstPercent: l.gstPercent ?? 18,
            qty: l.qty ?? 1,
            unit: l.unit ?? 'NOS',
            rate: l.rate ?? 0,
        })),
        discount: discount ?? 0,
        shipping: shipping ?? 0,

        addRow() {
            this.lines.push(blank());
        },
        removeRow(index) {
            this.lines.splice(index, 1);
        },
        lineAmount(line) {
            return (Number(line.qty) || 0) * (Number(line.rate) || 0);
        },
        lineTax(line) {
            return this.lineAmount(line) * (Number(line.gstPercent) || 0) / 100;
        },
        subtotal() {
            return this.lines.reduce((s, l) => s + this.lineAmount(l), 0);
        },
        taxes() {
            return this.lines.reduce((s, l) => s + this.lineTax(l), 0);
        },
        balanceDue() {
            return this.subtotal() + this.taxes() - (Number(this.discount) || 0) + (Number(this.shipping) || 0);
        },
        fmt: fmtCurrency,
    };
}

/**
 * Editor for Delivery Challans: a Part dropdown (with live stock lookup),
 * GST% / Qty / Unit / Rate, computed Amount/Total, and over-stock detection
 * that blocks Save the same way the original hand-written version did.
 */
function dcLineEditor(initial, parts) {
    const blank = () => ({ partId: parts[0]?.id ?? '', gstPercent: 18, qty: 1, unit: 'NOS', rate: 0 });

    return {
        parts,
        lines: (initial && initial.length ? initial : [blank()]).map(l => ({
            partId: l.partId ?? (parts[0]?.id ?? ''),
            gstPercent: l.gstPercent ?? 18,
            qty: l.qty ?? 1,
            unit: l.unit ?? 'NOS',
            rate: l.rate ?? 0,
        })),

        addRow() {
            this.lines.push(blank());
        },
        removeRow(index) {
            this.lines.splice(index, 1);
        },
        stockFor(partId) {
            return this.parts.find(p => p.id === partId)?.stock ?? 0;
        },
        isOverage(line) {
            return (Number(line.qty) || 0) > this.stockFor(line.partId);
        },
        hasOverage() {
            return this.lines.some(l => this.isOverage(l));
        },
        lineAmount(line) {
            return (Number(line.qty) || 0) * (Number(line.rate) || 0);
        },
        lineTotal(line) {
            const gst = Number(line.gstPercent) || 0;
            return this.lineAmount(line) * (1 + gst / 100);
        },
        grandTotal() {
            return this.lines.reduce((sum, line) => sum + this.lineTotal(line), 0);
        },
        subtotal() {
            return this.lines.reduce((sum, line) => sum + this.lineAmount(line), 0);
        },
        taxes() {
            return this.grandTotal() - this.subtotal();
        },
        fmt: fmtCurrency,
    };
}
