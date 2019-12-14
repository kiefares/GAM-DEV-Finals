using UnityEngine;
using System.Collections;

/// <summary>
/// A builder class for dialogs
/// By: NeilDG
/// </summary>
public class DialogBuilder {
	public enum DialogType {
		NOTIFICATION,
		/*TIMED_DIALOG,
		CHOICE_DIALOG*/

		CHOICE_DIALOG
	}

	public static DialogInterface Create(DialogType dialogType) 
	{
		DialogInterface dialog = null;

		switch(dialogType)
		{
			case DialogType.CHOICE_DIALOG	:dialog = RequestDialog (ViewNames.DialogNames.TWO_CHOICE_DIALOG_NAME); break;
			case DialogType.NOTIFICATION        : dialog = RequestDialog(ViewNames.DialogNames.NOTIFICATION_DIALOG_NAME); break;
			/*case DialogType.INSUFFICIENT_DIALOG : dialog = RequestDialog(DCScreenNames.Dialogs.INSUFFICIENT_DIALOG_STRING); break;
			case DialogType.TIMED_DIALOG        : dialog = RequestDialog(DCScreenNames.Dialogs.TIMED_DIALOG_STRING); break;
			case DialogType.CHOICE_DIALOG       : dialog = RequestDialog(DCScreenNames.Dialogs.TWO_CHOICE_DIALOG_STRING); break;
			case DialogType.FB_SHARE_DIALOG     : dialog = RequestDialog(DCScreenNames.Dialogs.FB_SHARE_DIALOG_STRING); break;
			case DialogType.CLAIM_SUCCESS_DIALOG: dialog = RequestDialog(DCScreenNames.Dialogs.CLAIM_SUCCESS_DIALOG_STRING);break;
			case DialogType.FB_CONNECTING_DIALOG: dialog = RequestDialog(DCScreenNames.Dialogs.FB_CONNECTING_DIALOG_STRING);break;*/
		}

		return dialog;
	}

	private static DialogInterface RequestDialog(string dialogName) 
	{
		DialogInterface dialogInterface = (DialogInterface) ViewHandler.Instance.Show(dialogName, true);
		return dialogInterface;
	}
}
