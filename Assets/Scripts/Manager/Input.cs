using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
	public class Input : Singleton<Input>
	{
		private static Dictionary<string, HashSet<Action>> collections;

		// List of disabled input collections.
		private static HashSet<string> disabled;

		new void Awake()
		{
			base.Awake();

			collections = new Dictionary<string, HashSet<Action>>();
			disabled = new HashSet<string>();
		}

		void Update()
		{
			foreach (KeyValuePair<string, HashSet<Action>> pair in collections)
			{
				if (disabled.Contains(pair.Key))
					continue;

				foreach (Action act in pair.Value)
					act();
			}
		}

		public static void NewCollection(string key)
		{
			if (collections.ContainsKey(key))
				return;

			collections.Add(key, new HashSet<Action>());
		}

		public static void RegisterAction(string key, Action act)
		{
            Debug.Log("Registered: " + key);
			if (!collections.ContainsKey(key))
				NewCollection(key);

			collections[key].Add(act);
		}

		public static void ToggleCollection(string key, bool flag)
		{
			if (flag != disabled.Contains(key))
				return;

			if (flag)
				disabled.Remove(key);
			else
				disabled.Add(key);
		}
	}
}