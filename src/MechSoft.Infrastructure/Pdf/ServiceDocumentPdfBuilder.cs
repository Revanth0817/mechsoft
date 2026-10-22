using System.Globalization;
using MechSoft.Application.Common;
using MechSoft.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MechSoft.Infrastructure.Pdf;

public record ExtraParty(string Title, string Company, string Name, string Address, string Contact);

public record ServiceDocLine(string PartNo, string Description, decimal Rate, decimal Qty, string Unit, decimal Gst, decimal Amount);

/// <summary>Neutral shape so invoices and delivery challans share one layout.</summary>
public record ServiceDocument(
    string Title,
    IReadOnlyList<(string Label, string Value)> Meta,
    string CompanyName, string CompanyAddress, string CompanyMobile, string CompanyEmail, string CompanyWebsite,
    string PartyTitle, string PartyCompany, string PartyName, string PartyAddress, string PartyContact,
    IReadOnlyList<ExtraParty> ExtraParties,
    IReadOnlyList<ServiceDocLine> Lines,
    decimal Discount, decimal Shipping, string TotalLabel,
    string NotesTitle, string Notes, IReadOnlyList<string> NoteExtras,
    string Gstin, string Pan, string Signatory)
{
    public decimal Subtotal => Lines.Sum(l => l.Amount);
    public decimal Taxes => Lines.Sum(l => l.Amount * l.Gst / 100m);
    public decimal Total => Subtotal + Taxes - Discount + Shipping;
}

/// <summary>
/// Renders the service-document layout: title block, Bill From / party block, a blue-header
/// line table, notes, and the Subtotal / Taxes / Discount / Shipping / Total summary.
/// </summary>
public static class ServiceDocumentPdfBuilder
{
    public static byte[] FromInvoice(InvoiceDetailDto inv) => Build(new ServiceDocument(
        "Invoice",
        new[] { ("Invoice no.:", inv.Id), ("Invoice date:", inv.InvoiceDate.ToString("MMM dd, yyyy")),
                ("Due:", inv.DueDate?.ToString("MMM dd, yyyy") ?? "—") },
        inv.CompanyName, inv.CompanyAddress, inv.CompanyMobile, inv.CompanyEmail, inv.CompanyWebsite,
        "Bill To", inv.CustomerCompany, inv.CustomerName, inv.CustomerAddress, inv.CustomerEmail,
        Array.Empty<ExtraParty>(),
        inv.Lines.Select(l => new ServiceDocLine(l.PartNo, l.Description, l.Rate, l.Qty, l.Unit, l.GstPercent, l.Amount)).ToList(),
        inv.Discount, inv.Shipping, "Balance Due",
        "Payment Instructions or other notes", inv.Notes,
        new[] { string.IsNullOrWhiteSpace(inv.PaymentTerms) ? "" : $"Payment terms: {inv.PaymentTerms}",
                string.IsNullOrWhiteSpace(inv.DcRef) ? "" : $"Against: {inv.DcRef}" },
        inv.Gstin, inv.Pan, inv.Signatory));

    public static byte[] FromDeliveryChallan(DeliveryChallanDetailDto dc) => Build(new ServiceDocument(
        "Delivery Challan",
        new[] { ("Challan no.:", dc.Id), ("Challan date:", dc.DcDate.ToString("MMM dd, yyyy")) },
        dc.CompanyName, dc.CompanyAddress, dc.CompanyMobile, dc.CompanyEmail, dc.CompanyWebsite,
        "Ship To", dc.CustomerCompany, dc.CustomerName, dc.CustomerAddress, dc.CustomerMobile,
        Array.Empty<ExtraParty>(),
        dc.Lines.Select(l => new ServiceDocLine(string.Empty, l.PartLabel, l.Rate, l.Qty, l.Unit, l.GstPercent, l.Amount)).ToList(),
        0m, 0m, "Total",
        "Transport details or other notes", dc.Notes, Array.Empty<string>(),
        dc.Gstin, dc.Pan, dc.Signatory));

    private const string Blue = "#1D6FF2";
    private const string BlueTint = "#EAF2FF";
    private static readonly string Line = Colors.Grey.Lighten2;
    private static readonly string Muted = Colors.Grey.Darken2;
    private static readonly CultureInfo EnIn = CultureInfo.GetCultureInfo("en-IN");

    private static string Num(decimal n) => n.ToString("N2", EnIn);

    public static byte[] FromPurchaseOrder(PurchaseOrderDetailDto po) => Build(new ServiceDocument(
        "Purchase Order",
        new[] { ("PO no.:", po.Id), ("PO date:", po.PoDate.ToString("MMM dd, yyyy")) },
        po.CompanyName, po.CompanyAddress, po.CompanyMobile, po.CompanyEmail, po.CompanyWebsite,
        "Vendor", po.VendorCompany, po.VendorName, po.VendorAddress, po.VendorEmail,
        new[] { new ExtraParty("Ship To", po.ShipCompany, po.ShipName, po.ShipAddress, po.ShipMobile) },
        po.Lines.Select(l => new ServiceDocLine(l.PartNo, l.Description, l.Rate, l.Qty, l.Unit, l.GstPercent, l.Amount)).ToList(),
        0m, 0m, "Total",
        "Delivery instructions or other notes", po.Notes,
        new[] { string.IsNullOrWhiteSpace(po.QuotationRef) ? "" : $"Quotation ref: {po.QuotationRef}",
                string.IsNullOrWhiteSpace(po.ShippingMethod) ? "" : $"Shipping method: {po.ShippingMethod}" },
        po.Gstin, po.Pan, po.Signatory));

