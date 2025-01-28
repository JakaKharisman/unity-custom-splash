using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace JK.UnityCustomSplash {
	public class Splash : MonoBehaviour {
		[Serializable]
		internal class SplashReference {
			public int Priority;
			public bool WaitUntilFinished = true;
			public SplashSequence Sequence;
		}

		[SerializeField] internal SplashReference[] _sequenceReferences;
		[SerializeField] internal bool playOnStart;

		private readonly List<SplashReference> references = new List<SplashReference>();

		private int currentIndex;
		private List<SplashReference> groupReferences = new List<SplashReference>();
		private readonly List<Coroutine> coroutines = new List<Coroutine>();

		private Coroutine sequenceRoutine;
		private Coroutine transitionRoutine;

		private bool isPlaying;
		private bool isFinished;

		[SerializeField] internal UnityEvent onPlayed;
		[SerializeField] internal UnityEvent onFinished;

		public UnityEvent OnPlayed => onPlayed;
		public UnityEvent OnFinished => onFinished;

		protected virtual void Awake() {
			currentIndex = 0;
			isFinished = false;

			references.Clear();
			if (_sequenceReferences != null && _sequenceReferences.Length > 0) {
				references.AddRange(_sequenceReferences.Where(reference => reference.Sequence != null));
				references.Sort((l, r) => (l.Priority < r.Priority) ? -1 : (l.Priority > r.Priority ? 1 : 0));
				foreach (var reference in references) {
					reference.Sequence.Setup();
				}
			}
			groupReferences.Clear();
		}

		protected virtual void Start() {
			if (playOnStart) Play();
		}

		protected virtual void OnDestroy() {

		}

		private void End() {
			if (!isFinished) return;

			isPlaying = false;
			isFinished = true;
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
			if (currentIndex >= references.Count) return false;

			int cacheIndex = currentIndex;
			for (int i = currentIndex; i < references.Count; i++) {
				cacheIndex = i;
				if (references[i].WaitUntilFinished) {
					break;
				}
			}

			if (cacheIndex != currentIndex) {
				groupReferences = references.GetRange(currentIndex, cacheIndex - currentIndex + 1);
			} else {
				groupReferences.Clear();
			}
			transitionRoutine = StartCoroutine(DoIn());
			return true;
		}

		private IEnumerator DoIn() {
			if (sequenceRoutine != null) yield break;

			if (groupReferences.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupReferences) {
					coroutines.Add(StartCoroutine(reference.Sequence.TransitionIn()));
				}
				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
			} else {
				yield return references[currentIndex].Sequence.TransitionIn();
			}

			transitionRoutine = null;
			sequenceRoutine = StartCoroutine(DoSequence());
		}

		private IEnumerator DoOut() {
			if (sequenceRoutine != null) yield break;

			if (groupReferences.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupReferences) {
					coroutines.Add(StartCoroutine(reference.Sequence.TransitionOut()));
				}

				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
				currentIndex = currentIndex + coroutines.Count;
			} else {
				yield return references[currentIndex].Sequence.TransitionOut();
				currentIndex++;
			}

			transitionRoutine = null;

			if (!Continue()) End();
		}

		private IEnumerator DoSequence() {
			if (transitionRoutine != null) yield break;

			if (groupReferences.Count > 0) {
				coroutines.Clear();
				foreach (var reference in groupReferences) {
					coroutines.Add(StartCoroutine(reference.Sequence.Sequence()));
				}

				foreach (var coroutine in coroutines) {
					yield return coroutine;
				}
			} else {
				yield return references[currentIndex].Sequence.Sequence();
			}

			sequenceRoutine = null;
			transitionRoutine = StartCoroutine(DoOut());
		}

		public WaitUntil Wait() {
			return new WaitUntil(() => isFinished);
		}

		public void Play() {
			if (isPlaying) return;

			isPlaying = true;
			isFinished = false;
			Continue();
		}

		public void Skip() {
			if (isFinished) return;
			if (transitionRoutine != null || sequenceRoutine == null) return;
			
			SkipCurrent();
			transitionRoutine = StartCoroutine(DoOut());
		}

		public void SkipAll() {
			if (isFinished) return;

			SkipCurrent();
			currentIndex = references.Count;
			transitionRoutine = StartCoroutine(DoOut());
		}
	}
}
