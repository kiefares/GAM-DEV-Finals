using UnityEngine;
using System.Collections;

namespace Prism.Demo {

[RequireComponent(typeof(Light))]
public class PrismLightFlicker : MonoBehaviour {

		[Range(-2f, 2f)]
		public float offset = 0.3f;

		[Range(0f, 1f)]
		public float flickerChance = 0.395f;

		[Range(0f, 5f)]
		public float minAliveTime = 0.04f;

		[Range(0f, 5f)]
		public float maxAliveTime = 0.52f;

		[Range(0f, 5f)]
		public float flickerSpeed = 2f;

		private float nextTick;

		private Light m_light;

		private float initialIntensity;

		private float targetIntensity;

		private float lerpAmount;

		// Use this for initialization
		void Start () {
			m_light = GetComponent<Light>();
			initialIntensity = m_light.intensity;
		}

		private static float cubicEaseIn( float start, float end, float t )
		{
			end -= start;
			return end * t * t * t + start;
		}
		
		// Update is called once per frame
		void Update () {
			
			m_light.intensity = cubicEaseIn(m_light.intensity, targetIntensity, lerpAmount);
			lerpAmount += (Time.deltaTime * flickerSpeed);
			lerpAmount = Mathf.Min(lerpAmount, 1.0f);

			if(Time.time < nextTick) return;

			float r = Random.Range(0f, 1f);

			if(r > flickerChance)
			{
				r += offset;
				targetIntensity = initialIntensity * r;
				nextTick = Time.time + Random.Range(minAliveTime, maxAliveTime);
				lerpAmount = 0f;
			}
		}
}

}
