using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Domain.Entities;

public class Report
{
    public Guid Id { get; private set; }

    public string ReporterUserId { get; private set; } = null!;

    public Guid? EmotionalPointId { get; private set; }
    public EmotionalPoint? EmotionalPoint { get; private set; }

    public Guid? PlaceId { get; private set; }
    public Place? Place { get; private set; }

    public string Reason { get; private set; } = null!;
    public ReportStatus Status { get; private set; } = ReportStatus.Open;

    public string? ModeratorNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Report() { }

    public Report(string reporterUserId, string reason, Guid? emotionalPointId = null, Guid? placeId = null)
    {
        if (string.IsNullOrWhiteSpace(reporterUserId)) throw new ArgumentException("ReporterUserId required.", nameof(reporterUserId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason required.", nameof(reason));
        if (emotionalPointId is null && placeId is null) throw new ArgumentException("Target required.");

        Id = Guid.NewGuid();
        ReporterUserId = reporterUserId;
        EmotionalPointId = emotionalPointId;
        PlaceId = placeId;
        Reason = reason.Trim();
        Status = ReportStatus.Open;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(ReportStatus status, string? moderatorNote = null)
    {
        Status = status;
        ModeratorNote = moderatorNote;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}