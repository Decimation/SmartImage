namespace SmartImage
{
	public sealed class CliCommand
	{
		public string Parameter { get; internal set; }

		public string Syntax { get; internal set; }
		
		public string Description { get; internal set; }
		
		public Cli.RunCommand Action { get; internal set; }

		public override string ToString()
		{
			return string.Format("{0}\nUsage: {1} {2}", Description, Parameter, Syntax);
		}
	}
}