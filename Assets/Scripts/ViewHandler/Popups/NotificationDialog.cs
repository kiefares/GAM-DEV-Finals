using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base popup inherited by other popups
/// BY: NeilDG
/// </summary>
public class NotificationDialog : View, DialogInterface 
{
	[SerializeField] protected Text messageText;
	[SerializeField] private Button closeButton;
	[SerializeField] private Text confirmText;

	private System.Action onConfirmAction;
	private System.Action onCancelAction;
	private System.Action onDismissAction;

	private RectTransform textRect;
	private const float TEXT_OFFSET = 30.0f;

	public override void OnShowStarted()
	{
		base.OnShowStarted();

		this.onConfirmAction = null;
		this.onCancelAction  = null;
		this.onDismissAction = null;
	}


	public void SetMessage(string message) {
		this.messageText.text = message;
	}

	/// <summary>
	/// Sets the text for the confirm button
	/// </summary>
	/// <param name="message">Message.</param>
	public void SetConfirmText(string message) {
		this.confirmText.text = message;
	}
	
	/// <summary>
	/// Executes the specified function upon click of the confirm button
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnConfirmListener(System.Action action) {
		this.onConfirmAction = action;
	}

	/// <summary>
	/// Executes the specified function upon click of the close/cancel button
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnCancelListener(System.Action action) {
		this.onCancelAction = action;
	}

	/// <summary>
	/// Executes the specified function upon successful hide of the popup
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnDismissListener(System.Action action) {
		this.onDismissAction = action;
	}

	/// <summary>
	/// Adds a close button if set dismissable is true.
	/// </summary>
	/// <param name="flag">If set to <c>true</c> flag.</param>
	public void SetDismissable(bool flag) {
		closeButton.gameObject.SetActive(flag);
	}

	public void OnConfirmClicked() 
	{
		
			this.Hide();

			if(this.onConfirmAction != null) 
			{
				this.onConfirmAction.Invoke();
			}
			
	}

	public void OnCloseClicked() 
	{
		
			this.Hide();

			if(this.onCancelAction != null) 
			{
				this.onCancelAction.Invoke();
			}
		
	}

	public override void OnHideCompleted ()
	{
		base.OnHideCompleted ();

		if(this.onDismissAction != null) 
		{
			this.onDismissAction.Invoke();
		}
	}
}
