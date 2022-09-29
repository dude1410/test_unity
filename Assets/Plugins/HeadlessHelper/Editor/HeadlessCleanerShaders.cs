#if HEADLESS
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace TBR.HeadlessServer
{
	public class HeadlessCleanerShaders : IPreprocessShaders, IPreprocessComputeShaders
	{
		public int callbackOrder => 999;

		public HeadlessCleanerShaders()
		{
		}

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			Debug.Log($"{this} removed all shader variants: {shader}");
			Clear(data);
		}

		public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
		{
			Debug.Log($"{this} removed all shader variants: {shader}");
			Clear(data);
		}

		void Clear(IList<ShaderCompilerData> data)
		{
			int count = data.Count;
			while (count-- > 0)
			{
				data.RemoveAt(0);
			}
		}

		public override string ToString()
		{
			return $"[{GetType().Name}]";
		}
	}
}
#endif