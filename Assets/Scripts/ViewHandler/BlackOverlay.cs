using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlackOverlay : MonoBehaviour {

	private static BlackOverlay sharedInstance = null;

	[SerializeField] private bool hideOnStart = false;
	private Button overlayButton;


	void Awake() {
		sharedInstance = this;

		if(sharedInstance.hideOnStart) {
			Hide();
		}
	}

	// Use this for initialization
	void Start () {
		this.overlayButton = this.GetComponent<Button>();
	}


	public static void Show(Transform uiTransform) {
		if(sharedInstance == null) {
			Debug.LogWarning("Black overlay not found");
			return;
		}

		//Debug.Log("Black overlay SHOW");
		uiTransform.SetAsLastSibling();
		sharedInstance.transform.SetSiblingIndex(uiTransform.GetSiblingIndex() - 1);

		sharedInstance.gameObject.SetActive(true);
	}

	public static void Hide() {
		if(sharedInstance == null) {
			Debug.LogWarning("Black overlay not found");
			return;
		}

		//Debug.Log("Black overlay HIDE");
		sharedInstance.gameObject.SetActive(false);
	}

	public void OnOverlayButtonClicked() 
	{
			ViewHandler.Instance.HideCurrentView();
	}
}