    public static byte[] FromQuote(QuoteDetailDto q) => Build(new ServiceDocument(
        "Quotation",
        new[] { ("Quote no.:", q.Id), ("Quote date:", q.QuoteDate.ToString("MMM dd, yyyy")),
                ("Valid till:", q.ValidUntil?.ToString("MMM dd, yyyy") ?? "—") },
        q.CompanyName, q.CompanyAddress, q.CompanyMobile, q.CompanyEmail, q.CompanyWebsite,
        "Quote To", q.CustomerCompany, q.CustomerName, q.CustomerAddress, q.CustomerEmail,
        Array.Empty<ExtraParty>(),
        q.Lines.Select(l => new ServiceDocLine(l.PartNo, l.Description, l.Rate, l.Qty, l.Unit, l.GstPercent, l.Amount)).ToList(),
        0m, 0m, "Total",
        "Terms or other notes", q.Notes,
        new[] { $"Status: {q.Status}" },
        q.Gstin, q.Pan, q.Signatory));

    private static byte[] Build(ServiceDocument inv)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Spacing(16);
                    col.Item().Element(c => Header(c, inv));
                    col.Item().Element(c => Parties(c, inv));
                    if (inv.ExtraParties.Count > 0) col.Item().Element(c => ExtraParties(c, inv));
                    col.Item().Element(c => Lines(c, inv));
                    col.Item().Element(c => Summary(c, inv));
                    col.Item().Element(c => Footer(c, inv));
                });
            });
        }).GeneratePdf();
    }

    private static void Header(IContainer container, ServiceDocument inv)
    {
        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(14).Border(1).BorderColor(Line).Background(Colors.Grey.Lighten5)
                .Padding(12).AlignMiddle().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Width(40).Height(40).Background(Colors.BlueGrey.Darken4)
                        .AlignMiddle().AlignCenter().Text("MS").FontColor(Colors.White).Bold().FontSize(14);
                    c.Item().PaddingTop(4).AlignCenter().Text(inv.CompanyName).FontSize(8).FontColor(Muted);
                });

            row.RelativeItem().Column(c =>
            {
                c.Spacing(4);
                c.Item().Border(1).BorderColor(Line).PaddingHorizontal(10).PaddingVertical(6)
                    .AlignRight().Text(inv.Title).FontSize(24).ExtraBold().FontColor(Colors.Grey.Lighten1);
                foreach (var (label, value) in inv.Meta) c.Item().Element(x => Meta(x, label, value));
            });
        });

        static void Meta(IContainer c, string label, string value) =>
            c.Border(1).BorderColor(Line).Background(Colors.Grey.Lighten5).PaddingHorizontal(10).PaddingVertical(5)
                .Row(r =>
                {
                    r.RelativeItem().Text(label).SemiBold();
                    r.AutoItem().Text(value);
                });
    }

    private static void Parties(IContainer container, ServiceDocument inv)
    {
        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(14).Column(c =>
            {
                c.Item().Text("Bill From").Bold().FontSize(10);
                c.Item().PaddingTop(4).Text(inv.CompanyName).SemiBold();
                c.Item().Text(inv.CompanyAddress).FontColor(Muted).FontSize(8);
                c.Item().Text(inv.CompanyMobile).FontColor(Muted).FontSize(8);
                c.Item().Text(inv.CompanyEmail).FontColor(Muted).FontSize(8);
                c.Item().Text(inv.CompanyWebsite).FontColor(Blue).FontSize(8);
            });

            row.RelativeItem().Column(c =>
            {
                c.Item().AlignRight().Text(inv.PartyTitle).Bold().FontSize(10);
                c.Item().PaddingTop(4).AlignRight().Text(Or(inv.PartyCompany)).SemiBold();
                if (!string.IsNullOrWhiteSpace(inv.PartyName)) c.Item().AlignRight().Text(inv.PartyName);
                if (!string.IsNullOrWhiteSpace(inv.PartyAddress)) c.Item().AlignRight().Text(inv.PartyAddress).FontColor(Muted).FontSize(8);
                if (!string.IsNullOrWhiteSpace(inv.PartyContact)) c.Item().AlignRight().Text(inv.PartyContact).FontColor(Muted).FontSize(8);
            });
        });

        static string Or(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s;
    }

    private static void ExtraParties(IContainer container, ServiceDocument inv)
    {
        container.Row(row =>
        {
            foreach (var p in inv.ExtraParties)
            {
                row.RelativeItem().PaddingRight(14).Column(c =>
                {
                    c.Item().Text(p.Title).Bold().FontSize(10);
                    c.Item().PaddingTop(4).Text(string.IsNullOrWhiteSpace(p.Company) ? "—" : p.Company).SemiBold();
                    if (!string.IsNullOrWhiteSpace(p.Name)) c.Item().Text(p.Name);
                    if (!string.IsNullOrWhiteSpace(p.Address)) c.Item().Text(p.Address).FontColor(Muted).FontSize(8);
                    if (!string.IsNullOrWhiteSpace(p.Contact)) c.Item().Text(p.Contact).FontColor(Muted).FontSize(8);
                });
            }
            row.RelativeItem();
        });
    }

    private static void Lines(IContainer container, ServiceDocument inv)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(5);
                cols.RelativeColumn(1.7f);
                cols.RelativeColumn(1.4f);
                cols.RelativeColumn(1.2f);
                cols.RelativeColumn(2f);
            });

            table.Header(h =>
            {
                h.Cell().Element(c => Head(c, "DESCRIPTION", false));
                h.Cell().Element(c => Head(c, "RATE, INR", true));
                h.Cell().Element(c => Head(c, "QTY", true));
                h.Cell().Element(c => Head(c, "TAX", true));
                h.Cell().Element(c => Head(c, "AMOUNT, INR", true));
            });

            foreach (var l in inv.Lines)
            {
                table.Cell().Element(Body).Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(l.PartNo)) c.Item().Text(l.PartNo).SemiBold().FontSize(8).FontColor(Muted);
                    c.Item().Text(l.Description);
                });
                table.Cell().Element(Body).AlignRight().Text(Num(l.Rate));
                table.Cell().Element(Body).AlignRight().Text($"{l.Qty:0.###} {l.Unit}");
                table.Cell().Element(Body).AlignRight().Text($"{l.Gst:0.##}%");
                table.Cell().Element(Body).AlignRight().Text(Num(l.Amount)).SemiBold();
            }

            if (inv.Lines.Count == 0)
            {
                table.Cell().ColumnSpan(5).Element(Body).AlignCenter().Text("No line items.").FontColor(Muted);
            }
        });

        static IContainer Head(IContainer c, string text, bool right)
        {
            var box = c.Background(Blue).PaddingVertical(7).PaddingHorizontal(8);
            (right ? box.AlignRight() : box).Text(text).FontColor(Colors.White).Bold().FontSize(8);
            return box;
        }

        static IContainer Body(IContainer c) =>
            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(7).PaddingHorizontal(8);
    }

    private static void Summary(IContainer container, ServiceDocument inv)
    {
        container.PaddingTop(4).BorderTop(1).BorderColor(Line).PaddingTop(14).Row(row =>
        {
            row.RelativeItem().PaddingRight(20).Column(c =>
            {
                c.Item().Text(inv.NotesTitle).Bold().FontSize(10);
                c.Item().PaddingTop(6).Text(string.IsNullOrWhiteSpace(inv.Notes) ? "—" : inv.Notes)
                    .FontColor(Muted);
                foreach (var extra in inv.NoteExtras.Where(e => !string.IsNullOrWhiteSpace(e)))
                    c.Item().PaddingTop(4).Text(extra).FontSize(8).FontColor(Muted);
            });

            row.RelativeItem().Column(c =>
            {
                c.Spacing(6);
                c.Item().Element(x => Sum(x, "Subtotal", Num(inv.Subtotal), true));
                c.Item().Element(x => Sum(x, "Taxes", Num(inv.Taxes), false));
                if (inv.Discount != 0) c.Item().Element(x => Sum(x, "Discount", "- " + Num(inv.Discount), false));
                if (inv.Shipping != 0) c.Item().Element(x => Sum(x, "Shipping", Num(inv.Shipping), false));
                c.Item().Background(BlueTint).PaddingHorizontal(8).PaddingVertical(8).Row(r =>
                {
                    r.RelativeItem().Text(inv.TotalLabel).Bold().FontSize(10);
                    r.AutoItem().Text(CurrencyFormatter.Format(inv.Total)).Bold().FontSize(12);
                });
            });
        });

        static void Sum(IContainer c, string label, string value, bool bold) =>
            c.PaddingHorizontal(4).Row(r =>
            {
                r.RelativeItem().Text(label).SemiBold();
                var t = r.AutoItem().Text(value);
                if (bold) t.SemiBold();
            });
    }

    private static void Footer(IContainer container, ServiceDocument inv)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("GSTIN").FontSize(8).FontColor(Muted);
                c.Item().Text(string.IsNullOrWhiteSpace(inv.Gstin) ? "—" : inv.Gstin);
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("PAN").FontSize(8).FontColor(Muted);
                c.Item().Text(string.IsNullOrWhiteSpace(inv.Pan) ? "—" : inv.Pan);
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignRight().Text($"For {inv.CompanyName}").SemiBold();
                c.Item().PaddingTop(22).AlignRight().Text(
                    string.IsNullOrWhiteSpace(inv.Signatory) ? "Authorized Signatory" : inv.Signatory)
                    .FontSize(8).FontColor(Muted);
            });
        });
    }
}
