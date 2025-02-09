using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace JK.UnityCustomSplash {
	public class SplashSequence : MonoBehaviour {

		static readonly Keyframe[] DEFAULT_SEQUENCE_KEYFRAMES = new Keyframe[] {
			new Keyframe(0, 0f, 0, 0),
			new Keyframe(1, 1, 0, 0),
			new Keyframe(3, 1, 0, 0),
			new Keyframe(4, 0f, 0, 0),
		};

		internal enum SequenceType : byte {
			CanvasGroup,
			Animator,
			Video,
		}

		internal enum StateDuringSequenceType {
			Ignore,
			True,
			False,
		}

		// CanvasGroup
		[SerializeField] internal CanvasGroup canvasGroup;
		[SerializeField] internal AnimationCurve sequenceCurve = new AnimationCurve(DEFAULT_SEQUENCE_KEYFRAMES);
		[SerializeField] internal StateDuringSequenceType interactableDuringSequence;
		[SerializeField] internal StateDuringSequenceType blocksRaycastsDuringSequence;

		// Animator
		[SerializeField] internal Animator animator;
		[SerializeField] internal int layerIndex;
		[SerializeField] internal int animatorStartStateHash;
		[SerializeField] internal int animatorEndStateHash = int.MinValue;

		// Video
		[SerializeField] internal VideoPlayer videoPlayer;

		[SerializeField] internal SequenceType sequenceType;
		[SerializeField] internal bool inactiveBeforeSequence = true;
		[SerializeField] internal bool inactiveAfterSequence = true;
		[SerializeField] internal bool skippable = true;

		private bool isPlaying;
		private bool skipped;

		private void Awake() {
			if (inactiveBeforeSequence) gameObject.SetActive(false);

			if (videoPlayer) {
				videoPlayer.playOnAwake = false;
				videoPlayer.isLooping = false;
			}
		}

#if UNITY_EDITOR
		protected virtual void OnValidate() {
			if (!canvasGroup) {
				if (TryGetComponent(out canvasGroup)) {
					sequenceType = SequenceType.CanvasGroup;
				}
			}

			if (!animator) {
				if (TryGetComponent(out animator)) {
					sequenceType = SequenceType.Animator;
				}
			}

			if (!videoPlayer) {
				if (TryGetComponent(out videoPlayer)) {
					sequenceType = SequenceType.Video;
				}
			}
		}

		protected virtual void Reset() {
			OnValidate();
		}
#endif


		public IEnumerator Play() {
			isPlaying = true;
			if (inactiveBeforeSequence) gameObject.SetActive(true);
			yield return OnSequence();
			if (inactiveAfterSequence) gameObject.SetActive(false);
			isPlaying = false;
		}

		protected virtual IEnumerator OnSequence() {
			skipped = false;
			switch (sequenceType) {
				case SequenceType.CanvasGroup:
					bool previousInteractable = canvasGroup.interactable;
					bool previousBlocksRaycasts = canvasGroup.blocksRaycasts;

					canvasGroup.interactable = interactableDuringSequence switch {
						StateDuringSequenceType.False => false,
						StateDuringSequenceType.True => true,
						_ => previousInteractable,
					};

					canvasGroup.blocksRaycasts = blocksRaycastsDuringSequence switch {
						StateDuringSequenceType.False => false,
						StateDuringSequenceType.True => true,
						_ => previousBlocksRaycasts,
					};

					switch (interactableDuringSequence) {
						case StateDuringSequenceType.True:
							canvasGroup.interactable = true;
							break;
						case StateDuringSequenceType.False:
							canvasGroup.interactable = false;
							break;
						case StateDuringSequenceType.Ignore:
							break;
					}
					var duration = (sequenceCurve.length > 0) ? sequenceCurve[sequenceCurve.length - 1].time : 0;
					var elapsedTime = 0f;
					while (elapsedTime < duration) {
						if (skipped) break;

						elapsedTime += Time.deltaTime;
						canvasGroup.alpha = sequenceCurve.Evaluate(Mathf.Lerp(0, duration, elapsedTime / duration));
						yield return null;
					}

					canvasGroup.alpha = (sequenceCurve.length > 0) ? sequenceCurve[sequenceCurve.length - 1].value : 0;
					canvasGroup.interactable = previousInteractable;
					canvasGroup.blocksRaycasts = previousBlocksRaycasts;
					break;
				case SequenceType.Animator:
					animator.Play(animatorStartStateHash, layerIndex, 0);
					while (true) {
						if (skipped) {
							animator.StopPlayback();
							break;
						}

						yield return null;
						var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
						bool inTransition = animator.IsInTransition(layerIndex);
						if (!inTransition && (stateInfo.normalizedTime >= 1f || (animatorEndStateHash != int.MinValue && stateInfo.fullPathHash == animatorEndStateHash))) break;
					}
					break;
				case SequenceType.Video:
					if (!videoPlayer.isPrepared) {
						videoPlayer.Prepare();
						while (!videoPlayer.isPrepared) {
							yield return null;
						}
					}

					videoPlayer.Play();
					while (videoPlayer.isPlaying) {
						if (skipped) {
							videoPlayer.time = videoPlayer.length;
							break;
						}
						yield return null;
					}
					break;
				default:
					break;
			}
		}

		public void Skip() {
			if (skipped) return;
			if (!isPlaying) return;
			if (!skippable) return;

			skipped = true;
		}
	}
}
