namespace UrlShortener.Api.Exceptions
{
    public class DuplicateAliasException(string message) : DomainException(message)
    {
    }
}
