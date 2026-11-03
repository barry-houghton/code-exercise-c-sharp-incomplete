namespace UrlShortener.Api.Exceptions
{
    public class InvalidAliasException(string message) : DomainException(message)
    {
    }
}
