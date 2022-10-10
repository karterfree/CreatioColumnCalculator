namespace CreatioColumnCalculator
{
	public class LogicalExpression
	{
		public LogicalExpressionNodeType NodeType { get; set; }
		public Type Type { get; set; }
		public object ConstantValue { get; set; }

		public LogicalConstantType ConstantType { get; set; }
		public string PropertyCode { get; set; }
		public LogicalExpression LeftExpression { get; set; }
		public LogicalExpression RightExpression { get; set; }

		public LogicalSelectQueryExpressionOptions SelectQueryExpressionOptions { get; set; }

		public Dictionary<string, LogicalExpression> Arguments { get; set; }
		public string FunctionCode { get; set; }
		public LogicalExpression ConditionExpression { get; set; }
	}
}
