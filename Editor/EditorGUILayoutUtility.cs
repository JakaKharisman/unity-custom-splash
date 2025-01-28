using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	public static class EditorGUILayoutUtility {
		public static void DrawInspectorScriptReference(this Object target) {
			EditorGUI.BeginDisabledGroup(true);
			MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);
			EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
			EditorGUI.EndDisabledGroup();
		}
	}
}