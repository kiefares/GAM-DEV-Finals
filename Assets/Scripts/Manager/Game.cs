using UnityEngine;

namespace Manager
{
	public class Game : Singleton<Game>
	{
		public static bool IsPaused { get; private set; }
		private static float TimeScale;

		new void Awake()
		{
			base.Awake();

			IsPaused = false;
			TimeScale = Time.timeScale = 1f;
		}

		public static void SetTimeScale(float v)
		{
			TimeScale = v;
		}

		public static void PauseGame()
		{
			if (IsPaused)
				return;

			IsPaused = true;
			Time.timeScale = 0;
		}

		public static void ResumeGame()
		{
			if (!IsPaused)
				return;

			IsPaused = false;
			Time.timeScale = TimeScale;
		}
	}
}