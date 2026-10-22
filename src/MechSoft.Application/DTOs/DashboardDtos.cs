namespace MechSoft.Application.DTOs;

public record DashboardStatsDto(
    int ActiveSkuCount,
    int LowOrOutOfStockCount,
    int OpenPurchaseOrderCount,
    int DeliveryChallanCount);

public record RecentDocumentDto(
    string Id,
    string Kind, // "Purchase Order" | "Delivery Challan"
    string CounterpartyName,
    DateTime Date,
    decimal Value);

public record ProjectSummaryDto(
    int OngoingCount,
    int CompletedCount,
    int OnHoldCount,
    double AverageCompletion);

public record DashboardViewDto(
    DashboardStatsDto Stats,
    IReadOnlyList<PartListItemDto> PartsRequiringAttention,
    IReadOnlyList<RecentDocumentDto> RecentDocuments,
    ProjectSummaryDto ProjectSummary,
    IReadOnlyList<ProjectListItemDto> OngoingProjects);
