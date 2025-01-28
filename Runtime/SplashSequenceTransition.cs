using System.Collections;
using UnityEngine;

namespace JK.UnityCustomSplash {
	public abstract class SplashSequenceTransition : MonoBehaviour {
		public abstract IEnumerator In();
		public abstract IEnumerator Out();
	}
}
