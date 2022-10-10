namespace CreatioColumnCalculator
{
	public class LogicalSelectQueryExpressionOptions
	{
		public string RootSchemaName { get; set; }
		public string ResultColumnPath { get; set; }
		public LogicalSelectQueryExpressionOperation Operation { get; set; }
		public string Filter { get; set; }
	}
}

