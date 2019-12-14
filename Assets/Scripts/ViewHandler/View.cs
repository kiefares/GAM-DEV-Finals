using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// Represents the basic user interface customized for BC. Heavily referenced from Dragon Cubes UI
/// 
/// By: NeilDG
/// </summary>
public class View : MonoBehaviour {

	[SerializeField] private bool asRootScreen = false;
	[SerializeField] protected DOViewAnimation.EntranceType entranceType = DOViewAnimation.EntranceType.FROM_TOP;
	[SerializeField] protected DOViewAnimation.ExitType exitType = DOViewAnimation.ExitType.TO_TOP;

	protected RectTransform rectTransform;
	private IViewAnimation viewAnimation;

	protected bool cancelable = true;
	private bool destroyable = true;
	private bool hasAttachedSounds = false;

	private List<Button> buttonList = null;

	void Awake() {
		this.rectTransform = this.GetComponent<RectTransform> (); //assume that it is a UI with rect transform.
	}

	public virtual void Show() {

        this.gameObject.SetActive(true);

		if(this.viewAnimation == null) {
			DOViewAnimation hoViewAnimation = new DOViewAnimation(this.rectTransform,this);
			hoViewAnimation.SetAnimationType(this.entranceType, this.exitType);
			
			this.viewAnimation = hoViewAnimation;
		}

		this.AttachButtonSounds();
		this.Reset();
		this.OnShowStarted();
		this.viewAnimation.PerformEntrance();
	}

	public virtual void Hide() {
		this.OnHideStarted();

		if(this.viewAnimation != null) {
			this.viewAnimation.PerformExit();
		}
		else {
			this.OnHideCompleted();
		}
	}

	public void SetCancelable(bool flag) {
		this.cancelable = flag;
	}

	public bool IsCancelable() {
		return this.cancelable;
	}

	private void Reset() {
		this.SetVisibility(true);

		this.transform.position = Vector3.zero;
		this.transform.localScale = Vector3.one;
		this.transform.rotation = Quaternion.identity;

		this.rectTransform.anchorMin = Vector2.zero;
		this.rectTransform.anchorMax = Vector2.one;
		this.rectTransform.offsetMax = this.rectTransform.offsetMin = Vector2.zero;
		this.rectTransform.pivot = new Vector2(0.5f, 0.5f);
	}

	private void PopulateButtonList(Transform parent) {
		if(this.buttonList == null) {
			this.buttonList = new List<Button>();
		}

		foreach(Transform child in parent) {
			Button button = child.GetComponent<Button>();

			if(button != null) {
				this.buttonList.Add(button);
			}

			this.PopulateButtonList(child);
		}

	}

	private void AttachButtonSounds() {
		if(this.hasAttachedSounds == false) {
			this.PopulateButtonList(this.transform);

			foreach(Button button in this.buttonList) {
				button.onClick.AddListener(this.ButtonTapSFX);
			}

			this.hasAttachedSounds = true;
		}
	}

	public void ToggleAllButtons(bool flag) {
		if(this.buttonList == null) {
			return;
		}

		foreach(Button button in this.buttonList) {
			if(button == null) continue;
			button.enabled = flag;
		}
	}

	public void ButtonTapSFX()
	{
		//DCSoundManager.GetInstance().PlaySfx("Button", false);
	}
	
	public string GetName() {
		return this.gameObject.name;
	}

	public bool IsRootScreen() {
		return this.asRootScreen;
	}

	public void DoNotDestroy() {
		this.destroyable = false;
	}
	
	public void MakeDestroyable() {
		this.destroyable = true;
	}
	
	public bool ShouldBeDestroyed() {
		return this.destroyable;
	}

	public void SetVisibility(bool flag) {
		this.gameObject.SetActive(flag);
	}

	/// <summary>
	/// Copies the properties from a view to the next view.
	/// </summary>
	/// <param name="view">View.</param>
	public void CopyProperty(View view) {
		this.rectTransform = view.GetComponent<RectTransform>();

		this.entranceType = view.entranceType;
		this.exitType = view.exitType;

		DOViewAnimation hoViewAnimation = new DOViewAnimation(this.rectTransform,this);
		hoViewAnimation.SetAnimationType(this.entranceType, this.exitType);
		
		this.viewAnimation = hoViewAnimation;
	}

	public void SetHOAnimation(DOViewAnimation.EntranceType entranceType, DOViewAnimation.ExitType exitType) {
		this.viewAnimation = null;
		this.entranceType = entranceType; 
		this.exitType = exitType;

		DOViewAnimation hoViewAnimation = new DOViewAnimation(this.rectTransform,this);
		hoViewAnimation.SetAnimationType(this.entranceType, this.exitType);
		
		this.viewAnimation = hoViewAnimation;
	}

	public DOViewAnimation.EntranceType GetEntranceType() {
		return this.entranceType;
	}

	public DOViewAnimation.ExitType GetExitType() {
		return this.exitType;
	}

	#region View events
	public virtual void OnShowStarted() {
		ViewHandler.Instance.RestrictUIActions();
		this.ToggleAllButtons(false);
	}
	public virtual void OnShowCompleted() {
		ViewHandler.Instance.AllowUIActions();
		this.ToggleAllButtons(true);
	}
	public virtual void OnBackButtonPressed() {}
	public virtual void OnHideStarted() {
		ViewHandler.Instance.RestrictUIActions();
		this.ToggleAllButtons(false);
	}
	public virtual void OnHideCompleted() {
		ViewHandler.Instance.OnViewHidden(this);
	}
	#endregion

}
