using UnityEngine;

namespace Controller
{
	public partial class Player : Singleton<Player>
	{
		[SerializeField]
		private GameObject player;
		[SerializeField]
		private LayerMask CollidableLayer;

		private new Rigidbody rigidbody;
		private new CapsuleCollider collider;

		private RigidbodyConstraints constraints;
		private Quaternion quaternion;

		private float x;
		private float y;

		new void Awake()
		{
			base.Awake();

			rigidbody = player.GetComponent<Rigidbody>();
			collider = player.GetComponent<CapsuleCollider>();

			constraints = rigidbody.constraints;
			quaternion = player.transform.rotation;

			x = player.transform.position.x;
			y = player.transform.position.y;
		}

		void Start()
		{
			Mobility_Start();
			Interact_Start();
		}

		void FixedUpdate()
		{
			Mobility_FixedUpdate();
		}
	}
}