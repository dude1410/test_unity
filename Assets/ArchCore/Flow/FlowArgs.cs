namespace ArchCore.Flow
{
    public class FlowArgs
    {
        public T To<T>() where T : FlowArgs
        {
            return this as T;
        }
    }
}