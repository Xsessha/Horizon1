namespace HORIZON1.Models
{
    // ENUM (Перерахування): Визначає чіткий список типів повторення подій.
    // Це допомагає уникнути помилок, бо ми не зможемо передати випадкове число,
    // а тільки одне з цих значень.
    public enum RecurrencePattern
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }
}
