using JK.UnityCustomSplash;
using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private SerializedProperty sequenceReferencesProperty;
		private SerializedProperty playOnStartProperty;

		private SerializedProperty onPlayedEventProperty;
		private SerializedProperty onFinishedEventProperty;

		private bool eventFoldout;

		private void OnEnable() {
			sequenceReferencesProperty = serializedObject.FindProperty(nameof(Splash._sequenceInfos));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));

			onPlayedEventProperty = serializedObject.FindProperty(nameof(Splash.onPlayed));
			onFinishedEventProperty = serializedObject.FindProperty(nameof(Splash.onFinished));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			int previousIndentLevel = EditorGUI.indentLevel;

			EditorGUILayout.PropertyField(playOnStartProperty);
			EditorGUILayout.PropertyField(sequenceReferencesProperty, new GUIContent("Sequences"), true);

			eventFoldout = EditorGUILayout.Foldout(eventFoldout, new GUIContent("Events"), true);
			if (eventFoldout) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(onPlayedEventProperty);
				EditorGUILayout.PropertyField(onFinishedEventProperty);
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel = previousIndentLevel;

			serializedObject.ApplyModifiedProperties();
		}
	}
}