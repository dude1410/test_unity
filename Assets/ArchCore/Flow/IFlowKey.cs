using System;

namespace ArchCore.Flow
{
    public interface IFlowKey
    {
        void Finish(FlowResult result);
        void ContinueWith<T>(FlowArgs args = null, FlowResult result = null) where T : BaseFlow;
        IFlowHandler RunSubFlow<T>(FlowArgs args = null) where T : BaseFlow;
    }
}