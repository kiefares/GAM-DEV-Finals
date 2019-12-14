using UnityEngine;

namespace Profile
{
	public static class Input
	{
        public static Hotkey moveUp = new Hotkey().MainKeys(
            KeyCode.W,
            KeyCode.UpArrow
        );

        public static Hotkey moveDown = new Hotkey().MainKeys(
            KeyCode.S,
            KeyCode.DownArrow 
        );

        public static Hotkey moveLeft = new Hotkey().MainKeys(
			KeyCode.A,
			KeyCode.LeftArrow
		);

		public static Hotkey moveRight = new Hotkey().MainKeys(
			KeyCode.D,
			KeyCode.RightArrow
		);

        public static Hotkey run = new Hotkey().MainKeys(
            KeyCode.LeftShift,
            KeyCode.RightShift
        );

        public static Hotkey accept = new Hotkey().MainKeys(
            KeyCode.E,
            KeyCode.Return,
            KeyCode.Mouse0
        );
	}
}
