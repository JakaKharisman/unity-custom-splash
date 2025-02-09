using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JK.UnityCustomSplash {
	public class Splash : MonoBehaviour {
		[Serializable]
		internal class SequenceGroup {
			internal MonoBehaviour runner;
			[SerializeField] internal List<SplashSequence> sequences = new List<SplashSequence>() { null };

			public bool Evaluate() {
				int index = 0;
				while (index < sequences.Count) {
					if (!sequences[index]) {
						sequences.RemoveAt(index);
						continue;
					}
					index++;
				}
				return index > 0;
			}

			public IEnumerator DoSequence() {
				var coroutines = new List<Coroutine>();
				for (int i = 0; i < sequences.Count; i++) {
					coroutines.Add(runner.StartCoroutine(sequences[i].Play()));
				}
				foreach (var coroutine in coroutines) yield return coroutine;
			}

			public void Skip() {
				foreach (var sequence in sequences) {
					sequence.Skip();
				}
			}
		}

		[SerializeField] internal List<SequenceGroup> sequenceGroups = new List<SequenceGroup>() { new() };

		[SerializeField] internal bool evaluateGroups = true;
		[SerializeField] internal bool playOnStart;

		[SerializeField] internal UnityEvent started;
		[SerializeField] internal UnityEvent ended;

		private int currentIndex = 0;
		private bool isPlaying;

		public UnityEvent Started => started;
		public UnityEvent Ended => ended;

		protected virtual void Awake() {
			int index = 0;
			while (index < sequenceGroups.Count) {
				if (!sequenceGroups[index].Evaluate()) {
					sequenceGroups.RemoveAt(index);
					continue;
				}

				sequenceGroups[index].runner = this;
				index++;
			}
		}

		protected virtual void Start() {
			if (playOnStart) Play();
		}

		public void Play() {
			if (isPlaying) return;

			StartCoroutine(Routine());

			IEnumerator Routine() {
				currentIndex = 0;

				isPlaying = true;
				started?.Invoke();

				for (int i = 0; i < sequenceGroups.Count; i++) {
					yield return sequenceGroups[i].DoSequence();
					currentIndex = i;
				}
				
				isPlaying = false;
				ended?.Invoke();
			}
		}

		public void Skip() {
			if (!isPlaying) return;

			sequenceGroups[currentIndex].Skip();
		}
	}
}
