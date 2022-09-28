using UnityEngine;

namespace ArchCore.Flow
{
	public abstract class BaseFlow
	{
		readonly bool isLogsShow = false;
		private IFlowKey key;
		protected virtual FlowResult OnFinish()
		{
			return null;
		}

		public virtual void Start()
		{
			
		}

		public FlowResult Finish()
		{
			if (isLogsShow)
				Debug.Log($"-[#flow] {GetType().Name} Finish");

			return OnFinish();
		}

		protected BaseFlow(IFlowKey key)
		{
			this.key = key;

			if (isLogsShow)
				Debug.Log($"-[#flow] {GetType().Name} Created");
		}
		
		~BaseFlow()
		{
			if (isLogsShow)
				Debug.Log($"-[#flow] {GetType().Name} Destroyed");
		}

		protected void ContinueWith<T>(FlowArgs args = null) where T : BaseFlow
		{
			key.ContinueWith<T>(args, Finish());
		}
		
		protected IFlowHandler RunSubFlow<T>(FlowArgs args = null) where T : BaseFlow
		{
			return key.RunSubFlow<T>(args);
		}

		protected void Return()
		{
			if (isLogsShow)
				Debug.Log($"-[#flow] {GetType().Name} Return");

			key.Finish(OnFinish());
		}
		
		protected void Return(FlowResult result)
		{
			OnFinish();
			key.Finish(result);
		}

		public override string ToString()
		{
			return $"[{GetType().Name}]";
		}
	}
}