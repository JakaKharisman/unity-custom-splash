using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JK.UnityCustomSplash {
	public class Splash : MonoBehaviour {
		[Serializable]
		internal class GroupInfo {

			static readonly Func<SplashSequence, IEnumerator>[] phases = new Func<SplashSequence, IEnumerator>[] {
				sequence => sequence.TransitionIn(),
				sequence => sequence.Sequence(),
				sequence => sequence.TransitionOut()
			};

			internal Splash splash;
			[SerializeField] internal SplashSequence[] sequences;

			private readonly List<Coroutine> coroutines = new List<Coroutine>();

			internal void Setup() {
			}

			internal IEnumerator Play() {
				foreach (var sequence in sequences) {
					sequence.Prepare();
				}

				coroutines.Clear();
				for (int i = 0; i < sequences.Length; i++) {
					coroutines.Add(splash.StartCoroutine(Play(i)));
				}
				for (int i = 0; i < coroutines.Count; i++) {
					yield return coroutines[i];
				}
			}

			internal void Skip() {
				for (int i = 0; i < sequences.Length; i++) {
					sequences[i].Skip();
				}
			}

			private IEnumerator Play(int index) {
				for (int i = 0; i < phases.Length; i++) {
					yield return phases[i](sequences[index]);
				}
			}
		}

		[SerializeField] internal GroupInfo[] groups;
		[SerializeField] internal bool playOnStart;
		[SerializeField] internal Button skipButton;

		private bool isPlaying;

		private int currentIndex;

		[SerializeField] internal UnityEvent onPlayed;
		[SerializeField] internal UnityEvent onFinished;

		public UnityEvent OnPlayed => onPlayed ??= new UnityEvent();
		public UnityEvent OnFinished => onFinished ??= new UnityEvent();

		protected virtual void Awake() {
			currentIndex = 0;

			foreach (var group in groups) {
				group.splash = this;
			}

			if (skipButton) {
				skipButton.onClick.AddListener(Skip);
			}
		}

		protected virtual void Start() {
			if (playOnStart) Play();
		}

		protected virtual void OnDestroy() {
			onPlayed?.RemoveAllListeners();
			onFinished?.RemoveAllListeners();
			if (skipButton) {
				skipButton.onClick.RemoveListener(Skip);
			}
		}

		public void Play() {
			if (isPlaying) return;

			isPlaying = true;
			StartCoroutine(PlayRoutine());
		}

		public void Skip() {
			if (!isPlaying) return;

			groups[currentIndex].Skip();
		}

		private IEnumerator PlayRoutine() {
			currentIndex = 0;
			for (int i = 0; i < groups.Length; i++) {
				yield return groups[i].Play();
				currentIndex = i;
			}
		}
	}
}
