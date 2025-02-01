using System.Collections;
using UnityEngine;

#if ENABLE_VIDEO
using UnityEngine.Video;
#endif

namespace JK.UnityCustomSplash {
	public class SplashSequence : MonoBehaviour {
		internal enum TransitionType {
			None = 0,
			CanvasGroup = 1,
			Animator = 2,
#if ENABLE_VIDEO
			Video = 3,
#endif
			Custom = 999,
		}

		internal enum SequenceType {
			Stay = 0,
			Animator = 1,
#if ENABLE_VIDEO
			Video = 2,
#endif
		}

		private enum InternalState {
			Ready,
			TransitionIn,
			Sequence,
			TransitionOut,
		}

		[System.Serializable]
		internal class CanvasGroupInfo {
			public enum TrType {
				SetAlpha,
				Fade,
			}

			private bool running;
			private bool skipped;
			private float duration;

			public TrType TransitionType;
			public bool Skippable;

			[Range(0, 1)] public float TargetAlpha;

			public AnimationCurve FadeCurve;
			[Min(0.1f)] public float FadeDeltaMultiplier = 1f;

			internal void Prepare() {
				duration = (FadeCurve.length > 0) ? FadeCurve[FadeCurve.length - 1].time : 0;
			}

			internal IEnumerator Do(CanvasGroup target) {
				running = true;
				skipped = false;

				switch (TransitionType) {
					case TrType.SetAlpha:
						target.alpha = TargetAlpha;
						break;
					case TrType.Fade:
						float elapsedTime = 0;
						while (elapsedTime < duration) {
							if (skipped) {
								break;
							}
							elapsedTime += Time.deltaTime * FadeDeltaMultiplier;
							target.alpha = FadeCurve.Evaluate(Mathf.Lerp(0, duration, elapsedTime / duration));
							yield return null;
						}
						target.alpha = FadeCurve.Evaluate(duration);
						break;
				}

				running = false;
			}

			internal void Skip() {
				if (!running) return;
				if (!Skippable) return;

				skipped = true;
			}

			internal static CanvasGroupInfo In() {
				return new CanvasGroupInfo() {
					FadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
					TargetAlpha = 1,
				};
			}

			internal static CanvasGroupInfo Out() {
				return new CanvasGroupInfo() {
					FadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0),
					TargetAlpha = 0,
				};
			}
		}

		[System.Serializable]
		internal class AnimatorInfo {
			private bool running;
			private bool skipped;

			public bool Skippable;
			public Animator Animator;
			public bool OverrideLayerIndex;
			[Min(0)] public int OverrideLayerIndexValue;

			public int EntryStateHash;
			
			public int LayerIndex => OverrideLayerIndex ? 0 : OverrideLayerIndexValue;

			internal IEnumerator Play() {
				running = true;
				skipped = false;
				Animator.Play(EntryStateHash, LayerIndex, 0);
				while (true) {
					if (skipped) {
						Animator.Play(EntryStateHash, LayerIndex, 1f);
						break;
					}

					yield return null;
					var stateInfo = Animator.GetCurrentAnimatorStateInfo(LayerIndex);
					bool inTransition = Animator.IsInTransition(LayerIndex);
					if (!inTransition && stateInfo.normalizedTime >= 1f) break;
				}
				running = false;
			}

			internal void Skip() {
				if (!running) return;
				if (!Skippable) return;

				skipped = true;
			}
		}

#if ENABLE_VIDEO
		[System.Serializable]
		internal class VideoInfo {
			private readonly bool skippable;

			private bool skipped;
			private bool running;

			public VideoPlayer VideoPlayer;
			public bool PrepareOnSetup;

			public VideoInfo(bool skippable) {
				this.skippable = skippable;
			}

			internal void Prepare() {
				if (VideoPlayer) {
					VideoPlayer.playOnAwake = false;
					VideoPlayer.isLooping = false;

					if (PrepareOnSetup) {
						VideoPlayer.Prepare();
					}
				}
			}

