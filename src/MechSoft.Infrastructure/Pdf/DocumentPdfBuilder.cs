using MechSoft.Application.Common;
using MechSoft.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MechSoft.Infrastructure.Pdf;

/// <summary>
/// Renders a <see cref="PdfDocumentDto"/> as a PDF, one shared layout for
/// Purchase Orders, Delivery Challans, Invoices and Quotes.
/// </summary>
public static class DocumentPdfBuilder
{
    private static readonly string BorderColor = Colors.Grey.Darken1;
    private static readonly string MutedColor = Colors.Grey.Darken2;
    private static readonly string AccentColor = Colors.Blue.Darken2;

    public static byte[] Build(PdfDocumentDto doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                page.Content().Border(1).BorderColor(BorderColor).Padding(14).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Element(c => ComposeLetterhead(c, doc));
                    column.Item().Element(c => ComposePartyBlocks(c, doc));

                    if (doc.ExtraFields.Count > 0)
                    {
                        column.Item().Element(c => ComposeMetaRow(c, doc.ExtraFields));
                    }

                    column.Item().Element(c => ComposeLineTable(c, doc));
                    column.Item().AlignRight().Element(c => ComposeGrandTotal(c, doc));
                    column.Item().Element(c => ComposeFooter(c, doc));
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeLetterhead(IContainer container, PdfDocumentDto doc)
    {
        container.BorderBottom(1).BorderColor(BorderColor).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem(2).Row(inner =>
            {
                inner.ConstantItem(36).Height(36).Background(Colors.BlueGrey.Darken4)
                    .AlignMiddle().AlignCenter()
                    .Text("MS").FontColor(Colors.White).SemiBold().FontSize(12);

                inner.RelativeItem().PaddingLeft(8).Column(col =>
                {
                    col.Item().Text(doc.CompanyName).FontSize(13).SemiBold();
                    col.Item().Text(doc.CompanyAddress).FontSize(8).FontColor(MutedColor);
                    col.Item().Text($"{doc.CompanyMobile}   ·   {doc.CompanyEmail}").FontSize(8).FontColor(MutedColor);
                    col.Item().Text(doc.CompanyWebsite).FontSize(8).FontColor(AccentColor);
                });
            });

            row.ConstantItem(180).Border(1).BorderColor(BorderColor).Column(col =>
            {
                col.Item().BorderBottom(1).BorderColor(BorderColor).Background(Colors.Grey.Lighten4)
                    .AlignCenter().Padding(4).Text(doc.DocumentTypeLabel.ToUpperInvariant()).FontSize(8).FontColor(MutedColor);

                foreach (var (meta, index) in doc.MetaRows.Select((m, i) => (m, i)))
                {
                    col.Item().Row(r =>
                    {
                        var top = index == 0 ? 0 : 1;
                        r.ConstantItem(68).BorderRight(1).BorderTop((float)top).BorderColor(BorderColor)
                            .Padding(4).Text(meta.Label).FontSize(8).FontColor(MutedColor);
                        r.RelativeItem().BorderTop((float)top).BorderColor(BorderColor)
                            .Padding(4).Text(meta.Value).FontSize(8).SemiBold();
                    });
                }
            });
        });
    }

    private static void ComposePartyBlocks(IContainer container, PdfDocumentDto doc)
    {
        container.Row(row =>
        {
            foreach (var block in doc.PartyBlocks)
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(block.Title.ToUpperInvariant()).FontSize(8).SemiBold().FontColor(MutedColor);
                    if (!string.IsNullOrWhiteSpace(block.Company)) col.Item().Text(block.Company).FontSize(9).SemiBold();
                    if (!string.IsNullOrWhiteSpace(block.Name)) col.Item().Text(block.Name).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(block.Address)) col.Item().Text(block.Address).FontSize(8).FontColor(MutedColor);
                    if (!string.IsNullOrWhiteSpace(block.Contact)) col.Item().Text(block.Contact).FontSize(8).FontColor(MutedColor);
                });
            }
        });
    }

    private static void ComposeMetaRow(IContainer container, IReadOnlyList<PdfMetaRow> fields)
    {
        container.Row(row =>
        {
            foreach (var field in fields)
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(field.Label).FontSize(8).FontColor(MutedColor);
                    col.Item().Text(string.IsNullOrWhiteSpace(field.Value) ? "—" : field.Value).FontSize(9);
                });
            }
        });
    }

    private static void ComposeLineTable(IContainer container, PdfDocumentDto doc)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                if (doc.ShowPartNoColumn) columns.RelativeColumn(2.2f); // Part No
                columns.RelativeColumn(4f);   // Description
                columns.RelativeColumn(1.3f); // GST %
                columns.RelativeColumn(1.3f); // Qty
                columns.RelativeColumn(1.3f); // Unit
                if (doc.ShowRateColumn) columns.RelativeColumn(1.6f); // Rate
                columns.RelativeColumn(1.8f); // Amount
                columns.RelativeColumn(1.8f); // Total
            });

            table.Header(header =>
            {
                if (doc.ShowPartNoColumn) header.Cell().Element(c => HeaderStyle(c, false)).Text("Part No");
                header.Cell().Element(c => HeaderStyle(c, false)).Text("Description");
                header.Cell().Element(c => HeaderStyle(c, true)).Text("GST %");
                header.Cell().Element(c => HeaderStyle(c, true)).Text("Qty");
                header.Cell().Element(c => HeaderStyle(c, false)).Text("Unit");
                if (doc.ShowRateColumn) header.Cell().Element(c => HeaderStyle(c, true)).Text("Rate");
                header.Cell().Element(c => HeaderStyle(c, true)).Text("Amount");
                header.Cell().Element(c => HeaderStyle(c, true)).Text("Total");
            });

            foreach (var line in doc.Lines)
            {
                if (doc.ShowPartNoColumn) table.Cell().Element(BodyCell).Text(line.PartNo);
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(c => BodyCell(c).AlignRight()).Text(line.GstPercent.ToString("0.##"));
                table.Cell().Element(c => BodyCell(c).AlignRight()).Text(line.Qty.ToString("0.###"));
                table.Cell().Element(BodyCell).Text(line.Unit);
                if (doc.ShowRateColumn)
                {
                    table.Cell().Element(c => BodyCell(c).AlignRight()).Text(CurrencyFormatter.Format(line.Rate ?? 0));
                }
                table.Cell().Element(c => BodyCell(c).AlignRight()).Text(CurrencyFormatter.Format(line.Amount));
                table.Cell().Element(c => BodyCell(c).AlignRight()).Text(CurrencyFormatter.Format(line.Total)).SemiBold();
            }

            if (doc.Lines.Count == 0)
            {
                var colSpan = 6 + (doc.ShowPartNoColumn ? 1 : 0) + (doc.ShowRateColumn ? 1 : 0);
                table.Cell().ColumnSpan((uint)colSpan).Element(c => BodyCell(c).AlignCenter())
                    .Text("No line items.").FontColor(MutedColor);
            }
        });

        static IContainer HeaderStyle(IContainer c, bool right) => (right ? c.AlignRight() : c)
            .Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(BorderColor)
            .PaddingVertical(5).PaddingHorizontal(4).DefaultTextStyle(x => x.FontSize(8).SemiBold().FontColor(MutedColor));

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5).PaddingHorizontal(4).DefaultTextStyle(x => x.FontSize(8.5f));
    }

    private static void ComposeGrandTotal(IContainer container, PdfDocumentDto doc)
    {
        container.Width(160).Border(1).BorderColor(BorderColor).Padding(8).Column(col =>
        {
            col.Item().AlignRight().Text("GRAND TOTAL").FontSize(8).FontColor(MutedColor);
            col.Item().AlignRight().Text(CurrencyFormatter.Format(doc.GrandTotal)).FontSize(13).SemiBold();
        });
    }

    private static void ComposeFooter(IContainer container, PdfDocumentDto doc)
    {
        container.PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("GSTIN").FontSize(8).FontColor(MutedColor);
                col.Item().Text(string.IsNullOrWhiteSpace(doc.Gstin) ? "—" : doc.Gstin).FontSize(9);
            });
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("PAN").FontSize(8).FontColor(MutedColor);
                col.Item().Text(string.IsNullOrWhiteSpace(doc.Pan) ? "—" : doc.Pan).FontSize(9);
            });
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text($"For {doc.CompanyName}").FontSize(9).SemiBold();
                col.Item().PaddingTop(20).AlignRight().Text(
                    string.IsNullOrWhiteSpace(doc.Signatory) ? "Authorized Signatory" : doc.Signatory).FontSize(8).FontColor(MutedColor);
            });
        });
    }
}
