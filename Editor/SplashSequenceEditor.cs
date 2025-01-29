using JK.UnityCustomSplashEditor;
using JK.UnityCustomSplash;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JK.UnitySplashExtendedEditor {
	[CustomEditor(typeof(SplashSequence), true)]
	public class SplashSequenceEditor : Editor {
		private SerializedProperty transitionTypeProperty;
		private SerializedProperty modifyGameObjectOnTransitionProperty;

		private SerializedProperty canvasGroupTransitionTargetProperty;
		private SerializedProperty canvasGroupFadeInCurveProperty;
		private SerializedProperty canvasGroupFadeInMultiplierProperty;
		private SerializedProperty canvasGroupFadeOutCurveProperty;
		private SerializedProperty canvasGroupFadeOutMultiplierProperty;
		private SerializedProperty modifyCanvasGroupInteractableOnTransitionProperty;
		private SerializedProperty modifyCanvasGroupBlockRaycastOnTransitionProperty;

		private SerializedProperty transitionInAnimatorInfoProperty;
		private SerializedProperty transitionOutAnimatorInfoProperty;

		private SerializedProperty transitionInAnimationInfoProperty;

		private SerializedProperty customTransitionTargetProperty;

		private SerializedProperty canvasGroupSequenceTypeProperty;
		private SerializedProperty animatorSequenceTypeProperty;
		private SerializedProperty genericSequenceTypeProperty;

		private SerializedProperty waitSequenceDurationProperty;
		private SerializedProperty sequenceAnimatorInfo;

		protected Type currentType;
		protected Type baseType;

		private bool inFoldout = true;
		private bool outFoldout = true;

		protected virtual bool DrawInspectorOfCurrentType => true;

		private void OnEnable() {
			currentType = target.GetType();
			baseType = typeof(SplashSequence);

			transitionTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionType));
			modifyGameObjectOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyGameObjectOnTransition));

			canvasGroupTransitionTargetProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupTransitionTarget));
			canvasGroupFadeInCurveProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupFadeInCurve));
			canvasGroupFadeInMultiplierProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupFadeInMultiplier));
			canvasGroupFadeOutCurveProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupFadeOutCurve));
			canvasGroupFadeOutMultiplierProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupFadeOutMultiplier));
			modifyCanvasGroupInteractableOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyCanvasGroupInteractableOnTransition));
			modifyCanvasGroupBlockRaycastOnTransitionProperty = serializedObject.FindProperty(nameof(SplashSequence.modifyCanvasGroupBlockRaycastOnTransition));

			transitionInAnimatorInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionInAnimatorInfo));
			transitionOutAnimatorInfoProperty = serializedObject.FindProperty(nameof(SplashSequence.transitionOutAnimatorInfo));

			customTransitionTargetProperty = serializedObject.FindProperty(nameof(SplashSequence.customTransitionTarget));

			canvasGroupSequenceTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.canvasGroupSequenceType));
			animatorSequenceTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.animatorSequenceType));
			genericSequenceTypeProperty = serializedObject.FindProperty(nameof(SplashSequence.genericSequenceType));

			waitSequenceDurationProperty = serializedObject.FindProperty(nameof(SplashSequence.waitSequenceDuration));
			sequenceAnimatorInfo = serializedObject.FindProperty(nameof(SplashSequence.sequenceAnimatorInfo));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			if (currentType != baseType) target.DrawInspectorScriptReference();
			DrawTransitionInspector();
			DrawSequenceInspector();

			if (DrawInspectorOfCurrentType) {
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
					DrawCanvasGroup();
					break;
				case SplashSequence.TransitionType.Animator:
					DrawAnimator();
					break;
				case SplashSequence.TransitionType.Custom:
					EditorGUILayout.PropertyField(customTransitionTargetProperty, new GUIContent("Target"));
					break;
				default:
					break;
			}

			EditorGUILayout.PropertyField(modifyGameObjectOnTransitionProperty, new GUIContent("Modify Game Object"));

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel = previousIndentLevel;

			void DrawCanvasGroup() {
				int previousIndentLevel = EditorGUI.indentLevel;

				EditorGUILayout.PropertyField(canvasGroupTransitionTargetProperty, new GUIContent("Target"));

				if (canvasGroupTransitionTargetProperty.objectReferenceValue) {
					inFoldout = Foldout("In", inFoldout, canvasGroupFadeInCurveProperty, canvasGroupFadeInMultiplierProperty);
					outFoldout = Foldout("Out", outFoldout, canvasGroupFadeOutCurveProperty, canvasGroupFadeOutMultiplierProperty);

					EditorGUILayout.PropertyField(modifyCanvasGroupBlockRaycastOnTransitionProperty, new GUIContent("Blocks Raycasts", "If true, CanvasGroup.blocksRaycasts will be modified during transition."));
					EditorGUILayout.PropertyField(modifyCanvasGroupInteractableOnTransitionProperty, new GUIContent("Interactable", "If true, CanvasGroup.interactable will be modified during transition."));
				}

				EditorGUI.indentLevel = previousIndentLevel;

				bool Foldout(string name, bool value, SerializedProperty curve, SerializedProperty multiplier) {
					bool newValue = EditorGUILayout.Foldout(value, new GUIContent(name), true);
					if (newValue) {
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(curve, new GUIContent("Curve"));
						EditorGUILayout.PropertyField(multiplier, new GUIContent("Multiplier"));
						EditorGUI.indentLevel--;
					}
					return newValue;
				}
			}

			void DrawAnimator() {
				int previousIndentLevel = EditorGUI.indentLevel;

				inFoldout = Foldout("In", inFoldout, transitionInAnimatorInfoProperty);
				outFoldout = Foldout("Out", outFoldout, transitionOutAnimatorInfoProperty);

				EditorGUI.indentLevel = previousIndentLevel;

				bool Foldout(string name, bool value, SerializedProperty property) {
					bool newValue = EditorGUILayout.Foldout(value, new GUIContent(name), true);
					if (newValue) {
						EditorGUI.indentLevel++;
						DrawAnimatorInfoProperty(property);
						EditorGUI.indentLevel--;
					}
					return newValue;
				}
			}
		}

		protected virtual void DrawSequenceInspector() {
			var sequenceLabel = new GUIContent("Sequence");

			var transitionType = (SplashSequence.TransitionType)transitionTypeProperty.enumValueIndex;
			switch (transitionType) {
				case SplashSequence.TransitionType.CanvasGroup:
					DrawCanvasGroup();
					break;
				case SplashSequence.TransitionType.Animator:
					DrawAnimator();
					break;
				default:
					DrawGeneric();
					break;
			}

			void DrawCanvasGroup() {
				EditorGUILayout.PropertyField(canvasGroupSequenceTypeProperty, sequenceLabel);

				int previousIndentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel++;

				var sequenceType = (SplashSequence.CanvasGroupSequenceType)(canvasGroupSequenceTypeProperty.enumValueIndex);
				switch (sequenceType) {
					case SplashSequence.CanvasGroupSequenceType.Wait:
						DrawWaitProperties();
						break;
					default:
						break;
				}

				EditorGUI.indentLevel--;
				EditorGUI.indentLevel = previousIndentLevel;
			}

			void DrawAnimator() {
				EditorGUILayout.PropertyField(animatorSequenceTypeProperty, sequenceLabel);

				int previousIndentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel++;

				var sequenceType = (SplashSequence.AnimatorSequenceType)(animatorSequenceTypeProperty.enumValueIndex);
				switch (sequenceType) {
					case SplashSequence.AnimatorSequenceType.Wait:
						DrawWaitProperties();
						break;
					case SplashSequence.AnimatorSequenceType.Animator:
						DrawAnimatorProperties();
						break;
					default:
						break;
				}

				EditorGUI.indentLevel--;
				EditorGUI.indentLevel = previousIndentLevel;
			}

			void DrawGeneric() {
				EditorGUILayout.PropertyField(genericSequenceTypeProperty, sequenceLabel);

				int previousIndentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel++;

				var sequenceType = (SplashSequence.GenericSequenceType)(genericSequenceTypeProperty.enumValueIndex);
				switch (sequenceType) {
					case SplashSequence.GenericSequenceType.Wait:
						DrawWaitProperties();
						break;
					case SplashSequence.GenericSequenceType.Animator:
						DrawAnimatorProperties();
						break;
					default:
						break;
				}

				EditorGUI.indentLevel--;
				EditorGUI.indentLevel = previousIndentLevel;
			}

			void DrawWaitProperties() {
				EditorGUILayout.PropertyField(waitSequenceDurationProperty, new GUIContent("Duration"));
			}

			void DrawAnimatorProperties() {
				DrawAnimatorInfoProperty(sequenceAnimatorInfo);
			}
		}

		private static void DrawAnimatorInfoProperty(SerializedProperty property) {
			int previousIndentLevel = EditorGUI.indentLevel;

			var animatorProperty = property.FindPropertyRelative(nameof(SplashSequence.AnimatorInfo.Animator));
			EditorGUILayout.PropertyField(animatorProperty, new GUIContent("Animator"), true);
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

			EditorGUI.indentLevel = previousIndentLevel;
		}
	}
}