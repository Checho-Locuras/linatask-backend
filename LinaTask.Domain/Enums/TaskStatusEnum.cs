namespace LinaTask.Domain.Enums
{
    public enum TaskStatus
    {
        Open,
        InReview,
        Assigned,
        Paid,
        InProgress,
        Delivered,
        InCorrection,
        Completed,
        Cancelled,
        Disputed
    }

    public enum OfferStatus
    {
        Pending,
        Accepted,
        Rejected,
        Withdrawn
    }

    public enum PaymentStatus
    {
        Pending,
        Held,
        Released,
        Refunded,
        Failed
    }

    public enum WorkType
    {
        Essay,
        Workshop,
        Exam,
        Project,
        Programming,
        Research,
        Presentation,
        Other
    }

    public enum AcademicLevel
    {
        School,
        Technical,
        University,
        Postgraduate
    }

    public enum RequiredFormat
    {
        Word,
        PDF,
        PowerPoint,
        Code,
        Video,
        Excel,
        Other
    }

    public enum CorrectionStatus
    {
        Pending,
        InProgress,
        Resolved
    }

    public enum MarketplacePaymentStatus
    {
        Pending,
        Held,
        Released,
        Refunded,
        Failed
    }
}
