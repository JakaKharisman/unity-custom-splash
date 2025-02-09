using JK.UnityCustomSplash;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private SerializedProperty sequenceGroupsProperty;
		private SerializedProperty evaluateGroupsProperty;
		private SerializedProperty playOnStartProperty;
		private SerializedProperty startedEventProperty;
		private SerializedProperty endedEventProperty;

		private bool groupsFoldout;
		private Vector2 sequenceGroupScrollPosition;

		protected virtual void OnEnable() {
			sequenceGroupsProperty = serializedObject.FindProperty(nameof(Splash.sequenceGroups));

			evaluateGroupsProperty = serializedObject.FindProperty(nameof(Splash.evaluateGroups));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));

			startedEventProperty = serializedObject.FindProperty(nameof(Splash.started));
			endedEventProperty = serializedObject.FindProperty(nameof(Splash.ended));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			groupsFoldout = EditorGUILayout.Foldout(groupsFoldout, new GUIContent("Groups"), true);
			if (groupsFoldout) {
				int length = sequenceGroupsProperty.arraySize;

				EditorGUILayout.BeginVertical(GUI.skin.box);
				sequenceGroupScrollPosition = EditorGUILayout.BeginScrollView(sequenceGroupScrollPosition, GUILayout.Height(100));

				for (int i = 0; i < length; i++) {
					var group = sequenceGroupsProperty.GetArrayElementAtIndex(i);
					var sequencesInGroup = group.FindPropertyRelative(nameof(Splash.SequenceGroup.sequences));

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(new GUIContent($"Group {i + 1}"));
					if (GUILayout.Button(new GUIContent("-"), new GUIStyle(GUI.skin.button) { fixedWidth = 30 })) {
						sequenceGroupsProperty.DeleteArrayElementAtIndex(i);
						break;
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.PropertyField(sequencesInGroup, new GUIContent(sequencesInGroup.isExpanded ? "▼" : "►"));
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

			EditorGUILayout.PropertyField(evaluateGroupsProperty);
			EditorGUILayout.PropertyField(playOnStartProperty);

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(startedEventProperty);
			EditorGUILayout.PropertyField(endedEventProperty);

			serializedObject.ApplyModifiedProperties();
		}
	}
}