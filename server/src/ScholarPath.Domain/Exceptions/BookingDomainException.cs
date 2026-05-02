namespace ScholarPath.Domain.Exceptions;

public sealed class BookingDomainException : Exception
{
    public BookingDomainException(string message)
        : base(message)
    {
    }

    public BookingDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
