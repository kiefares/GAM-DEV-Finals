using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabricated
{
	public class MainMenuUI : BaseUI<MainMenuUI>
	{
		public Button play;
		public Button settings;
		public Button exit;
		public GameObject settingsUI;

		new void Awake()
		{
			base.Awake();
            Debug.Log("Awaken");

			settingsUI = Instantiate(settingsUI);
			settingsUI.GetComponent<SettingsUI>().parent = gameObject;

			play.onClick.AddListener(PlayCallback);
			settings.onClick.AddListener(SettingsCallback);
			exit.onClick.AddListener(ExitCallback);
		}

		new void Start()
		{
		}

		void PlayCallback()
		{
			//SceneManager.LoadScene("Chapter1", LoadSceneMode.Single);
            SceneManager.LoadScene("Chapter2", LoadSceneMode.Single);
		}

		void SettingsCallback()
		{
			gameObject.SetActive(false);
			settingsUI.SetActive(true);
		}

		void ExitCallback()
		{
			Application.Quit();
		}
	}
}