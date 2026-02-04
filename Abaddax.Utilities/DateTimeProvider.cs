namespace Abaddax.Utilities
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
    public abstract class DateTimeProviderBase : TimeProvider, IDateTimeProvider
    {
        public virtual DateTime Now => UtcNow.ToLocalTime();
        public abstract DateTime UtcNow { get; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
    public sealed class DateTimeProviderWrapper : DateTimeProviderBase
    {
        private readonly TimeProvider _timeProvider;

        public override DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        private DateTimeProviderWrapper(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            _timeProvider = timeProvider;
        }
        public static DateTimeProviderWrapper Wrap(TimeProvider timeProvider)
        {
            return new DateTimeProviderWrapper(timeProvider);
        }
    }
}
