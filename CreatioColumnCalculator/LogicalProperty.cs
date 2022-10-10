namespace CreatioColumnCalculator
{
	public class LogicalProperty {
		public string Code { get; set; }
		public LogicalPropertyType PropertyType { get; set; }
		public Guid DataValueType { get; set; }
		public object PropertyValue { get; set; }
		public bool IsChanged { get; set; }

		public string Description { get; set; }
	}
}
