namespace CreatioColumnCalculator
{
	internal class LogicalRuleCalculatingProperties
	{
		internal List<string> ActualDependOn { get; set; }

		internal bool IsDependOnActualizeRunning { get; set; }
		internal bool IsPositionActualizeRunning { get; set; }
		internal bool IsPositionActualizeFinished { get; set; }
		internal int PositionActualizeTryingIterator { get; set; }

		internal bool IsCalculationFinished { get; set; }

		internal int CalculationTryingIterator { get; set; }

		internal bool CalculationSuccess { get; set; }

		internal Exception CalculationException { get; set; }

		internal LogicalRuleCalculatingProperties() {
			PositionActualizeTryingIterator = 0;
		}
	}
	public class LogicalRule
	{
		internal LogicalRuleCalculatingProperties CalculatingProperties { get; set; }

		public string TargetPropertyName { get; set; }
		public LogicalExpression Expression { get; set; }

		public List<string> DependOn { get; set; }

		internal int Position { get; set; }
		public LogicalRule() {
			Position = 0;
			CalculatingProperties = new LogicalRuleCalculatingProperties();
			DependOn = new List<string>();
		}
	}
}
