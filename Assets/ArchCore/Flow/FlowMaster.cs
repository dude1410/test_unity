using System;
using System.Collections.Generic;
using System.Linq;
using ArchCore.DataStructures.Hierarchy;
using UnityEngine;

using Zenject;

namespace ArchCore.Flow
{
    public class FlowMaster 
    {
        private DiContainer diContainer;
        private Hierarchy<FlowKey> flowTree;

        private Type creationLock = null;
        
        public FlowMaster(DiContainer diContainer)
        {
            this.diContainer = diContainer;
            flowTree = new Hierarchy<FlowKey>();
        }


        private void Create(FlowKey key, Type type, FlowArgs args)
        {
            if (creationLock!=null) throw new CreatingFlowFromOtherFlowConstructorException(type, creationLock);
            
            var arguments = new List<object>(2){key};
            if (args != null) arguments.Add(args);
            key.OnContinueWith = ContinueWith;
            key.OnRunSubFlow = RunSubFlow;
            creationLock = type;
            key.Flow = diContainer.Instantiate(type, arguments) as BaseFlow;
            creationLock = null;
            key.Flow.Start();
            
        }

        public void Start<T>(FlowArgs args = null) where T : BaseFlow
        {
            flowTree.FirstNode?.Flow?.Finish();
            flowTree.Clear();
            var key = new FlowKey();
            flowTree.AddItem(key);
            Create(key, typeof(T), args);
            
        }
        
        private void ContinueWith(FlowKey key, Type type, FlowArgs args)
        {
            RemoveFlowChildren(key);
            Create(key, type, args);
        }

        private IFlowHandler RunSubFlow(FlowKey key, Type type, FlowArgs args) 
        {
            RemoveFlowChildren(key);
            var subKey = new FlowKey();
            subKey.OnFinished += r => RemoveFlow(subKey);
            flowTree.AddItem(subKey);
            Create(subKey, type, args);
            
            return subKey;
        }

        void RemoveFlow(FlowKey item)
        {
            if (!flowTree.Contains(item))
            {
                Debug.LogError($"Hierarchy does not contain item {item.Flow.GetType()}!");
                return;
            }

            while (flowTree.Count > 0)
            {
                FlowKey removedView = flowTree.LastNode;
                flowTree.RemoveLast();
                if (removedView == item) break;
                removedView.Flow.Finish();
                removedView.Cleanup();
            }

        }
        
        void RemoveFlowChildren(FlowKey item)
        {
            if (!flowTree.Contains(item))
            {
                Debug.LogError($"Hierarchy does not contain item {item.Flow.GetType()}!");
                return;
            }

            while (flowTree.Count > 0)
            {
                FlowKey removedFlow = flowTree.LastNode;
                
                if(removedFlow == item) break;
                
                
                flowTree.RemoveLast();
                removedFlow.Flow.Finish();
                removedFlow.Cleanup();
            }
           
        }

        public void RemoveAll()
        {
            Debug.Log($"{this} remove all. count: {flowTree.Count}");
            while (flowTree.Count > 0)
            {
                FlowKey removedFlow = flowTree.LastNode;
                flowTree.RemoveLast();
                removedFlow.Flow.Finish();
                removedFlow.Cleanup();
            }
        }

        private class FlowKey : IFlowKey, IFlowHandler
        {            
            public event Action<FlowResult> OnFinished;
            public event Action<FlowResult> OnFlowChanged;
            
            public Action<FlowKey, Type, FlowArgs> OnContinueWith { get; set; }
            public Func<FlowKey, Type, FlowArgs, IFlowHandler> OnRunSubFlow { get; set; }

            public BaseFlow Flow { get; set; }

            public void Finish(FlowResult result)
            {
                OnFinished?.Invoke(result);
            }

            public void ContinueWith<T>(FlowArgs args, FlowResult result) 
                where T : BaseFlow
            {
                OnContinueWith(this, typeof(T), args);
                OnFlowChanged?.Invoke(result);
            }

            public IFlowHandler RunSubFlow<T>(FlowArgs args) 
                where T : BaseFlow
            {
                return OnRunSubFlow(this, typeof(T), args);
            }

            public void Cleanup()
            {
                OnFinished = null;
                OnFlowChanged = null;
                OnContinueWith = null;
                OnRunSubFlow = null;
                Flow = null;
            }

            public override string ToString()
            {
                return Flow?.GetType().ToString();
            }
        }
    }
}