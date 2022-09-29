using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TBR.HeadlessServer
{
	public static class HeadlessHelper
	{
		static bool isHeadlessForced = false;

		public static void SetHeadlessForced(bool val)
		{
			isHeadlessForced = val;
		}

		public static bool IsHeadless()
		{
			return isHeadlessForced || Headless.IsHeadless();
		}
	}
}