using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TwoChoiceDialog : View, DialogInterface {

	[SerializeField] private Text messageText;
	[SerializeField] private Text positiveText;
	[SerializeField] private Text negativeText;

	private System.Action onConfirmAction;
	private System.Action onCancelAction;
	private System.Action onDismissAction;

	public void SetMessage(string message) {
		this.messageText.text = message;
	}

	public void SetConfirmText(string text) {
		this.positiveText.text = text;
	}

	public void SetCancelText(string text) {
		this.negativeText.text = text;
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

	public void OnConfirmClicked() {
		this.Hide();

		if(this.onConfirmAction != null) {
			this.onConfirmAction.Invoke();
		}
	}

	public void OnCloseClicked() { 
		this.Hide();

		if(this.onCancelAction != null) {
			this.onCancelAction.Invoke();
		}
	}

	public override void OnHideCompleted ()
	{
		base.OnHideCompleted ();

		if(this.onDismissAction != null) {
			this.onDismissAction.Invoke();
		}

		this.onConfirmAction = null;
		this.onCancelAction = null;
		this.onDismissAction = null;
	}
}
