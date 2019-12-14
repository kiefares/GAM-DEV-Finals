using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimedDialog : View, DialogInterface {
	[SerializeField] private Text messageText; 

	private System.Action onDismissAction;

	private float timedDuration;
	private bool indefinite = false;

	void Start() {
		this.SetCancelable(false);
	}

	public void SetMessage(string message) {
		this.messageText.text = message;
	}

	public void SetDuration(float timedDuration) {
		this.timedDuration = timedDuration;

		this.StartCoroutine(this.DelayedHide());
	}

	public void Dismiss() {
		this.Hide();
	}

	private IEnumerator DelayedHide() {
		yield return new WaitForSeconds(this.timedDuration);

		this.Hide();
	}

	/// <summary>
	/// Executes the specified function upon click of the confirm button
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnConfirmListener(System.Action action) {
		Debug.LogError("Not implemented for a timed dialog!");
	}
	
	/// <summary>
	/// Executes the specified function upon click of the close/cancel button
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnCancelListener(System.Action action) {
		Debug.LogError("Not implemented for a timed dialog!");
	}
	
	/// <summary>
	/// Executes the specified function upon successful hide of the popup
	/// </summary>
	/// <param name="action">Action.</param>
	public void SetOnDismissListener(System.Action action) {
		this.onDismissAction = action;
	}

	public override void OnHideCompleted ()
	{
		base.OnHideCompleted ();

		this.StopAllCoroutines();

		if(this.onDismissAction != null) {
			this.onDismissAction.Invoke();
		}

		this.onDismissAction = null;
	}
}
