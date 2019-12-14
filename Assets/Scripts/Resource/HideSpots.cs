using System.Collections.Generic;
using UnityEngine;

namespace Resource
{
	public class HideSpots : Singleton<HideSpots>
	{
		[SerializeField]
		private GameObject Collection;
		private static HashSet<GameObject> list;

		new void Awake()
		{
			base.Awake();

			list = new HashSet<GameObject>();

			foreach (Transform child in Collection.transform)
				list.Add(child.gameObject);
		}

		public static GameObject NearestHideSpot(Vector3 position, float minDis)
		{
			float target = position.z;
			GameObject nearest = null;
			float dis = 0f;

			foreach (GameObject spot in list)
			{
				float nextDis = Mathf.Abs(target - spot.transform.position.z);

				if (nextDis > minDis)
					continue;

				if (nearest == null)
				{
					nearest = spot;
					dis = nextDis;
				}
				else if (nextDis < dis)
				{
					nearest = spot;
					dis = nextDis;
				}
			}

			return nearest;
		}
	}
}
