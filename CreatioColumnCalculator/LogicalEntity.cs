namespace CreatioColumnCalculator
{
	using System.Collections.Generic;

	public class LogicalEntity
	{
		internal int Iterator { get; set; }

		public string EntitySchemaName { get; set; }

		public LogicalEntityStorageStatus StorageStatus { get; set; }
		public List<LogicalProperty> LogicalProperties { get; set; }
		public List<LogicalRule> LogicalRules { get; set; }

		public LogicalEntity() {
			Iterator = 0;
			LogicalProperties = new List<LogicalProperty>();
			LogicalRules = new List<LogicalRule>();
		}
	}
}


