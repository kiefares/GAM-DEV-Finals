using UnityEngine;

namespace Controller
{
	public partial class Player
	{
		public const string INPUT_PLAYER_MOBILITY = "PlayerInputMobility";

        public float movementSpeed;

		private float Speed = 0f;
		private float Rotation = 0f;
		// true = Right; false = Left
		private bool Direction = true;
        private bool isRunning = false;

		void Mobility_Start()
		{
			Manager.Input.RegisterAction(INPUT_PLAYER_MOBILITY, () =>
			{
				Speed = 0f;

				if (Profile.Input.moveLeft.IsHeld())
					Speed--;

				if (Profile.Input.moveRight.IsHeld() )
					Speed++;

                if (Profile.Input.run.IsHeld())
                    isRunning = true;
                else isRunning = false;

				if (Speed != 0)
					Direction = Speed > 0;

                if (Direction)
				{
					if (Rotation > 0f)
						Rotation = Mathf.Max(
							0,
							Rotation - Time.deltaTime * 360f * Profile.Player.turnRate
						);
				}
				else if (Rotation < 180f)
					Rotation = Mathf.Min(
						180f,
						Rotation + Time.deltaTime * 360f * Profile.Player.turnRate
					);

				player.transform.rotation = Quaternion.Euler(0f, Rotation, 0f); 
			});
		}

		void Mobility_FixedUpdate()
		{
            movementSpeed = Profile.Player.moveSpeed; // Used to change speed when walking and running
            if (isRunning) {
                movementSpeed = movementSpeed * 1.5f;
            }

			float speed = Speed * movementSpeed * Time.deltaTime;
			rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, speed);
		}
	}
}