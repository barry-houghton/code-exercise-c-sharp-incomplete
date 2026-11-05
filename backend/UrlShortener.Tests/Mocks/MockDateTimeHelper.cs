using Moq;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.Mocks
{
    public class MockDateTimeHelper : Mock<IDateTimeHelper>
    {
        public MockDateTimeHelper()
        {
            Setup(m => m.GetUtcNow()).Returns(DateTime.UtcNow);
        }

        public void AdvanceTimeBy(TimeSpan timeSpan)
        {
            var currentTime = DateTime.UtcNow;
            Setup(m => m.GetUtcNow()).Returns(currentTime.Add(timeSpan));
        }
    }
}
