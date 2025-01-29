using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace JK.UnityCustomSplash {
	public class Splash : MonoBehaviour {
		[Serializable]
		internal class SequenceInfo {
			public bool WaitUntilFinished = true;
			public SplashSequence Sequence;
		}

		[SerializeField] internal SequenceInfo[] _sequenceInfos;
		[SerializeField] internal bool playOnStart;

		private readonly List<SequenceInfo> sequenceInfos = new List<SequenceInfo>();

		private int currentIndex;
		private List<SequenceInfo> groupSequenceInfos = new List<SequenceInfo>();
		private readonly List<Coroutine> coroutines = new List<Coroutine>();

		private Coroutine sequenceRoutine;
		private Coroutine transitionRoutine;

		private bool isPlaying;
		private bool isFinished;

		[SerializeField] internal UnityEvent onPlayed;
		[SerializeField] internal UnityEvent onFinished;

		public UnityEvent OnPlayed => onPlayed ??= new UnityEvent();
		public UnityEvent OnFinished => onFinished ??= new UnityEvent();

		protected virtual void Awake() {
			currentIndex = 0;
			isFinished = false;

			sequenceInfos.Clear();
			if (_sequenceInfos != null && _sequenceInfos.Length > 0) {
				sequenceInfos.AddRange(_sequenceInfos.Where(reference => reference.Sequence != null));
				foreach (var reference in sequenceInfos) {
					reference.Sequence.Setup();
				}
			}
			groupSequenceInfos.Clear();
		}

		protected virtual void Start() {
			if (playOnStart) Play();
		}

		protected virtual void OnDestroy() {
			onPlayed?.RemoveAllListeners();
			onFinished?.RemoveAllListeners();
		}

		private void End() {
			if (!isPlaying || isFinished) return;

			isPlaying = false;
			isFinished = true;
			OnFinished?.Invoke();
		}

		private void SkipCurrent() {
			foreach (var coroutine in coroutines) {
				StopCoroutine(coroutine);
			}

			StopCoroutine(sequenceRoutine);
			sequenceRoutine = null;
		}

		private bool Continue() {
			if (isFinished) return false;
			if (currentIndex >= sequenceInfos.Count) return false;

			int cacheIndex = currentIndex;
			for (int i = currentIndex; i < sequenceInfos.Count; i++) {
				cacheIndex = i;
				if (sequenceInfos[i].WaitUntilFinished) {
					break;
				}
			}

			if (cacheIndex != currentIndex) {
				groupSequenceInfos = sequenceInfos.GetRange(currentIndex, cacheIndex - currentIndex + 1);
			} else {
				groupSequenceInfos.Clear();
			}
			transitionRoutine = StartCoroutine(StartInTransition());
			return true;
		}

		private IEnumerator StartInTransition() {
			if (sequenceRoutine != null) yield break;

			if (groupSequenceInfos.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupSequenceInfos) {
					coroutines.Add(StartCoroutine(reference.Sequence.TransitionIn()));
				}
				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
			} else {
				yield return sequenceInfos[currentIndex].Sequence.TransitionIn();
			}

			transitionRoutine = null;
			sequenceRoutine = StartCoroutine(StartSequence());
		}

		private IEnumerator StartTransitionOut() {
			if (sequenceRoutine != null) yield break;

			if (groupSequenceInfos.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupSequenceInfos) {
					coroutines.Add(StartCoroutine(reference.Sequence.TransitionOut()));
				}

				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
				currentIndex = currentIndex + coroutines.Count;
			} else {
				yield return sequenceInfos[currentIndex].Sequence.TransitionOut();
				currentIndex++;
			}

			transitionRoutine = null;
			if (!Continue()) End();
		}

		private IEnumerator StartSequence() {
			if (transitionRoutine != null) yield break;

			if (groupSequenceInfos.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupSequenceInfos) {
					coroutines.Add(StartCoroutine(reference.Sequence.Sequence()));
				}

				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
			} else {
				yield return sequenceInfos[currentIndex].Sequence.Sequence();
			}

			sequenceRoutine = null;
			transitionRoutine = StartCoroutine(StartTransitionOut());
		}

		public WaitUntil Wait() {
			return new WaitUntil(() => isFinished);
		}

		public void Play() {
			if (isPlaying) return;

			isPlaying = true;
			isFinished = false;
			Continue();
			OnPlayed?.Invoke();
		}

		public void Skip() {
			if (isFinished) return;
			if (transitionRoutine != null || sequenceRoutine == null) return;
			
			SkipCurrent();
			transitionRoutine = StartCoroutine(StartTransitionOut());
		}

		public void SkipAll() {
			if (isFinished) return;

			SkipCurrent();
			currentIndex = sequenceInfos.Count;
			transitionRoutine = StartCoroutine(StartTransitionOut());
		}
	}
}
