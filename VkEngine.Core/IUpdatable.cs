namespace VkEngine
{
    public interface IUpdatable
    {
        void StartUpdate(PageWriteKey key);

        void Update(PageWriteKey key);
    }
}
