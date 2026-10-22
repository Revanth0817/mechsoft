using MechSoft.Domain.Enums;

namespace MechSoft.Application.DTOs;

public record ProjectListItemDto(
    string Id,
    string Name,
    string MachineType,
    string CustomerName,
    DateTime StartDate,
    DateTime? TargetDate,
    ProjectStatus Status,
    int CompletionPercent,
    bool HasPhoto);

public record ProjectDetailDto(
    string Id,
    string Name,
    string MachineType,
    string CustomerName,
    DateTime StartDate,
    DateTime? TargetDate,
    ProjectStatus Status,
    int CompletionPercent,
    string? Notes,
    bool HasPhoto);

public record ProjectSaveDto(
    string? Id,
    string Name,
    string MachineType,
    string CustomerName,
    DateTime StartDate,
    DateTime? TargetDate,
    ProjectStatus Status,
    int CompletionPercent,
    string? Notes);
