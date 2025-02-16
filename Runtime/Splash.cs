using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JK.UnityCustomSplash {
	public class Splash : MonoBehaviour {
		internal enum SkippableCondition {
			Any,
			All,
		}

		[Serializable]
		internal class SequenceGroup {
			[SerializeField] internal SkippableCondition skippableCondition;
			[SerializeField] internal List<SplashSequence> sequences = new List<SplashSequence>() { null };

			public bool Any { get; private set; }
			public bool Skippable { get; private set; }

			public void Initialize(bool removeEmptyReferences) {
				if (removeEmptyReferences) {
					int index = 0;
					while (index < sequences.Count) {
						if (!sequences[index]) {
							sequences.RemoveAt(index);
							continue;
						}
						index++;
					}
				}

				int count = sequences.Count;
				int skippableCount = 0;
				for (int i = 0; i < count; i++) {
					if (skippableCondition == SkippableCondition.Any && sequences[i].skippable) {
						skippableCount = count;
						break;
					}

					skippableCount++;
				}

				Any = sequences.Count > 0;
				Skippable = skippableCount == count;
				if (Skippable) {
					for (int i = 0; i < count; i++) {
						sequences[i].skippable = true;
					}
				}
			}

			public bool Skip() {
				if (!Skippable) return false;

				for (int i = 0; i < sequences.Count; i++) {
					sequences[i].Skip();
				}
				return true;
			}
		}

		[SerializeField] internal List<SequenceGroup> sequenceGroups = new List<SequenceGroup>() { new() };

		[SerializeField] internal bool removeEmptyReferences;
		[SerializeField] internal bool skippable;
		[SerializeField] internal bool playOnStart;

		[SerializeField] internal UnityEvent onPlay;
		[SerializeField] internal UnityEvent onSkip;
		[SerializeField] internal UnityEvent onEnd;

		private int sequenceIndex = 0;
		private bool running;
		private readonly List<Coroutine> coroutines = new List<Coroutine>();

		public UnityEvent OnPlay => onPlay;
		public UnityEvent OnSkip => onSkip;
		public UnityEvent OnEnd => onEnd;

		protected virtual void Awake() {
			int index = 0;
			while (index < sequenceGroups.Count) {
				sequenceGroups[index].Initialize(removeEmptyReferences);
				if (!sequenceGroups[index].Any) {
					sequenceGroups.RemoveAt(index);
					continue;
				}
				index++;
			}
		}

		protected virtual void Start() {
			if (playOnStart) Play();
		}

		public void Play() {
			if (running) return;

			StartCoroutine(Routine());

			IEnumerator Routine() {
				sequenceIndex = 0;

				running = true;
				OnPlay?.Invoke();

				while (sequenceIndex < sequenceGroups.Count) {
					coroutines.Clear();

					var group = sequenceGroups[sequenceIndex];
					for (int j = 0; j < group.sequences.Count; j++) {
						var coroutine = StartCoroutine(group.sequences[j].Play());
						coroutines.Add(coroutine);
					}

					for (int j = 0; j < coroutines.Count; j++) {
						yield return coroutines[j];
					}

					sequenceIndex++;
				}

				running = false;
				OnEnd?.Invoke();
			}
		}

		public void Skip() {
			if (!running) return;
			if (!skippable) return;
			if (!sequenceGroups[sequenceIndex].Skip()) return;
			
			OnSkip?.Invoke();
		}
	}
}
