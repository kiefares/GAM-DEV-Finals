namespace Fabricated
{
	public partial class DialogUI
	{
		public class Sequence
		{
			public readonly string header;
			public readonly string body;

			public Sequence(string header, string body)
			{
				this.header = header;
				this.body = body;
			}
		}
	}
}