using System;

namespace ArchCore.Pooling
{
    public interface IPrototype : ICloneable
    {
        void ResetToProto();
        void SetProto(IPrototype prototype);
    }
}