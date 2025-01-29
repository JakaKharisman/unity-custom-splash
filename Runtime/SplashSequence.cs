using System.Collections;
using UnityEngine;

namespace JK.UnityCustomSplash {
	public class SplashSequence : MonoBehaviour {
		internal enum TransitionType {
			None,
			CanvasGroup,
			Animator,
			Custom,
		}

		internal enum CanvasGroupSequenceType {
			Wait,
		}

		internal enum AnimatorSequenceType {
			Wait,
			Animator,
		}

		internal enum GenericSequenceType {
			Wait,
			Animator,
		}

		[System.Serializable]
		internal class AnimatorInfo {
			public Animator Animator;
			public bool OverrideLayerIndex;
			[Min(0)] public int OverrideLayerIndexValue;

			public int EntryStateHash;

			public int LayerIndex => OverrideLayerIndex ? 0 : OverrideLayerIndexValue;
		}

		const int SEQUENCE_NONE = -1;
		const int SEQUENCE_WAIT = 0;
		const int SEQUENCE_ANIMATION = 1;

		[SerializeField] internal TransitionType transitionType;

		// Transition - Canvas Group

		[SerializeField] internal CanvasGroup canvasGroupTransitionTarget;
		[SerializeField] internal AnimationCurve canvasGroupFadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField, Min(0.1f)] internal float canvasGroupFadeInMultiplier = 1f;
		[SerializeField] internal AnimationCurve canvasGroupFadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
		[SerializeField, Min(0.1f)] internal float canvasGroupFadeOutMultiplier = 1f;
		[SerializeField] internal bool modifyCanvasGroupInteractableOnTransition;
		[SerializeField] internal bool modifyCanvasGroupBlockRaycastOnTransition;

		private float canvasGroupFadeInDuration;
		private float canvasGroupFadeOutDuration;
		private bool previousCanvasGroupInteractable;
		private bool previousCanvasGroupBlockRaycast;

		// Transition - Animator

		[SerializeField] internal AnimatorInfo transitionInAnimatorInfo;
		[SerializeField] internal AnimatorInfo transitionOutAnimatorInfo;

		// Transition - Custom

		[SerializeField] internal SplashSequenceTransition customTransitionTarget;

		[SerializeField] internal bool modifyGameObjectOnTransition = false;

		// Shown only on specific requirement

		[SerializeField] internal CanvasGroupSequenceType canvasGroupSequenceType;
		[SerializeField] internal AnimatorSequenceType animatorSequenceType;
		[SerializeField] internal GenericSequenceType genericSequenceType;

		[SerializeField, Min(0)] internal float waitSequenceDuration = 1;
		[SerializeField] internal AnimatorInfo sequenceAnimatorInfo;

#if UNITY_EDITOR
		[SerializeField, HideInInspector] private bool editorInspectorInit;
#endif

#if UNITY_EDITOR
		/// <summary>
		/// NOTE: Don't forget to wrap this on `#if UNITY_EDITOR` if this is used.
		/// </summary>
		protected virtual void OnValidate() {
			if (!editorInspectorInit) {
				if (TryGetComponent(out canvasGroupTransitionTarget)) SetTransitionTargetType(TransitionType.CanvasGroup);
				if (TryGetComponent<Animator>(out var animator)) {
					transitionInAnimatorInfo ??= new AnimatorInfo();
					transitionOutAnimatorInfo ??= new AnimatorInfo();

					transitionInAnimatorInfo.Animator = animator;
					transitionOutAnimatorInfo.Animator = animator;

					CheckState(transitionInAnimatorInfo, "Transition In");
					CheckState(transitionOutAnimatorInfo, "Transition Out");
					SetTransitionTargetType(TransitionType.Animator);

					void CheckState(AnimatorInfo info, string name) {
						int hash = Animator.StringToHash(name);
						if (animator.HasState(0, hash)) {
							info.EntryStateHash = hash;
							return;
						}
						
						int mergedHash = Animator.StringToHash(name.Replace(" ", string.Empty));
						if (animator.HasState(0, mergedHash)) {
							info.EntryStateHash = mergedHash;
						}
					}
				}

				void SetTransitionTargetType(TransitionType type) {
					if (transitionType != TransitionType.None) return;

					transitionType = type;
				}

				editorInspectorInit = true;
			}

			switch (transitionType) {
				case TransitionType.Custom:
					if (!customTransitionTarget) {
						customTransitionTarget = GetComponent<SplashSequenceTransition>();
					}
					break;
				default:
					break;
			}
		}

		private void Reset() {
			editorInspectorInit = false;
			OnValidate();
		}
#endif

