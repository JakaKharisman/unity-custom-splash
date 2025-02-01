using JK.UnityCustomSplash;
using UnityEditor;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private static class Styles {
			public static GUIStyle SmallButtonFixed { get; }
			public static GUIStyle SmallButtonSelected { get; }
			public static GUIStyle WideButton { get; }

			static Styles() {
				WideButton = new GUIStyle(GUI.skin.button) { fixedWidth = 60 };
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
		private int navIndex;

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
							if (Button(selectedSequenceIndex == index ? "▲" : "▼")) {
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

			int range = Mathf.Clamp(groupsProperty.arraySize, 0, NAV_MAX);
			EditorGUILayout.BeginHorizontal();

			GUI.enabled = navIndex != 0;
			if (Button("<")) {
				navIndex = Mathf.Min(0, navIndex - 1);
			}
			GUI.enabled = true;

			for (int i = 0; i < range; i++) {
				int index = i + navIndex;
				if (Button($"{index + 1}", i == selectedGroupIndex)) {
					selectedGroupIndex = index;
					selectedSequenceIndex = -1;
				}
			}

			GUI.enabled = navIndex < groupsProperty.arraySize - range;
			if (Button(">")) {
				navIndex = Mathf.Min(navIndex + 1, groupsProperty.arraySize - range);
			}
			GUI.enabled = true;

			EditorGUILayout.Space();

			GUI.enabled = groupsProperty.arraySize > 0;
			if (Button("-")) {
				if (groupsProperty.arraySize > 0) {
					groupsProperty.DeleteArrayElementAtIndex(selectedGroupIndex);
					selectedGroupIndex = Mathf.Clamp(selectedGroupIndex - 1, 0, groupsProperty.arraySize);
				}
				if (groupsProperty.arraySize == 0) {
					selectedGroupIndex = -1;
				}
			}
			GUI.enabled = true;

			if (Button("+")) {
				CreateGroupAt(groupsProperty.arraySize);
			}
			EditorGUILayout.EndHorizontal();
			GUI.enabled = guiEnabled;
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
			selectedGroupIndex = index;
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