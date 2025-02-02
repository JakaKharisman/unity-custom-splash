using JK.UnityCustomSplash;
using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private static class Styles {
			public static GUIStyle SmallButtonFixed { get; }
			public static GUIStyle SmallButtonSelected { get; }

			static Styles() {
				SmallButtonFixed = new GUIStyle(GUI.skin.button) { fixedWidth = 30 };
				SmallButtonSelected = new GUIStyle(GUI.skin.button) { fixedWidth = 30, fontStyle = FontStyle.Bold, };
			}
		}

		const int NAV_MAX = 3;

		private SerializedProperty groupsProperty;
		private SerializedProperty playOnStartProperty;
		private SerializedProperty skipButtonProperty;

		private SerializedProperty onPlayedEventProperty;
		private SerializedProperty onFinishedEventProperty;

		private int selectedGroupIndex;
		private int selectedSequenceIndex;
		private int navigationIndex;

		private bool eventFoldout;

		private Editor sequenceEditor;

		private void OnEnable() {
			groupsProperty = serializedObject.FindProperty(nameof(Splash.groups));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));
			skipButtonProperty = serializedObject.FindProperty(nameof(Splash.skipButton));

			onPlayedEventProperty = serializedObject.FindProperty(nameof(Splash.onPlayed));
			onFinishedEventProperty = serializedObject.FindProperty(nameof(Splash.onFinished));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.Separator();
			DrawNavigation();
			EditorGUILayout.Separator();
			DrawSelectedGroup();
			DrawOptions();
			DrawEvents();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawSelectedGroup() {
			int previousIndentLevel = EditorGUI.indentLevel;

			if (groupsProperty.arraySize == 0 || selectedGroupIndex == -1) {
				EditorGUILayout.HelpBox("No sequence reference exists", MessageType.Warning);
			} else {
				EditorGUILayout.LabelField($"Group {selectedGroupIndex + 1}");
				if (selectedGroupIndex >= 0) {
					var groupProperty = groupsProperty.GetArrayElementAtIndex(selectedGroupIndex);
					var sequencesProperty = groupProperty.FindPropertyRelative(nameof(Splash.GroupInfo.sequences));

					int count = sequencesProperty.arraySize;
					if (count == 0) {
						EditorGUILayout.HelpBox("Add one or more sequence", MessageType.Warning);
					} else {
						for (int i = 0; i < count; i++) {
							int index = i;
							var sequenceProperty = sequencesProperty.GetArrayElementAtIndex(index);

							EditorGUILayout.BeginHorizontal();
							if (Button("-")) {
								sequencesProperty.DeleteArrayElementAtIndex(index);
								break;
							}
							EditorGUILayout.PropertyField(sequenceProperty, new GUIContent($"Sequence"));
							GUI.enabled = sequenceProperty.objectReferenceValue;
							if (Button(sequenceProperty.objectReferenceValue && selectedSequenceIndex == index ? "▲" : "▼")) {
								if (selectedSequenceIndex == index) {
									selectedSequenceIndex = -1;
									break;
								} else {
									if (sequenceProperty.objectReferenceValue) {
										selectedSequenceIndex = index;
										break;
									}
								}
							}
							GUI.enabled = true;
							EditorGUILayout.EndHorizontal();

							if (selectedSequenceIndex == index) {
								var target = sequenceProperty.objectReferenceValue;
								if (target) {
									EditorGUI.indentLevel++;
									CreateCachedEditor(target, typeof(SplashSequenceEditor), ref sequenceEditor);
									sequenceEditor.OnInspectorGUI();
									EditorGUILayout.Separator();
									EditorGUI.indentLevel--;
								}
							}
						}
					}
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					if (Button("+")) {
						sequencesProperty.InsertArrayElementAtIndex(sequencesProperty.arraySize);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUI.indentLevel = previousIndentLevel;
		}

		private void DrawNavigation() {
			var guiEnabled = GUI.enabled;

			EditorGUILayout.BeginHorizontal();
			int size = Mathf.Max((groupsProperty.arraySize - 1) / NAV_MAX, 0);

			GUI.enabled = navigationIndex > 0;
			if (Button("<")) {
				navigationIndex = Mathf.Clamp(navigationIndex - 1, 0, size);
			}

			for (int i = 0; i < NAV_MAX; i++) {
				int index = (navigationIndex * NAV_MAX) + i;
				GUI.enabled = index < groupsProperty.arraySize;
				if (Button($"{index + 1}", index == selectedGroupIndex)) {
					selectedGroupIndex = index;
					selectedSequenceIndex = -1;
				}
			}

			GUI.enabled = navigationIndex < size;
			if (Button(">")) {
				navigationIndex = Mathf.Clamp(navigationIndex + 1, 0, size);
			}

			EditorGUILayout.Space();

			GUI.enabled = groupsProperty.arraySize > 0;
			if (Button("-")) {
				if (groupsProperty.arraySize > 0) {
					groupsProperty.DeleteArrayElementAtIndex(selectedGroupIndex);
					selectedGroupIndex = Mathf.Clamp(selectedGroupIndex - 1, 0, groupsProperty.arraySize);
				}
				if (groupsProperty.arraySize == 0) selectedGroupIndex = -1;
				UpdateIndex();
			}

			GUI.enabled = true;
			if (Button("+")) {
				selectedGroupIndex = groupsProperty.arraySize;
				CreateGroupAt(selectedGroupIndex);
				UpdateIndex();
			}

			EditorGUILayout.EndHorizontal();
			
			GUI.enabled = guiEnabled;

			void UpdateIndex() {
				int min = navigationIndex * NAV_MAX;
				int max = min + NAV_MAX;
				if (selectedGroupIndex >= min && selectedGroupIndex < max) return;

				navigationIndex = Mathf.Max((groupsProperty.arraySize - 1) / NAV_MAX, 0);
			}
		}

		private void DrawOptions() {
			int previousIndentLevel = EditorGUI.indentLevel;

			EditorGUILayout.LabelField("Options");

			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(playOnStartProperty, new GUIContent("Play on Start"));
			EditorGUILayout.PropertyField(skipButtonProperty, new GUIContent("Skip Button"));

			EditorGUI.indentLevel = previousIndentLevel;
		}

		private void DrawEvents() {
			eventFoldout = EditorGUILayout.Foldout(eventFoldout, new GUIContent("Events"), true);
			if (eventFoldout) {
				EditorGUILayout.PropertyField(onPlayedEventProperty);
				EditorGUILayout.PropertyField(onFinishedEventProperty);
			}
		}

		private void CreateGroupAt(int index) {
			groupsProperty.InsertArrayElementAtIndex(index);
			var groupProperty = groupsProperty.GetArrayElementAtIndex(index);
			var sequencesProperty = groupProperty.FindPropertyRelative(nameof(Splash.GroupInfo.sequences));
			sequencesProperty.ClearArray();
			sequencesProperty.InsertArrayElementAtIndex(0);
		}

		private static bool Button(string label, bool selected = false) {
			return GUILayout.Button(new GUIContent(label), (!selected) ? Styles.SmallButtonFixed : Styles.SmallButtonSelected);
		}
	}
}