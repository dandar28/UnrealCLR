using System;
using System.Drawing;
using UnrealEngine.Framework;

namespace UnrealEngine.Game {
	public static class Main {
		public static void OnWorldPostBegin() {
			Debug.AddOnScreenMessage(-1, 2, Color.Cyan, "Hello World!");
		}

		public static void OnWorldEnd() {
			Debug.AddOnScreenMessage(-1, 2, Color.Cyan, "Goodbye World!");
		}
	}
}