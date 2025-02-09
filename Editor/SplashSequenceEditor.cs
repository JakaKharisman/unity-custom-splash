using JK.UnityCustomSplash;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(SplashSequence), true)]
	public class SplashSequenceEditor : Editor {
		private SerializedProperty canvasGroupProperty;
		private SerializedProperty sequenceCurveProperty;
		private SerializedProperty interactableDuringSequenceProperty;
		private SerializedProperty blocksRaycastsDuringSequenceProperty;

		private SerializedProperty animatorProperty;
		private SerializedProperty layerIndexProperty;
		private SerializedProperty animatorStartStateHashProperty;
		private SerializedProperty animatorEndStateHashProperty;

		private SerializedProperty videoPlayerProperty;

		private SerializedProperty sequenceTypeProperty;
		private SerializedProperty inactiveBeforeSequenceProperty;
		private SerializedProperty inactiveAfterSequenceProperty;
		private SerializedProperty skippableProperty;

		private void OnEnable() {
			canvasGroupProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroup));
			sequenceCurveProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceCurve));
			interactableDuringSequenceProperty = serializedObject.FindProperty(nameof(SplashSequence.interactableDuringSequence));
			blocksRaycastsDuringSequenceProperty = serializedObject.FindProperty(nameof(SplashSequence.blocksRaycastsDuringSequence));

			animatorProperty = serializedObject.FindProperty(nameof(SplashSequence.animator));
			layerIndexProperty = serializedObject.FindProperty(nameof(SplashSequence.layerIndex));
			animatorStartStateHashProperty = serializedObject.FindProperty(nameof(SplashSequence.animatorStartStateHash));
			animatorEndStateHashProperty = serializedObject.FindProperty(nameof(SplashSequence.animatorEndStateHash));

			videoPlayerProperty = serializedObject.FindProperty(nameof(SplashSequence.videoPlayer));

			sequenceTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceType));
			inactiveAfterSequenceProperty = serializedObject.FindProperty(nameof(SplashSequence.inactiveAfterSequence));
			inactiveBeforeSequenceProperty = serializedObject.FindProperty(nameof(SplashSequence.inactiveBeforeSequence));
			skippableProperty = serializedObject.FindProperty(nameof(SplashSequence.skippable));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			bool enabled;
			int indentLevel = EditorGUI.indentLevel;

			EditorGUILayout.PropertyField(sequenceTypeProperty, new GUIContent("Sequence Type"));
			var sequenceType = (SplashSequence.SequenceType)sequenceTypeProperty.enumValueIndex;
			EditorGUI.indentLevel++;
			switch (sequenceType) {
				case SplashSequence.SequenceType.CanvasGroup:
					EditorGUILayout.PropertyField(canvasGroupProperty, new GUIContent("Target"));

					var canvasGroup = canvasGroupProperty.objectReferenceValue as CanvasGroup;

					enabled = GUI.enabled;
					GUI.enabled = canvasGroup;

					EditorGUILayout.PropertyField(sequenceCurveProperty, new GUIContent("Sequence Curve"));
					EditorGUILayout.PropertyField(interactableDuringSequenceProperty, new GUIContent("Interactable During Sequence"));
					EditorGUILayout.PropertyField(blocksRaycastsDuringSequenceProperty, new GUIContent("Blocks Raycasts During Sequence"));

					GUI.enabled = enabled;
					break;
				case SplashSequence.SequenceType.Animator:
					EditorGUILayout.PropertyField(animatorProperty, new GUIContent("Target"));

					var animator = animatorProperty.objectReferenceValue as Animator;
					var animatorController = animator
						? animator.runtimeAnimatorController as AnimatorController
						: null;

					enabled = GUI.enabled;
					GUI.enabled = animator && animatorController;

					int layerIndex = layerIndexProperty.intValue;
					int maxLayerIndex = (animatorController)
						? Mathf.Max(0, animatorController.layers.Length - 1)
						: 0;
					layerIndexProperty.intValue = layerIndex = EditorGUILayout.IntSlider("Layer Index", layerIndex, 0, maxLayerIndex);

					var animatorStates = new List<AnimatorState>();
					if (animatorController) {
						var childStates = animatorController.layers[layerIndex].stateMachine.states;
						foreach (var childState in childStates) {
							if (animatorStates.Contains(childState.state)) continue;

							animatorStates.Add(childState.state);
						}
					}

					var startOptions = (animatorStates.Count > 0)
						? animatorStates
							.Select(state => state.name)
							.ToArray()
						: new string[] { "None" };
					int startStateIndex = Mathf.Max(0, animatorStates.FindIndex(state => state.nameHash == animatorStartStateHashProperty.intValue));
					startStateIndex = EditorGUILayout.Popup("Start State", startStateIndex, startOptions);
					animatorStartStateHashProperty.intValue = (animatorStates.Count > 0)
						? animatorStates[startStateIndex].nameHash
						: int.MinValue;
					
					var endOptions = (animatorStates.Count > 0)
						? animatorStates
							.Select(state => state.name)
							.Prepend("None")
							.ToArray()
						: new string[] { "None" };

					int endStateIndex = animatorStates.FindIndex(state => state.nameHash == animatorEndStateHashProperty.intValue);
					endStateIndex = Mathf.Max(0, endStateIndex + 1);

					endStateIndex = EditorGUILayout.Popup("End State", endStateIndex, endOptions);

					animatorEndStateHashProperty.intValue = (endStateIndex > 0)
						? animatorStates[endStateIndex - 1].nameHash
						: int.MinValue;

					GUI.enabled = enabled;
					break;
				case SplashSequence.SequenceType.Video:
					EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent("Target"));
					break;
			}
			EditorGUI.indentLevel--;

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(inactiveBeforeSequenceProperty, new GUIContent("Inactive Before Sequence"));
			EditorGUILayout.PropertyField(inactiveAfterSequenceProperty, new GUIContent("Inactive After Sequence"));
			EditorGUILayout.PropertyField(skippableProperty, new GUIContent("Skippable"));

			EditorGUI.indentLevel = indentLevel;
			serializedObject.ApplyModifiedProperties();
		}
	}
}