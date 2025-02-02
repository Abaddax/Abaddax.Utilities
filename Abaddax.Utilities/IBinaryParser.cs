namespace Abaddax.Utilities
{
    public interface IBinaryParser<T>
    {
        T Parse(byte[] packet);
        byte[] Parse(T message);
    }
}