		public virtual void Setup() {
			if (modifyGameObjectOnTransition) gameObject.SetActive(false);

			switch (transitionType) {
				case TransitionType.CanvasGroup:
					canvasGroupFadeInDuration = (canvasGroupFadeInCurve.length > 0) ? canvasGroupFadeInCurve[canvasGroupFadeInCurve.length - 1].time : 0;
					canvasGroupFadeOutDuration = (canvasGroupFadeOutCurve.length > 0) ? canvasGroupFadeOutCurve[canvasGroupFadeOutCurve.length - 1].time : 0;

					previousCanvasGroupInteractable = canvasGroupTransitionTarget.interactable;
					previousCanvasGroupBlockRaycast = canvasGroupTransitionTarget.blocksRaycasts;
					ModifyCanvasGroup(false, false);

					break;
				case TransitionType.Animator:

					break;
			}
		}

		public IEnumerator TransitionIn() {
			if (modifyGameObjectOnTransition) gameObject.SetActive(true);

			switch (transitionType) {
				case TransitionType.CanvasGroup: {
						ModifyCanvasGroup(false, false);

						float elapsedTime = 0f;
						while (elapsedTime < canvasGroupFadeInDuration) {
							elapsedTime += Time.deltaTime * canvasGroupFadeInMultiplier;
							canvasGroupTransitionTarget.alpha = canvasGroupFadeInCurve.Evaluate(elapsedTime / canvasGroupFadeInDuration);
							yield return null;
						}

						ModifyCanvasGroup(previousCanvasGroupInteractable, previousCanvasGroupBlockRaycast);
					}
					break;
				case TransitionType.Animator:
					yield return PlayAnimatorInfo(transitionInAnimatorInfo);
					break;
				case TransitionType.Custom:
					if (customTransitionTarget) {
						yield return customTransitionTarget.In();
					} else {
						Debug.LogWarning("TransitionTarget is not referenced, skipping In...");
					}
					break;
				default:
					break;
			}
		}

		public IEnumerator TransitionOut() {
			switch (transitionType) {
				case TransitionType.CanvasGroup: {
						float elapsedTime = 0f;
						while (elapsedTime < canvasGroupFadeOutDuration) {
							elapsedTime += Time.deltaTime * canvasGroupFadeOutMultiplier;
							canvasGroupTransitionTarget.alpha = canvasGroupFadeOutCurve.Evaluate(elapsedTime / canvasGroupFadeOutDuration);
							yield return null;
						}

						ModifyCanvasGroup(previousCanvasGroupInteractable, previousCanvasGroupBlockRaycast);
					}
					break;
				case TransitionType.Animator:
					yield return PlayAnimatorInfo(transitionOutAnimatorInfo);
					break;
				case TransitionType.Custom:
					if (customTransitionTarget) {
						yield return customTransitionTarget.Out();
					} else {
						Debug.LogWarning("TransitionTarget is not referenced, skipping Out...");
					}
					break;
				default:
					break;
			}

			if (modifyGameObjectOnTransition) gameObject.SetActive(false);
		}

		public IEnumerator Sequence() {
			int type = GetSequenceType();
			switch (type) {
				case SEQUENCE_WAIT:
					yield return new WaitForSeconds(waitSequenceDuration);
					break;
				case SEQUENCE_ANIMATION:
					yield return PlayAnimatorInfo(sequenceAnimatorInfo);
					break;
				default:
					break;
			}
		}

		private void ModifyCanvasGroup(bool interactable, bool blocksRaycasts) {
			if (modifyCanvasGroupInteractableOnTransition) {
				canvasGroupTransitionTarget.interactable = interactable;
			}

			if (modifyCanvasGroupBlockRaycastOnTransition) {
				canvasGroupTransitionTarget.blocksRaycasts = blocksRaycasts;
			}
		}

		private IEnumerator PlayAnimatorInfo(AnimatorInfo info) {
			info.Animator.Play(info.EntryStateHash, info.LayerIndex);
			while (true) {
				yield return null;
				var stateInfo = info.Animator.GetCurrentAnimatorStateInfo(info.LayerIndex);
				bool inTransition = info.Animator.IsInTransition(info.LayerIndex);
				if (!inTransition && stateInfo.normalizedTime >= 1f) break;
			}
		}

		int GetSequenceType() {
			int type = SEQUENCE_NONE;
			switch (transitionType) {
				case TransitionType.CanvasGroup:
					switch (canvasGroupSequenceType) {
						case CanvasGroupSequenceType.Wait:
							type = SEQUENCE_WAIT;
							break;
					}
					break;
				case TransitionType.Animator:
					switch (animatorSequenceType) {
						case AnimatorSequenceType.Wait:
							type = SEQUENCE_WAIT;
							break;
						case AnimatorSequenceType.Animator:
							type = SEQUENCE_ANIMATION;
							break;
					}
					break;
				default:
					switch (genericSequenceType) {
						case GenericSequenceType.Wait:
							type = SEQUENCE_WAIT;
							break;
						case GenericSequenceType.Animator:
							type = SEQUENCE_ANIMATION;
							break;
					}
					break;
			}
			return type;
		}
	}
}
