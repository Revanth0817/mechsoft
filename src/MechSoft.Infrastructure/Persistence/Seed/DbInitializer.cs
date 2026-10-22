using MechSoft.Domain.Entities;
using MechSoft.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechSoft.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies pending EF Core migrations and seeds the mock data from the
/// original React app (INITIAL_PARTS, CUSTOMERS, INITIAL_POS, INITIAL_DCS,
/// INITIAL_INVOICES) on first run.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (await context.Parts.AnyAsync(ct))
        {
            logger.LogInformation("Database already seeded, skipping.");
        }
        else
        {
            logger.LogInformation("Seeding MechSoft mock data...");

            SeedParts(context);
            SeedCustomers(context);
            SeedPurchaseOrders(context);
            SeedDeliveryChallans(context);
            SeedInvoices(context);
            SeedQuotes(context);

            await context.SaveChangesAsync(ct);
            logger.LogInformation("Seeding complete.");
        }

        // Projects were added after the original seed, so backfill demo projects
        // into existing databases too instead of only seeding on a brand-new one.
        if (!await context.Projects.AnyAsync(ct))
        {
            SeedProjects(context);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo projects.");
        }
    }

    private static void SeedParts(ApplicationDbContext context)
    {
        var parts = new List<Part>
        {
            NewPart("P-001", "TRL-4402-TI", "Titanium Rocker Lever", "Grade 5 Titanium", 128, StockStatus.InStock, "Valvetrain",
                ("Torque", "45 N·m"), ("Weight", "0.18 kg"), ("Bore Dia.", "12.4 mm"), ("Finish", "Anodized")),
            NewPart("P-002", "ACT-9021-CAL", "Calibrated Linear Actuator", "6061 Aluminum", 6, StockStatus.LowStock, "Actuation",
                ("Voltage", "24 VDC"), ("Stroke", "150 mm"), ("Load Rating", "800 N"), ("Speed", "42 mm/s")),
            NewPart("P-003", "BRG-1188-SS", "Sealed Roller Bearing", "440C Stainless", 342, StockStatus.InStock, "Motion",
                ("Bore Dia.", "35 mm"), ("Outer Dia.", "72 mm"), ("Width", "17 mm"), ("Load Rating", "12.5 kN")),
            NewPart("P-004", "HYD-5560-CYL", "Hydraulic Cylinder Assembly", "Forged Steel", 0, StockStatus.OutOfStock, "Hydraulics",
                ("Bore Dia.", "63 mm"), ("Stroke", "300 mm"), ("Pressure", "210 bar"), ("Rod", "Hard Chrome")),
            NewPart("P-005", "SRV-2274-HD", "Heavy-Duty Servo Motor", "Cast Iron / Copper Wound", 19, StockStatus.InStock, "Drive",
                ("Voltage", "480 VAC"), ("Torque", "12.8 N·m"), ("RPM", "3000"), ("Frame", "NEMA 34")),
            NewPart("P-006", "VLV-3309-BDY", "Directional Valve Body", "Ductile Iron", 4, StockStatus.LowStock, "Hydraulics",
                ("Ports", "4/3-Way"), ("Pressure", "250 bar"), ("Flow Rate", "80 L/min"), ("Thread", "G 1/2\"")),
            NewPart("P-007", "GBX-7715-HSG", "Precision Gearbox Housing", "A356 Cast Aluminum", 57, StockStatus.InStock, "Transmission",
                ("Ratio Range", "5:1 – 40:1"), ("Weight", "4.2 kg"), ("Mounting", "Face Flange"), ("Rating", "IP65")),
            NewPart("P-008", "SNS-1042-PRX", "Inductive Proximity Sensor", "Nickel-Plated Brass", 210, StockStatus.InStock, "Sensors",
                ("Voltage", "10–30 VDC"), ("Range", "8 mm"), ("Output", "PNP NO"), ("Rating", "IP67")),
            NewPart("P-009", "CPL-6633-FLX", "Flexible Shaft Coupling", "EN8 Carbon Steel", 3, StockStatus.LowStock, "Motion",
                ("Bore Range", "10–35 mm"), ("Torque", "95 N·m"), ("Max RPM", "6500"), ("Backlash", "Zero")),
            NewPart("P-010", "PMP-8814-GER", "Gerotor Hydraulic Pump", "Sintered Alloy", 12, StockStatus.InStock, "Hydraulics",
                ("Displacement", "16 cc/rev"), ("Pressure", "180 bar"), ("Speed", "2800 RPM"), ("Inlet", "SAE-12")),
        };

        context.Parts.AddRange(parts);
    }

    private static Part NewPart(string id, string sku, string name, string material, int stock, StockStatus status,
        string category, params (string Label, string Value)[] specs)
    {
        var part = new Part
        {
            Id = id,
            Sku = sku,
            Name = name,
            Material = material,
            Stock = stock,
            Status = status,
            Category = category,
        };

        var order = 0;
        foreach (var (label, value) in specs)
        {
            part.Specs.Add(new PartSpec { Label = label, Value = value, SortOrder = order++ });
        }

        return part;
    }

    private static void SeedCustomers(ApplicationDbContext context)
    {
        string[] names =
        {
            "Meridian Heavy Equipment Co.",
            "Blackstone Fabrication Ltd.",
            "Andover Industrial Group",
            "Falcon Drivetrain Systems",
            "Quarryline Machinery Inc.",
        };

        context.Customers.AddRange(names.Select(n => new Customer { CompanyName = n }));
    }

    private static void SeedPurchaseOrders(ApplicationDbContext context)
    {
        var pos = new[]
        {
            new PurchaseOrder
            {
                Id = "PO-24091", VendorCompany = "Titanmet Alloys Pvt. Ltd.", VendorName = "Procurement Desk",
                VendorAddress = "Industrial Estate, Coimbatore", VendorEmail = "sales@titanmet.example",
                ShipCompany = "MechSoft Technologies", ShipName = "Stores In-Charge", ShipAddress = "Perundurai, Erode",
                ShipMobile = "+91 9994142288", PoDate = new DateTime(2026, 8, 22), QuotationRef = "QTN-8801",
                ShippingMethod = "Road", Gstin = "33AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan",
                ItemCount = 6, TotalValue = 482300m,
            },
            new PurchaseOrder
            {
                Id = "PO-24088", VendorCompany = "Steelcraft Forgings", VendorName = "Sales Team",
                VendorAddress = "Ambattur Industrial Estate, Chennai", VendorEmail = "sales@steelcraft.example",
                ShipCompany = "MechSoft Technologies", ShipName = "Stores In-Charge", ShipAddress = "Perundurai, Erode",
                ShipMobile = "+91 9994142288", PoDate = new DateTime(2026, 8, 18), QuotationRef = "QTN-8795",
                ShippingMethod = "Road", Gstin = "33AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan",
                ItemCount = 3, TotalValue = 115000m,
            },
            new PurchaseOrder
            {
                Id = "PO-24081", VendorCompany = "Precision Bearings Co.", VendorName = "Sales Desk",
                VendorAddress = "Peenya Industrial Area, Bengaluru", VendorEmail = "sales@precisionbearings.example",
                ShipCompany = "MechSoft Technologies", ShipName = "Stores In-Charge", ShipAddress = "Perundurai, Erode",
                ShipMobile = "+91 9994142288", PoDate = new DateTime(2026, 8, 10), QuotationRef = "QTN-8770",
                ShippingMethod = "Rail", Gstin = "33AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan",
                ItemCount = 12, TotalValue = 690450m,
            },
            new PurchaseOrder
            {
                Id = "PO-24076", VendorCompany = "Coastal Hydraulics", VendorName = "Sales Team",
                VendorAddress = "Tuticorin Industrial Zone", VendorEmail = "sales@coastalhydraulics.example",
                ShipCompany = "MechSoft Technologies", ShipName = "Stores In-Charge", ShipAddress = "Perundurai, Erode",
                ShipMobile = "+91 9994142288", PoDate = new DateTime(2026, 8, 4), QuotationRef = "QTN-8752",
                ShippingMethod = "Road", Gstin = "33AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan",
                ItemCount = 4, TotalValue = 234000m,
            },
        };

        context.PurchaseOrders.AddRange(pos);
    }

    private static void SeedDeliveryChallans(ApplicationDbContext context)
    {
        var dcs = new[]
        {
            new DeliveryChallan
            {
                Id = "DC-11029", CustomerCompany = "Meridian Heavy Equipment Co.", CustomerName = "Stores Manager",
                CustomerAddress = "Plot 14, Heavy Equipment Park, Pune", CustomerMobile = "+91 9800011029",
                DcDate = new DateTime(2026, 9, 8), Gstin = "27AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan",
                Lines = new List<DeliveryChallanLine>
                {
                    new() { PartId = "P-001", Qty = 40, Rate = 0m, GstPercent = 18m, Unit = "NOS" },
                    new() { PartId = "P-003", Qty = 60, Rate = 0m, GstPercent = 18m, Unit = "NOS" },
                },
            },
            new DeliveryChallan
            {
                Id = "DC-11024", CustomerCompany = "Blackstone Fabrication Ltd.", CustomerName = "Stores Manager",
                CustomerAddress = "Sector 5, Fabrication Hub, Nagpur", CustomerMobile = "+91 9800011024",
                DcDate = new DateTime(2026, 9, 5), Gstin = "27AACCM5678F1Z5", Pan = "AACCM5678F", Signatory = "R. Kannan",
                Lines = new List<DeliveryChallanLine>
                {
                    new() { PartId = "P-005", Qty = 8, Rate = 0m, GstPercent = 18m, Unit = "NOS" },
                    new() { PartId = "P-007", Qty = 15, Rate = 0m, GstPercent = 18m, Unit = "NOS" },
                },
            },
            new DeliveryChallan
            {
                Id = "DC-11019", CustomerCompany = "Andover Industrial Group", CustomerName = "Stores Manager",
                CustomerAddress = "Andover Industrial Park, Hyderabad", CustomerMobile = "+91 9800011019",
                DcDate = new DateTime(2026, 9, 1), Gstin = "36AACCM9012F1Z5", Pan = "AACCM9012F", Signatory = "R. Kannan",
                Lines = new List<DeliveryChallanLine>
                {
                    new() { PartId = "P-002", Qty = 3, Rate = 0m, GstPercent = 18m, Unit = "NOS" },
                },
            },
        };

        context.DeliveryChallans.AddRange(dcs);
    }

    private static void SeedInvoices(ApplicationDbContext context)
    {
        var invoices = new[]
        {
            new Invoice
            {
                Id = "INV-7701", CustomerCompany = "Meridian Heavy Equipment Co.", CustomerName = "Accounts Payable",
                CustomerAddress = "Plot 14, Heavy Equipment Park, Pune", CustomerEmail = "accounts@meridianheavy.example",
                InvoiceDate = new DateTime(2026, 9, 8), DcRef = "DC-11029", PaymentTerms = "Net 30",
                Gstin = "27AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan", TotalValue = 384200.00m,
            },
            new Invoice
            {
                Id = "INV-7695", CustomerCompany = "Blackstone Fabrication Ltd.", CustomerName = "Accounts Payable",
                CustomerAddress = "Sector 5, Fabrication Hub, Nagpur", CustomerEmail = "accounts@blackstonefab.example",
                InvoiceDate = new DateTime(2026, 9, 5), DcRef = "DC-11024", PaymentTerms = "Net 30",
                Gstin = "27AACCM5678F1Z5", Pan = "AACCM5678F", Signatory = "R. Kannan", TotalValue = 112750.00m,
            },
            new Invoice
            {
                Id = "INV-7688", CustomerCompany = "Andover Industrial Group", CustomerName = "Accounts Payable",
                CustomerAddress = "Andover Industrial Park, Hyderabad", CustomerEmail = "accounts@andoverindustrial.example",
                InvoiceDate = new DateTime(2026, 9, 1), DcRef = "DC-11019", PaymentTerms = "Net 15",
                Gstin = "36AACCM9012F1Z5", Pan = "AACCM9012F", Signatory = "R. Kannan", TotalValue = 48900.00m,
            },
        };

        context.Invoices.AddRange(invoices);
    }

    private static void SeedQuotes(ApplicationDbContext context)
    {
        var quotes = new[]
        {
            new Quote
            {
                Id = "QT-1002", CustomerCompany = "Quarryline Machinery Inc.", CustomerName = "Procurement Manager",
                CustomerAddress = "Quarryline Industrial Zone, Nagpur", CustomerEmail = "procurement@quarryline.example",
                QuoteDate = new DateTime(2026, 9, 10), ValidUntil = new DateTime(2026, 9, 25), Status = QuoteStatus.Sent,
                Gstin = "27AACCM3456F1Z5", Pan = "AACCM3456F", Signatory = "R. Kannan", TotalValue = 156400.00m,
            },
            new Quote
            {
                Id = "QT-1001", CustomerCompany = "Falcon Drivetrain Systems", CustomerName = "V. Muthukumar",
                CustomerAddress = "Chakan-Talegaon Rd, Pune - 410501, Maharashtra", CustomerEmail = "v.muthukumar@falcondrivetrain.co.in",
                QuoteDate = new DateTime(2026, 9, 3), ValidUntil = new DateTime(2026, 9, 18), Status = QuoteStatus.Accepted,
                Gstin = "27AACCM1234F1Z5", Pan = "AACCM1234F", Signatory = "R. Kannan", TotalValue = 19175.00m,
                ConvertedInvoiceId = "INV-7703",
            },
        };

        context.Quotes.AddRange(quotes);
    }

    private static void SeedProjects(ApplicationDbContext context)
    {
        var today = DateTime.Today;

        var projects = new[]
        {
            new Project
            {
                Id = "PRJ-001", Name = "VMC 850 — Vertical Machining Center", MachineType = "CNC Vertical Machining Center",
                CustomerName = "Falcon Drivetrain Systems", StartDate = today.AddDays(-46), TargetDate = today.AddDays(14),
                Status = ProjectStatus.InProgress, CompletionPercent = 65,
                Notes = "Spindle and ATC assembled; awaiting servo motor delivery for final axis calibration.",
            },
            new Project
            {
                Id = "PRJ-002", Name = "HP-210 Hydraulic Press Line", MachineType = "Hydraulic Press",
                CustomerName = "Quarryline Machinery Inc.", StartDate = today.AddDays(-80), TargetDate = today.AddDays(-5),
                Status = ProjectStatus.Testing, CompletionPercent = 88,
                Notes = "In load testing at 210 bar. Customer FAT scheduled once pressure test report is signed off.",
            },
            new Project
            {
                Id = "PRJ-003", Name = "GBX-40 Precision Gearbox Cell", MachineType = "Gearbox Assembly Line",
                CustomerName = "Repro Vendor", StartDate = today.AddDays(-20), TargetDate = today.AddDays(40),
                Status = ProjectStatus.Planning, CompletionPercent = 10,
                Notes = "Design review complete. Long-lead castings on order.",
            },
            new Project
            {
                Id = "PRJ-004", Name = "SRV-3000 Servo Winding Rig", MachineType = "Servo Motor Test Rig",
                CustomerName = "Quarryline Machinery Inc.", StartDate = today.AddDays(-15), TargetDate = today.AddDays(5),
                Status = ProjectStatus.OnHold, CompletionPercent = 30,
                Notes = "Paused pending customer approval of revised winding spec.",
            },
            new Project
            {
                Id = "PRJ-005", Name = "DCV-4/3 Directional Valve Test Bench", MachineType = "Hydraulics Test Bench",
                CustomerName = "Falcon Drivetrain Systems", StartDate = today.AddDays(-120), TargetDate = today.AddDays(-30),
                Status = ProjectStatus.Completed, CompletionPercent = 100,
                Notes = "Delivered and commissioned. Handover sign-off on file.",
            },
            new Project
            {
                Id = "PRJ-006", Name = "PMP-88 Gerotor Pump Cell", MachineType = "Pump Assembly & Test Cell",
                CustomerName = "Repro Vendor", StartDate = today.AddDays(-8), TargetDate = today.AddDays(55),
                Status = ProjectStatus.InProgress, CompletionPercent = 22,
                Notes = "Base frame fabrication underway.",
            },
        };

        context.Projects.AddRange(projects);
    }
}
