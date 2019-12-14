using UnityEngine;

namespace Fabricated
{
	public class BaseUI<T> : ObjectSingleton<T> where T : MonoBehaviour
	{
		protected void Start()
		{
			gameObject.SetActive(false);
		}
	}
}