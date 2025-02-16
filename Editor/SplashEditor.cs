using JK.UnityCustomSplash;
using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private SerializedProperty sequenceGroupsProperty;
		private SerializedProperty removeEmptyReferencesProperty;

		private SerializedProperty skippableProperty;
		private SerializedProperty playOnStartProperty;

		private SerializedProperty onPlayProperty;
		private SerializedProperty onSkipProperty;
		private SerializedProperty onEndProperty;

		private bool groupsFoldout;
		private Vector2 sequenceGroupScrollPosition;

		protected virtual void OnEnable() {
			sequenceGroupsProperty = serializedObject.FindProperty(nameof(Splash.sequenceGroups));

			removeEmptyReferencesProperty = serializedObject.FindProperty(nameof(Splash.removeEmptyReferences));
			skippableProperty = serializedObject.FindProperty(nameof(Splash.skippable));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));

			onPlayProperty = serializedObject.FindProperty(nameof(Splash.onPlay));
			onSkipProperty = serializedObject.FindProperty(nameof(Splash.onSkip));
			onEndProperty = serializedObject.FindProperty(nameof(Splash.onEnd));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			var indentLevel = EditorGUI.indentLevel;

			groupsFoldout = EditorGUILayout.Foldout(groupsFoldout, new GUIContent("Groups"), true);
			if (groupsFoldout) {
				int length = sequenceGroupsProperty.arraySize;

				EditorGUILayout.BeginVertical(GUI.skin.box);
				sequenceGroupScrollPosition = EditorGUILayout.BeginScrollView(sequenceGroupScrollPosition, GUILayout.Height(200));

				for (int i = 0; i < length; i++) {
					var sequenceGroupProperty = sequenceGroupsProperty.GetArrayElementAtIndex(i);
					var skippableConditionProperty = sequenceGroupProperty.FindPropertyRelative(nameof(Splash.SequenceGroup.skippableCondition));
					var sequencesInGroupProperty = sequenceGroupProperty.FindPropertyRelative(nameof(Splash.SequenceGroup.sequences));

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(new GUIContent($"Group {i + 1}"));
					if (GUILayout.Button(new GUIContent("-"), new GUIStyle(GUI.skin.button) { fixedWidth = 30 })) {
						sequenceGroupsProperty.DeleteArrayElementAtIndex(i);
						break;
					}
					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(sequencesInGroupProperty, new GUIContent("Sequences"));
					EditorGUILayout.PropertyField(skippableConditionProperty, new GUIContent("Skippable Condition"));
					EditorGUI.indentLevel--;
				}

				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button(new GUIContent("+"), new GUIStyle(GUI.skin.button) { fixedWidth = 30 })) {
					sequenceGroupsProperty.InsertArrayElementAtIndex(length);
					var property = sequenceGroupsProperty.GetArrayElementAtIndex(length);

				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.PropertyField(removeEmptyReferencesProperty);
			EditorGUILayout.PropertyField(skippableProperty);
			EditorGUILayout.PropertyField(playOnStartProperty);

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(onPlayProperty);
			EditorGUILayout.PropertyField(onSkipProperty);
			EditorGUILayout.PropertyField(onEndProperty);

			EditorGUI.indentLevel = indentLevel;
			serializedObject.ApplyModifiedProperties();
		}
	}
}