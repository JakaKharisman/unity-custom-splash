using JK.UnityCustomSplash;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(Splash))]
	public class SplashEditor : Editor {
		private static class Styles {
			public static GUIStyle SmallButtonFixed { get; }
			public static GUIStyle SmallButtonSelected { get; }
			public static GUIStyle LabelBold { get; }

			static Styles() {
				SmallButtonFixed = new GUIStyle(GUI.skin.button) { fixedWidth = 30 };
				SmallButtonSelected = new GUIStyle(GUI.skin.button) { fixedWidth = 30, fontStyle = FontStyle.Bold, };
				LabelBold = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
			}
		}

		const int NAV_MAX = 5;

		private SerializedProperty groupsProperty;
		private SerializedProperty playOnStartProperty;
		private SerializedProperty skipButtonProperty;

		private SerializedProperty onPlayedEventProperty;
		private SerializedProperty onSkippedEventProperty;
		private SerializedProperty onFinishedEventProperty;

		private int navigationIndex;
		private int selectedGroupIndex;
		private Dictionary<int, List<bool>> groupSequenceStates = new Dictionary<int, List<bool>>();

		private bool eventFoldout;

		private Editor sequenceEditor;

		private void OnEnable() {
			groupsProperty = serializedObject.FindProperty(nameof(Splash.groups));
			playOnStartProperty = serializedObject.FindProperty(nameof(Splash.playOnStart));
			skipButtonProperty = serializedObject.FindProperty(nameof(Splash.skipButton));

			onPlayedEventProperty = serializedObject.FindProperty(nameof(Splash.onPlayed));
			onSkippedEventProperty = serializedObject.FindProperty(nameof(Splash.onSkipped));
			onFinishedEventProperty = serializedObject.FindProperty(nameof(Splash.onFinished));
		}

		private void Reset() {
			selectedGroupIndex = 0;
			groupSequenceStates.Clear();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			DrawNavigation();
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
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				EditorGUILayout.LabelField(new GUIContent($"Group {selectedGroupIndex + 1}"), new GUIStyle(Styles.LabelBold) { alignment = TextAnchor.MiddleCenter });
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Separator();

				if (selectedGroupIndex >= 0) {
					if (!groupSequenceStates.ContainsKey(selectedGroupIndex)) groupSequenceStates[selectedGroupIndex] = new List<bool>();

					var groupProperty = groupsProperty.GetArrayElementAtIndex(selectedGroupIndex);
					var sequencesProperty = groupProperty.FindPropertyRelative(nameof(Splash.GroupInfo.sequences));

					int count = sequencesProperty.arraySize;
					if (count == 0) {
						EditorGUILayout.HelpBox("Add one or more sequence", MessageType.Warning);
					} else {
						for (int i = 0; i < count; i++) {
							if (groupSequenceStates[selectedGroupIndex].Count >= i) groupSequenceStates[selectedGroupIndex].Add(false);

							var sequenceProperty = sequencesProperty.GetArrayElementAtIndex(i);

							EditorGUILayout.BeginHorizontal();
							if (Button("-")) {
								sequencesProperty.DeleteArrayElementAtIndex(i);
								groupSequenceStates[selectedGroupIndex].RemoveAt(i);
								break;
							}
							EditorGUILayout.PropertyField(sequenceProperty, new GUIContent($"Sequence"));
							GUI.enabled = sequenceProperty.objectReferenceValue;
							if (!sequenceProperty.objectReferenceValue) groupSequenceStates[selectedGroupIndex][i] = false;

							if (Button(groupSequenceStates[selectedGroupIndex][i] ? "▼" : "▲")) {
								groupSequenceStates[selectedGroupIndex][i] = !groupSequenceStates[selectedGroupIndex][i];
							}
							GUI.enabled = true;
							EditorGUILayout.EndHorizontal();

							if (groupSequenceStates[selectedGroupIndex][i]) {
								var target = sequenceProperty.objectReferenceValue;
								if (target) {
									CreateCachedEditor(target, typeof(SplashSequenceEditor), ref sequenceEditor);
									sequenceEditor.OnInspectorGUI();
									EditorGUILayout.Separator();
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

			int size = Mathf.Max((groupsProperty.arraySize - 1) / NAV_MAX, 0);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();

			GUI.enabled = groupsProperty.arraySize > 0;
			if (Button("-")) {
				if (groupsProperty.arraySize > 0) {
					RemoveGroupAt(selectedGroupIndex);
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

			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			// pages
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			
			GUI.enabled = navigationIndex > 0;
			if (Button("<")) {
				navigationIndex = Mathf.Clamp(navigationIndex - 1, 0, size);
			}

			for (int i = 0; i < NAV_MAX; i++) {
				int index = (navigationIndex * NAV_MAX) + i;
				GUI.enabled = index < groupsProperty.arraySize;
				if (Button($"{index + 1}", index == selectedGroupIndex)) {
					selectedGroupIndex = index;
				}
			}

			GUI.enabled = navigationIndex < size;
			if (Button(">")) {
				navigationIndex = Mathf.Clamp(navigationIndex + 1, 0, size);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

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

			EditorGUILayout.LabelField(new GUIContent("Options"), Styles.LabelBold);

			EditorGUILayout.PropertyField(playOnStartProperty, new GUIContent("Play on Start"));
			EditorGUILayout.PropertyField(skipButtonProperty, new GUIContent("Skip Button"));
		}

		private void DrawEvents() {
			eventFoldout = EditorGUILayout.Foldout(eventFoldout, new GUIContent("Events"), true);
			if (eventFoldout) {
				EditorGUILayout.PropertyField(onPlayedEventProperty);
				EditorGUILayout.PropertyField(onSkippedEventProperty);
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

		private void RemoveGroupAt(int index) {
			groupsProperty.DeleteArrayElementAtIndex(index);
			selectedGroupIndex = Mathf.Clamp(index - 1, 0, groupsProperty.arraySize);
			groupSequenceStates.Remove(index);
		}

		private static bool Button(string label, bool selected = false) {
			return GUILayout.Button(new GUIContent(label), (!selected) ? Styles.SmallButtonFixed : Styles.SmallButtonSelected);
		}
	}
}