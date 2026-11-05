namespace UrlShortener.Api.Services
{
    public interface IDateTimeHelper
    {
        DateTime GetUtcNow();
    }

    public class DateTimeHelper : IDateTimeHelper
    {
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
