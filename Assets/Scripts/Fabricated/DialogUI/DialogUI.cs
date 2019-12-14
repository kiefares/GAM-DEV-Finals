using UnityEngine;
using UnityEngine.UI;

namespace Fabricated
{
	public partial class DialogUI : BaseUI<DialogUI>
	{
		public const string DIALOG_INPUT = "DialogInput";

		public GameObject Header;
		public GameObject Body;

		private Text HeaderText;
		private Text BodyText;

		private Sequence[] sequences;
		private int step = 0;

		new void Awake()
		{
			base.Awake();

			HeaderText = Header.GetComponent<Text>();
			BodyText = Body.GetComponent<Text>();

			Manager.Input.RegisterAction(DIALOG_INPUT, () =>
			{
				if (!Self.gameObject.activeSelf)
					return;

				if (Profile.Input.accept.IsDown())
				{
					step++;

					if (step < sequences.Length)
						SetDialog(sequences[step].header, sequences[step].body);
					else
						ToggleDisplay(false);
				}
			});
		}

		public static void SetDialog(string header, string body)
		{
			Self.HeaderText.text = header;
			Self.BodyText.text = body;
		}

		public static void SetSequence(params Sequence[] sequences)
		{
			Self.sequences = sequences;
			Self.step = 0;

			bool flag = sequences?.Length > 0;

			if (flag)
				SetDialog(sequences[0].header, sequences[0].body);

			ToggleDisplay(flag);
		}

		public static void ToggleDisplay(bool flag)
		{
			Manager.Input.ToggleCollection(Controller.Player.INPUT_PLAYER_MOBILITY, !flag);
			Manager.Input.ToggleCollection(Controller.Player.INPUT_PLAYER_INTERACT, !flag);

			Self.gameObject.SetActive(flag);
		}
	}
}