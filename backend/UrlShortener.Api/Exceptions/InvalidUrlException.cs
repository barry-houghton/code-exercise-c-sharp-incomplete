namespace UrlShortener.Api.Exceptions
{
    public class InvalidUrlException(string message) : DomainException(message)
    {
    }
}
