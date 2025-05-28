namespace LichLord
{
    public interface IBackHandler
    {
        int Priority { get; }
        bool IsActive { get; }

        bool OnBackAction();
    }
}
