using UnityEngine;
using UnityEngine.UI;

namespace Fabricated
{
	public class SettingsUI : BaseUI<SettingsUI>
	{
		public GameObject parent;
		public Button back;

		public Button[] tabs;
		public GameObject[] canvases;

		public int currentTab = 0;

		new void Awake()
		{
			base.Awake();

			back.onClick.AddListener(BackCallback);

			for (int i = 0; i < tabs.Length; i++)
			{
				Button tab = tabs[i];
				int n = i;

				tab.onClick.AddListener(() => TabCallback(n));
			}
		}

		void BackCallback()
		{
			gameObject.SetActive(false);
			parent.SetActive(true);
		}

		void TabCallback(int n)
		{
			Debug.Log(currentTab);

			if (currentTab == n)
				return;

			currentTab = n;

			for (int i = 0; i < canvases.Length; i++)
			{
				GameObject canvas = canvases[i];
				canvas.SetActive(i == n);
			}
		}
	}
}