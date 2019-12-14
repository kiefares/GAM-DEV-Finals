namespace Controller
{
	public class Stealth : Singleton<Stealth>
	{
		public float visibility = 0f;

		new void Awake()
		{
			base.Awake();
		}
	}
}