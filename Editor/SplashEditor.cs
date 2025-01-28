using JK.UnityCustomSplash;
using UnityEditor;
using UnityEngine;

namespace JK.UnitySplashExtendedEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private SerializedProperty sequenceReferencesProperty;
		private SerializedProperty playOnStartProperty;

		private SerializedProperty onPlayedEventProperty;
		private SerializedProperty onFinishedEventProperty;

		private bool eventFoldout;

		private void OnEnable() {
			sequenceReferencesProperty = serializedObject.FindProperty(nameof(Splash._sequenceReferences));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));

			onPlayedEventProperty = serializedObject.FindProperty(nameof(Splash.onPlayed));
			onFinishedEventProperty = serializedObject.FindProperty(nameof(Splash.onFinished));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			int previousIndentLevel = EditorGUI.indentLevel;

			EditorGUILayout.PropertyField(sequenceReferencesProperty, new GUIContent("Sequences"));
			EditorGUILayout.PropertyField(playOnStartProperty);

			//EditorGUILayout.LabelField("Events");
			eventFoldout = EditorGUILayout.Foldout(eventFoldout, new GUIContent("Events"));
			if (eventFoldout) {
				EditorGUILayout.PropertyField(onPlayedEventProperty);
				EditorGUILayout.PropertyField(onFinishedEventProperty);
			}

			EditorGUI.indentLevel = previousIndentLevel;

			serializedObject.ApplyModifiedProperties();
		}
	}
}