			internal IEnumerator Play() {
				skipped = false;
				running = true;
				if (!VideoPlayer.isPrepared) VideoPlayer.Prepare();
				yield return new WaitUntil(() => VideoPlayer.isPrepared);

				VideoPlayer.Play();
				yield return null;
				while (VideoPlayer.frame == 0 || VideoPlayer.isPlaying) {
					if (skipped) {
						VideoPlayer.time = VideoPlayer.length;
						break;
					}
					yield return null;
				}
				running = false;
			}

			internal void Skip() {
				if (!running) return;

				skipped = true;
			}
		}
#endif

		[SerializeField] internal TransitionType transitionType;

		// Transition - Canvas Group

		[SerializeField] internal CanvasGroup canvasGroupTransitionTarget;
		[SerializeField] internal CanvasGroupInfo transitionInCanvasGroupInfo = CanvasGroupInfo.In();
		[SerializeField] internal CanvasGroupInfo transitionOutCanvasGroupInfo = CanvasGroupInfo.Out();
		[SerializeField] internal bool modifyCanvasGroupInteractableOnTransition;
		[SerializeField] internal bool modifyCanvasGroupBlockRaycastOnTransition;

		private bool previousCanvasGroupInteractable;
		private bool previousCanvasGroupBlockRaycast;

		// Transition - Animator

		[SerializeField] internal AnimatorInfo transitionInAnimatorInfo;
		[SerializeField] internal AnimatorInfo transitionOutAnimatorInfo;

		// Transition - Video

#if ENABLE_VIDEO
		[SerializeField] internal VideoInfo transitionInVideoInfo;
		[SerializeField] internal VideoInfo transitionOutVideoInfo;
#endif

		// Transition - Custom

		[SerializeField] internal SplashSequenceTransition customTransitionTarget;

		[SerializeField] internal bool modifyGameObjectOnTransition = false;

		// Sequence
		[SerializeField] internal SequenceType sequenceType;
		[SerializeField, Min(0)] internal float sequenceStayDuration = 1;
		[SerializeField] internal AnimatorInfo sequenceAnimatorInfo;
		
#if ENABLE_VIDEO
		[SerializeField] internal VideoInfo sequenceVideoInfo;
#endif

#if UNITY_EDITOR
		[SerializeField, HideInInspector] private bool editorInspectorInit;
#endif

