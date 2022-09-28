using System;

namespace ArchCore.Flow
{
    public class CreatingFlowFromOtherFlowConstructorException : Exception
    {
        public CreatingFlowFromOtherFlowConstructorException(Type newFlow, Type constructorFlow) 
            : base($"Trying to create {newFlow} from {constructorFlow} Constructor. Use {constructorFlow}.Start() for creating new flows.")
        {
            
        }
    }
}