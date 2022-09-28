using System;

namespace ArchCore.MVP
{
    public class ViewNotRegisteredException : Exception
    {
        public ViewNotRegisteredException(Type presenterType, AbstractViewManager viewManager) 
            : base($"No view registered of type {presenterType} in {viewManager}.")
        {
            
        }
    }
}