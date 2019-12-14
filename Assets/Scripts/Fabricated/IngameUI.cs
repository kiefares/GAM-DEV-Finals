using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabricated
{
	public class IngameUI : BaseUI<IngameUI>
	{
		public Button cancel; // Return to Game
		public Button settings;
		public Button toMenu;
		public Button exit;
		public GameObject settingsUI;

		new void Awake()
		{
			base.Awake();

			settingsUI = Instantiate(settingsUI);
			settingsUI.GetComponent<SettingsUI>().parent = gameObject;

			Manager.Input.RegisterAction("Menu", () =>
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
                    Debug.Log("Escape is pressed");
					if (settingsUI.activeSelf)
					{
						settingsUI.SetActive(false);
						gameObject.SetActive(true);
					}
					else
					{
						gameObject.SetActive(!gameObject.activeSelf);

						if (gameObject.activeSelf)
							Manager.Game.PauseGame();
						else
							Manager.Game.ResumeGame();
					}
				}
			});

			cancel.onClick.AddListener(CancelCallback);
			settings.onClick.AddListener(SettingsCallback);
			toMenu.onClick.AddListener(ToMenuCallback);
			exit.onClick.AddListener(ExitCallback);
		}

		void CancelCallback()
		{
			gameObject.SetActive(false);
		}

		void SettingsCallback()
		{
			gameObject.SetActive(false);
			settingsUI.SetActive(false);
		}

		void ToMenuCallback()
		{
			SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
		}

		void ExitCallback()
		{
			Application.Quit();
		}
	}
}