using System;

namespace ArchCore.Flow
{
    public interface IFlowHandler
    {
        event Action<FlowResult> OnFinished;
        event Action<FlowResult> OnFlowChanged;
        BaseFlow Flow { get; }
    }
}