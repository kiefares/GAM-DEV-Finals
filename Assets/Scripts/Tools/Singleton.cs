using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T self;

	public static T Self
	{
		get
		{
			if (!self)
				self = FindObjectOfType<T>();

			return self;
		}
	}

	protected void Awake()
	{
		if (Self == this)
		{
			Debug.Log($"Singleton<{typeof(T).Name}> instantiated.");
			return;
		}

		Debug.LogError($"Singleton<{typeof(T).Name}> duplicate found.");
		Destroy(this);
	}

	protected void OnDestroy()
	{
		if (!self)
			return;

		if (self != this)
			return;

		self = null;
	}

	public static bool Exists()
	{
		return self != null;
	}
}