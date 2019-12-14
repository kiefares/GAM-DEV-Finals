using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Fabricated.DialogUI;

namespace Controller
{
	public partial class Player
	{
		public const string INPUT_PLAYER_INTERACT = "PlayerInputInteract";
        [SerializeField] private GameObject newObj;
        [SerializeField] private GameObject hotdog;
        [SerializeField] private GameObject hotdogc;
        [SerializeField] private GameObject office;
        [SerializeField] private GameObject officec;
        [SerializeField] private GameObject Cat;
        [SerializeField] private GameObject AdvNextLevel;
        [SerializeField] private GameObject Officer;

		private bool isInteracting = false;
		private bool isHiding = false;

		void Interact_Start()
		{
            newObj.gameObject.SetActive(false);
            hotdog.gameObject.SetActive(false);
            hotdogc.gameObject.SetActive(false);
            office.gameObject.SetActive(false);
            officec.gameObject.SetActive(false);
            Cat.gameObject.SetActive(false);
            AdvNextLevel.SetActive(false);

			Manager.Input.RegisterAction(INPUT_PLAYER_INTERACT, () =>
			{
				if (isInteracting)
				{
					isInteracting = false;
					return;
				}

				if (Profile.Input.accept.IsDown())
				{
					isInteracting = true;

					if (isHiding)
					{
						isHiding = false;
						player.transform.position = new Vector3(x, y, player.transform.position.z);
						player.transform.rotation = quaternion;
						rigidbody.constraints = constraints;

						Manager.Input.ToggleCollection(INPUT_PLAYER_MOBILITY, true);
                        
						return;
					}

					// Temporary stuff.
					{
						GameObject riana = Resource.Actors.actors.FirstOrDefault(
							v => v.name == "Riana Pascual"
						);
						float distance = Mathf.Abs(riana.transform.position.z - player.transform.position.z);

						if (distance <= Profile.Player.talkRange)
						{
							Speed = 0f;

							SetSequence(
								new Sequence("Riana Pascual", "Hello."),
								new Sequence("Me", "Hi. Have you seen a cat anywhere around here?"),
								new Sequence("Riana Pascual", "I don't think I have, maybe ask the student volunteer near the grocery store?")
							);
                            newObj.gameObject.SetActive(true);
                            return;
						}
					}

                    {
                        GameObject denise = Resource.Actors.actors.FirstOrDefault(
                            v => v.name == "Denise Borja"
                        );
                        float distance = Mathf.Abs(denise.transform.position.z - player.transform.position.z);

                        if (distance <= Profile.Player.talkRange)
                        {
                            Speed = 0f;

                            SetSequence(
                                new Sequence("Denise Borja", "Hello."),
                                new Sequence("Me", "Hi. Have you seen a cat anywhere around here?"),
                                new Sequence("Denise Borja", "Most of the stray cats stay near the hotdog cart, that might be a good place to start looking!!")
                            );
                            Cat.gameObject.SetActive(true);
                            hotdog.gameObject.SetActive(true);
                            hotdogc.gameObject.SetActive(true);
                            AdvNextLevel.SetActive(true);
                            Officer.SetActive(false);
                            return;
                        }
                    }

                    {
                        GameObject skipper = Resource.Actors.actors.FirstOrDefault(
                            v => v.name == "Officer Skipper"
                        );
                        float distance = Mathf.Abs(skipper.transform.position.z - player.transform.position.z);

                        if (distance <= Profile.Player.talkRange)
                        {
                            Speed = 0f;

                            SetSequence(
                                new Sequence("Officer Skipper", "Good Morning!"),
                                new Sequence("Me", "Hello officer! Did you happen to see any cats around here?"),
                                new Sequence("Officer Skipper", "There have been noise complaints of loud cats near the Office Building!"),
                                new Sequence("Officer Skipper", "You might find the cat you're looking for there!")
                            );
                            office.gameObject.SetActive(true);
                            officec.gameObject.SetActive(true);
                            return;
                        }
                    }

                    {
                        GameObject jomokee = Resource.Actors.actors.FirstOrDefault(
                            v => v.name == "Jomokee"
                        );
                        float distance = Mathf.Abs(jomokee.transform.position.z - player.transform.position.z);

                        if (distance <= Profile.Player.talkRange)
                        {
                            Speed = 0f;

                            SetSequence(
                                new Sequence("Jomokee", "That cat behind the diner scratched me. I should probably go to the hospital")
                            );
                            //newObj.gameObject.SetActive(true);
                            return;
                        }
                    }



                    GameObject spot = Resource.HideSpots.NearestHideSpot(
						player.transform.position,
						Profile.Player.hideRange
					);

					if (spot != null)
					{
						Manager.Input.ToggleCollection(INPUT_PLAYER_MOBILITY, false);

						isHiding = true;
						rigidbody.constraints = RigidbodyConstraints.FreezeAll;

						player.transform.SetPositionAndRotation(
							spot.transform.position,
							spot.transform.rotation
						);
					}
				}
			});
		}
	}
}