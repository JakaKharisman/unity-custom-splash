using JK.UnityCustomSplash;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Video;

namespace JK.UnityCustomSplashEditor {
	[CustomEditor(typeof(SplashSequence), true)]
	public class SplashSequenceEditor : Editor {
		private SerializedProperty transitionTypeProperty;
		private SerializedProperty modifyGameObjectOnTransitionProperty;

		private SerializedProperty canvasGroupTransitionTargetProperty;
		private SerializedProperty modifyCanvasGroupInteractableOnTransitionProperty;
		private SerializedProperty modifyCanvasGroupBlockRaycastOnTransitionProperty;

		private SerializedProperty transitionInCanvasGroupInfoProperty;
		private SerializedProperty transitionOutCanvasGroupInfoProperty;

		private SerializedProperty transitionInAnimatorInfoProperty;
		private SerializedProperty transitionOutAnimatorInfoProperty;

		private SerializedProperty transitionInVideoInfoProperty;
		private SerializedProperty transitionOutVideoInfoProperty;

		private SerializedProperty customTransitionTargetProperty;

		private SerializedProperty sequenceTypeProperty;
		private SerializedProperty sequenceStayDurationProperty;
		private SerializedProperty sequenceAnimatorInfoProperty;
		private SerializedProperty sequenceVideoInfoProperty;

		protected Type currentType;
		protected Type baseType;

		private bool inFoldout = true;
		private bool outFoldout = true;

		protected virtual bool DrawCurrentTypeProperties => true;

