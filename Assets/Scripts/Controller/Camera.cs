using UnityEngine;

namespace Controller
{
	public class Camera : Singleton<Camera>
	{
		public Transform target;
		public float speed = 0.125f;
		public Vector3 offset;

		void LateUpdate()
		{
			UnityEngine.Camera.main.transform.position = target.position + offset;
		}
	}
}