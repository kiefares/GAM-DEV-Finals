using UnityEngine;

namespace Controller
{
	public class Fabricator : MonoBehaviour
	{
		[SerializeField]
		private GameObject[] objects;

		void Awake()
		{
			foreach (GameObject stuff in objects)
				Instantiate(stuff);
		}
	}
}