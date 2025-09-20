using DWD.Pooling;

namespace LichLord.Items
{
    public class Item : DWDObjectPoolObject
    {
        public virtual void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
        }
    }
}
