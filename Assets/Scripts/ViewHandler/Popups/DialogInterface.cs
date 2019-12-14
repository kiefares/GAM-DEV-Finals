using UnityEngine;
using System.Collections;

public interface DialogInterface {
	void SetMessage(string text);
	void SetOnConfirmListener(System.Action action);
	void SetOnCancelListener(System.Action action);
	void SetOnDismissListener(System.Action action);
}
