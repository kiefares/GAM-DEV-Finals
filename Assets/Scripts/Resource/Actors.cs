using System.Collections.Generic;
using UnityEngine;

namespace Resource
{
	public class Actors : Singleton<Actors>
	{
		[SerializeField]
		private GameObject Collection;
		public static HashSet<GameObject> actors;

		new void Awake()
		{
			base.Awake();

			actors = new HashSet<GameObject>();

			foreach (Transform child in Collection.transform)
				actors.Add(child.gameObject);
		}
	}
}