		private InternalState state;
		private bool skipped;

#if UNITY_EDITOR
		/// <summary>
		/// NOTE: Don't forget to wrap this on `#if UNITY_EDITOR` if this is used.
		/// </summary>
		protected virtual void OnValidate() {
			if (!editorInspectorInit) {
				Animator animator = null;
				if (TryGetComponent(out canvasGroupTransitionTarget)) {
					SetTransitionTargetType(TransitionType.CanvasGroup);
				} else if (TryGetComponent<Animator>(out animator)) {
					transitionInAnimatorInfo ??= new AnimatorInfo();
					transitionOutAnimatorInfo ??= new AnimatorInfo();

					transitionInAnimatorInfo.Animator = animator;
					transitionOutAnimatorInfo.Animator = animator;

					CheckAnimatorStates(animator, transitionInAnimatorInfo, "Transition In");
					CheckAnimatorStates(animator, transitionOutAnimatorInfo, "Transition Out");
					SetTransitionTargetType(TransitionType.Animator);
				}

				if (animator) {
					sequenceType = SequenceType.Animator;
					sequenceAnimatorInfo ??= new AnimatorInfo();
					sequenceAnimatorInfo.Animator = animator;

					CheckAnimatorStates(animator, sequenceAnimatorInfo, "Sequence", "Sequence Entry", "Sequence Start");
				}

				void SetTransitionTargetType(TransitionType type) {
					if (transitionType != TransitionType.None) return;

					transitionType = type;
				}

				bool CheckAnimatorStates(Animator animator, AnimatorInfo info, params string[] names) {
					for (int i = 0; i < names.Length; i++) {
						int hash = Animator.StringToHash(names[i]);
						if (animator.HasState(0, hash)) {
							info.EntryStateHash = hash;
							return true;
						}

						int mergedHash = Animator.StringToHash(names[i].Replace(" ", string.Empty));
						if (animator.HasState(0, mergedHash)) {
							info.EntryStateHash = mergedHash;
							return true;
						}
					}
					return false;
				}

				editorInspectorInit = true;
			}

			switch (transitionType) {
#if ENABLE_VIDEO
				case TransitionType.Video:
					if (!sequenceVideoInfo.VideoPlayer) {
						sequenceVideoInfo.VideoPlayer = GetComponent<VideoPlayer>();
					}
					break;
#endif
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

		public virtual void Prepare() {
			if (modifyGameObjectOnTransition) gameObject.SetActive(false);

			switch (transitionType) {
				case TransitionType.CanvasGroup:
					transitionInCanvasGroupInfo.Prepare();
					transitionOutCanvasGroupInfo.Prepare();

					previousCanvasGroupInteractable = canvasGroupTransitionTarget.interactable;
					previousCanvasGroupBlockRaycast = canvasGroupTransitionTarget.blocksRaycasts;
					ModifyCanvasGroup(false, false);

					break;
#if ENABLE_VIDEO
				case TransitionType.Video:
					transitionInVideoInfo.Prepare();
					transitionOutVideoInfo.Prepare();
					sequenceVideoInfo.Prepare();
					break;
#endif
				default:
					break;
			}

			state = InternalState.Ready;
			skipped = false;
		}

		public virtual IEnumerator TransitionIn() {
			if (state != InternalState.Ready) yield break;
			state = InternalState.TransitionIn;

			skipped = false;

			if (modifyGameObjectOnTransition) gameObject.SetActive(true);

			switch (transitionType) {
				case TransitionType.CanvasGroup:
					ModifyCanvasGroup(false, false);
					yield return transitionInCanvasGroupInfo.Do(canvasGroupTransitionTarget);
					ModifyCanvasGroup(previousCanvasGroupInteractable, previousCanvasGroupBlockRaycast);
					break;
				case TransitionType.Animator:
					yield return transitionInAnimatorInfo.Play();
					break;
#if ENABLE_VIDEO
				case TransitionType.Video:
					yield return transitionInVideoInfo.Play();
					break;
#endif
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

			state = InternalState.Ready;
		}

		public virtual IEnumerator TransitionOut() {
			if (state != InternalState.Ready) yield break;
			state = InternalState.TransitionOut;

			switch (transitionType) {
				case TransitionType.CanvasGroup:
					yield return transitionOutCanvasGroupInfo.Do(canvasGroupTransitionTarget);
					ModifyCanvasGroup(previousCanvasGroupInteractable, previousCanvasGroupBlockRaycast);
					break;
				case TransitionType.Animator:
					yield return transitionOutAnimatorInfo.Play();
					break;
#if ENABLE_VIDEO
				case TransitionType.Video:
					yield return transitionOutVideoInfo.Play();
					break;
#endif
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
			state = InternalState.Ready;
		}

		public virtual IEnumerator Sequence() {
			if (state != InternalState.Ready) yield break;
			state = InternalState.Sequence;

			skipped = false;

			switch (sequenceType) {
				case SequenceType.Stay:
					float elapsedTime = 0;
					while (elapsedTime < sequenceStayDuration) {
						yield return null;
						elapsedTime += Time.deltaTime;
						if (skipped) {
							break;
						}
					}
					break;
				case SequenceType.Animator:
					yield return sequenceAnimatorInfo.Play();
					break;
#if ENABLE_VIDEO
				case SequenceType.Video:
					yield return sequenceVideoInfo.Play();
					break;
#endif
				default:
					break;
			}

			state = InternalState.Ready;
		}

		public void Skip() {
			switch (state) {
				case InternalState.TransitionIn:
					skipped = true;
					transitionInAnimatorInfo.Skip();

					break;
				case InternalState.Sequence:
					skipped = true;
					switch (sequenceType) {
						case SequenceType.Animator:
							sequenceAnimatorInfo.Skip();
							break;
#if ENABLE_VIDEO
						case SequenceType.Video:
							sequenceVideoInfo.Skip();
							break;
#endif
					}
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
	}
}
