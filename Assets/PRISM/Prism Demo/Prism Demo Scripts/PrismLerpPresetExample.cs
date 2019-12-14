using UnityEngine;
using System.Collections;
using Prism.Utils;

namespace Prism.Demo {

	/// <summary>
	/// Prism lerp preset example. 
	/// </summary>
	public class PrismLerpPresetExample : MonoBehaviour {

		private PrismEffects prism;
		
		[Header("NOTE: This is an example script, you should only have one per scene")]
		[Header("This script lerps a PRISM preset based on distance to the camera")]
		public float maxDistance = 500f;
		public float t;
		[Tooltip("The Prism-Preset to lerp TO")]
		public PrismPreset target;
		public AnimationCurve cameraDistanceCurve;

		// Use this for initialization
		void Start () {
			prism = Camera.main.GetComponent<PrismEffects>();

			if(!prism)
			{
				Debug.LogWarning("Main camera had no PRISM on it! Can't initialize demo script.");
				enabled = false;
			}
		}
		
		// Update is called once per frame
		void Update () {

			t = Vector3.Distance(transform.position, Camera.main.transform.position);
			t = cameraDistanceCurve.Evaluate(t);
			prism.LerpToPreset(target, t);
		}
	}

}