		private void OnEnable() {
			currentType = target.GetType();
			baseType = typeof(SplashSequence);

			transitionTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionType));
			modifyGameObjectOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyGameObjectOnTransition));

			canvasGroupTransitionTargetProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupTransitionTarget));
			transitionInCanvasGroupInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionInCanvasGroupInfo));
			transitionOutCanvasGroupInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionOutCanvasGroupInfo));
			modifyCanvasGroupInteractableOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyCanvasGroupInteractableOnTransition));
			modifyCanvasGroupBlockRaycastOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyCanvasGroupBlockRaycastOnTransition));

			transitionInAnimatorInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionInAnimatorInfo));
			transitionOutAnimatorInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionOutAnimatorInfo));

			transitionInVideoInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionInVideoInfo));
			transitionOutVideoInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionOutVideoInfo));

			customTransitionTargetProperty = serializedObject.FindProperty(nameof(SplashSequence.customTransitionTarget));

			sequenceTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceType));

			sequenceStayDurationProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceStayDuration));
			sequenceAnimatorInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceAnimatorInfo));
			sequenceVideoInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.sequenceVideoInfo));

		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			if (currentType != baseType) target.DrawInspectorScriptReference();
			DrawTransitionInspector();
			DrawSequenceInspector();

			if (DrawCurrentTypeProperties) {
				var property = serializedObject.GetIterator();
				property.NextVisible(true);

				while (property.NextVisible(false)) {
					var field = currentType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field == null || field.DeclaringType == baseType) continue;

					EditorGUILayout.PropertyField(property, true);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		protected virtual void DrawTransitionInspector() {
			EditorGUILayout.PropertyField(transitionTypeProperty, new GUIContent("Transition"));

			int previousIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel++;
			switch ((SplashSequence.TransitionType)transitionTypeProperty.enumValueIndex) {
				case SplashSequence.TransitionType.CanvasGroup:
					EditorGUILayout.PropertyField(canvasGroupTransitionTargetProperty, new GUIContent("Target"));
					if (canvasGroupTransitionTargetProperty.objectReferenceValue) {
						inFoldout = DrawCanvasGroupInfoProperty("In", inFoldout, transitionInCanvasGroupInfoProperty);
						outFoldout = DrawCanvasGroupInfoProperty("Out", outFoldout, transitionOutCanvasGroupInfoProperty);

						EditorGUILayout.PropertyField(modifyCanvasGroupBlockRaycastOnTransitionProperty, new GUIContent("Blocks Raycasts", "If true, CanvasGroup.blocksRaycasts will be modified during transition."));
						EditorGUILayout.PropertyField(modifyCanvasGroupInteractableOnTransitionProperty, new GUIContent("Interactable", "If true, CanvasGroup.interactable will be modified during transition."));
					}
					DrawAdditionalProperties();
					break;
				case SplashSequence.TransitionType.Animator:
					DrawAnimatorInfoProperty("In", transitionInAnimatorInfoProperty, true);
					DrawAnimatorInfoProperty("Out", transitionOutAnimatorInfoProperty, true);
					DrawAdditionalProperties();
					break;
				case SplashSequence.TransitionType.Video:
					DrawVideoInfoProperty("In", transitionInVideoInfoProperty);
					DrawVideoInfoProperty("Out", transitionOutVideoInfoProperty);
					DrawAdditionalProperties();
					break;
				case SplashSequence.TransitionType.Custom:
					EditorGUILayout.PropertyField(customTransitionTargetProperty, new GUIContent("Target"));
					break;
				default:
					break;
			}

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel = previousIndentLevel;

			void DrawAdditionalProperties() {
				EditorGUILayout.PropertyField(modifyGameObjectOnTransitionProperty, new GUIContent("Modify GameObject"));
			}
		}

		protected virtual void DrawSequenceInspector() {
			int previousIndentLevel = EditorGUI.indentLevel;

			var options = GetSequenceTypes((SplashSequence.TransitionType)transitionTypeProperty.enumValueIndex);
			var index = sequenceTypeProperty.enumValueIndex;
			index = EditorGUILayout.Popup("Sequence", index, options);
			var selectedType = Enum.Parse<SplashSequence.SequenceType>(options[index]);
			sequenceTypeProperty.enumValueIndex = (int)selectedType;

			EditorGUI.indentLevel++;
			switch (selectedType) {
				case SplashSequence.SequenceType.Stay:
					EditorGUILayout.PropertyField(sequenceStayDurationProperty, new GUIContent("Duration"));
					break;
				case SplashSequence.SequenceType.Animator:
					DrawAnimatorInfoProperty("Target", sequenceAnimatorInfoProperty, false);
					break;
				case SplashSequence.SequenceType.Video:
					DrawVideoInfoProperty("Target", sequenceVideoInfoProperty);
					break;
				default:
					break;
			}
			EditorGUI.indentLevel--;

			EditorGUI.indentLevel = previousIndentLevel;
		}

		private static bool DrawCanvasGroupInfoProperty(string label, bool foldout, SerializedProperty property) {
			int previousIndentLevel = EditorGUI.indentLevel;

			bool newFoldout = EditorGUILayout.Foldout(foldout, new GUIContent(label), true);
			if (newFoldout) {
				EditorGUI.indentLevel++;
				var sequenceTypeProperty = property.FindPropertyRelative(nameof(SplashSequence.CanvasGroupInfo.TransitionType));
				EditorGUILayout.PropertyField(sequenceTypeProperty, new GUIContent("Transition Type"));

				switch ((SplashSequence.CanvasGroupInfo.TrType)sequenceTypeProperty.enumValueIndex) {
					case SplashSequence.CanvasGroupInfo.TrType.SetAlpha:
						var alphaTargetProperty = property.FindPropertyRelative(nameof(SplashSequence.CanvasGroupInfo.TargetAlpha));
						EditorGUILayout.PropertyField(alphaTargetProperty, new GUIContent("Target"));
						break;
					case SplashSequence.CanvasGroupInfo.TrType.Fade:
						var fadeCurveProperty = property.FindPropertyRelative(nameof(SplashSequence.CanvasGroupInfo.FadeCurve));
						var fadeDeltaMultiplier = property.FindPropertyRelative(nameof(SplashSequence.CanvasGroupInfo.FadeDeltaMultiplier));

						EditorGUILayout.PropertyField(fadeCurveProperty, new GUIContent("Curve"));
						EditorGUILayout.PropertyField(fadeDeltaMultiplier, new GUIContent("Multiplier"));
						break;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel = previousIndentLevel;
			return newFoldout;
		}

		private static void DrawAnimatorInfoProperty(string label, SerializedProperty property, bool indent) {
			int previousIndentLevel = EditorGUI.indentLevel;

			var animatorProperty = property.FindPropertyRelative(nameof(SplashSequence.AnimatorInfo.Animator));
			EditorGUILayout.PropertyField(animatorProperty, new GUIContent(label), true);

			if (indent) EditorGUI.indentLevel++;
			var animator = animatorProperty.objectReferenceValue as Animator;
			if (animator) {
				var controller = animator.runtimeAnimatorController as AnimatorController;
				if (controller) {
					int layerIndex = 0;
					var overrideLayerIndexProperty = property.FindPropertyRelative(nameof(SplashSequence.AnimatorInfo.OverrideLayerIndex));
					EditorGUILayout.PropertyField(overrideLayerIndexProperty, new GUIContent("Override Layer Index"));
					if (overrideLayerIndexProperty.boolValue) {
						EditorGUI.indentLevel++;

						var overrideLayerIndexValueProperty = property.FindPropertyRelative(nameof(SplashSequence.AnimatorInfo.OverrideLayerIndexValue));
						EditorGUILayout.PropertyField(overrideLayerIndexValueProperty, new GUIContent("Layer Index"));
						layerIndex = Mathf.Clamp(overrideLayerIndexValueProperty.intValue, 0, Mathf.Max(0, controller.layers.Length - 1));
						overrideLayerIndexValueProperty.intValue = layerIndex;

						EditorGUI.indentLevel--;
					}

					var animatorStates = new List<AnimatorState>();
					var stateNames = new List<string>();
					var childStates = controller.layers[layerIndex].stateMachine.states;
					foreach (var childState in childStates) {
						if (animatorStates.Contains(childState.state)) continue;

						animatorStates.Add(childState.state);
						stateNames.Add(childState.state.name);
					}

					if (animatorStates.Count > 0) {
						var entryStateHashProperty = property.FindPropertyRelative(nameof(SplashSequence.AnimatorInfo.EntryStateHash));
						int currentIndex = Mathf.Max(0, animatorStates.FindIndex(state => state.nameHash == entryStateHashProperty.intValue));
						currentIndex = EditorGUILayout.Popup("Entry State", currentIndex, stateNames.ToArray());
						entryStateHashProperty.intValue = animatorStates[currentIndex].nameHash;
					}
				}
			}
			if (indent) EditorGUI.indentLevel--;

			EditorGUI.indentLevel = previousIndentLevel;
		}

		private static void DrawVideoInfoProperty(string label, SerializedProperty property) {
			var videoPlayerProperty = property.FindPropertyRelative(nameof(SplashSequence.VideoInfo.VideoPlayer));
			EditorGUILayout.PropertyField(videoPlayerProperty, new GUIContent(label));
			var videoPlayer = videoPlayerProperty.objectReferenceValue as VideoPlayer;
			if (videoPlayer) {
				var prepareOnSetupProperty = property.FindPropertyRelative(nameof(SplashSequence.VideoInfo.PrepareOnSetup));
				EditorGUILayout.PropertyField(prepareOnSetupProperty, new GUIContent("Prepare on Setup"));
			}
		}

		private static string[] GetSequenceTypes(SplashSequence.TransitionType transitionType) {
			var list = new List<string>();
			var sequenceTypes = Enum.GetValues(typeof(SplashSequence.SequenceType));
			foreach (SplashSequence.SequenceType sequenceType in sequenceTypes) {
				// for filtering unwanted SequenceType based on the TransitionType.
				switch (transitionType) {
					default:
						break;
				}
				list.Add(sequenceType.ToString());
			}
			return list.ToArray();
		}
	}
}