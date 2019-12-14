using UnityEngine;

public class ObjectSingleton<T> : MonoBehaviour where T : MonoBehaviour
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
			Debug.Log($"ObjectSingleton<{typeof(T).Name}> instantiated.");
			return;
		}

		Debug.LogError($"ObjectSingleton<{typeof(T).Name}> duplicate found.");

		if (self?.gameObject != gameObject)
			Destroy(gameObject);
		else
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