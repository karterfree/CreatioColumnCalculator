using System.Globalization;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Factories;
using Terrasoft.Core.Process;

namespace CreatioColumnCalculator
{
	using System.Linq.Expressions;

	public class CalculatedItem<T>
	{
		public Expression Expression { get; set; }
		public string Description { get; set; }

	}
	public class LogicalEntityCalculator
	{
		private Dictionary<string, string> _functions;
		public static int MaxCalculationIterators = 2;
		private UserConnection _userConnection;

		public LogicalEntityCalculator(UserConnection userConnection) {
			_userConnection = userConnection;
			LoadFunctions();

		}

		private void LoadFunctions() {
			// Can be selected from DB
			_functions = new Dictionary<string, string>() {
				{ "Sqrt", "PartnerSolution.SqrtLogicalFunction" }
			};
		}

		public void Calculate(LogicalEntity logicalEntity) {
			ActualizeRulesDepending(logicalEntity);
			ActualizeRulesPosition(logicalEntity);
			CalculateRules(logicalEntity);
		}

		private void CalculateRules(LogicalEntity logicalEntity) {
			while (logicalEntity.LogicalRules.Any(x=>!x.CalculatingProperties.IsCalculationFinished)) {
				var logicalRule = logicalEntity.LogicalRules.Where(x => !x.CalculatingProperties.IsCalculationFinished)
					.OrderBy(x => x.Position).First();
				CalculateRule(logicalEntity, logicalRule);
			}
		}

		private void CalculateRule(LogicalEntity logicalEntity, LogicalRule logicalRule) {
			if (!logicalRule.CalculatingProperties.IsCalculationFinished) {
				logicalRule.CalculatingProperties.CalculationTryingIterator++;
				if (logicalRule.CalculatingProperties.CalculationTryingIterator > MaxCalculationIterators) {
					logicalRule.CalculatingProperties.IsCalculationFinished = true;
					logicalRule.CalculatingProperties.CalculationException = new Exception("ToDoType");
				}
			}
			if (logicalRule.CalculatingProperties.IsCalculationFinished) {
				return;
			}

			try {
				LogicalProperty targetProperty = GetPropertyByName(logicalEntity, logicalRule.TargetPropertyName);
				var propertyValue = CalculateRuleValue(logicalEntity, logicalRule);
				targetProperty.PropertyValue = propertyValue;
				targetProperty.IsChanged = true;
				logicalRule.CalculatingProperties.IsCalculationFinished = true;
			} catch (Exception e) {
				logicalRule.CalculatingProperties.CalculationException = e;
			}
		}

		private object CalculateRuleValue(LogicalEntity logicalEntity, LogicalRule logicalRule) {
			return CalculateTypedExpressionValue(logicalEntity, logicalRule.Expression);
		}

		private object CalculateTypedExpressionValue(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var method =
				LogicalReflectionUtilities.GetGenericMethod(GetType(), "CalculateTypedExpressionValue",
					logicalExpression.Type);
			return method?.Invoke(this, new object[] {logicalEntity, logicalExpression});
		}

		private T CalculateTypedExpressionValue<T>(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var expression = GenerateExpression(logicalEntity, logicalExpression);
			var lambda = Expression.Lambda<Func<T>>(expression);
			var func = lambda.Compile();
			return func();
		}

		private LogicalProperty GetPropertyByName(LogicalEntity logicalEntity, string propertyName) {
			var targetProperty =
				logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == propertyName);
			if (targetProperty == null) {
				throw new Exception("ToDoType");
			}

			return targetProperty;
		}

		private Expression GenerateExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			if (logicalExpression.NodeType == LogicalExpressionNodeType.ConstantExpression) {
				return GenerateConstantExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.PropertyExpression) {
				return GeneratePropertyExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.DivideExpression) {
				return GenerateDivideExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.MultipleExpression) {
				return GenerateMultipleExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.AddExpression) {
				return GenerateAddExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.SubtractExpression) {
				return GenerateSubtractExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.SelectQueryExpression) {
				return GenerateSelectQueryExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.CustomFunctionExpression) {
				return GenerateCustomFunctionExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.IfThenElseExpression) {
				return GenerateIfThenElseExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.GreaterOrEqual) {
				return GenerateGreaterOrEqualExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.LessOrEqual) {
				return GenerateLessOrEqualExpression(logicalEntity, logicalExpression);
			}
			if (logicalExpression.NodeType == LogicalExpressionNodeType.AndExpression) {
				return GenerateAndExpression(logicalEntity, logicalExpression);
			}
			throw new NotImplementedException();
		}

		private Expression GenerateAndExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.And(leftExpression, rightExpression);
		}

		private Expression GenerateLessOrEqualExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.LessThanOrEqual(leftExpression, rightExpression);
		}

		private Expression GenerateGreaterOrEqualExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.GreaterThanOrEqual(leftExpression, rightExpression);
		}

		private Expression GenerateIfThenElseExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var returnTarget = Expression.Label(logicalExpression.Type);
			var conditionExpression = GenerateExpression(logicalEntity, logicalExpression.ConditionExpression);
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			var ifThenElseExpression = Expression.IfThenElse(conditionExpression, Expression.Return(returnTarget, leftExpression),
				Expression.Return(returnTarget, rightExpression));
			return Expression.Block(ifThenElseExpression, Expression.Label(returnTarget, rightExpression));
		}

		private Expression GenerateCustomFunctionExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var customFunctionArguments = new Dictionary<string, object>();
			logicalExpression.Arguments.ForEach(x => {
				var argumentValue = CalculateTypedExpressionValue(logicalEntity, x.Value);
				customFunctionArguments.Add(x.Key, argumentValue);
			});

			var functionTypeName = _functions[logicalExpression.FunctionCode];
			var functionInstance = GetFunctionInstance(functionTypeName);
			var functionValue = functionInstance.Execute(_userConnection, customFunctionArguments);
			return Expression.Convert(Expression.Constant(functionValue), logicalExpression.Type);
		}

		private ICustomLogicalFunction GetFunctionInstance(string name) {
			var type = Type.GetType(name);
			if (type != null) {
				return (ICustomLogicalFunction)Activator.CreateInstance(type);
			}

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				type = assembly.GetType(name);
				if (type != null) {
					return (ICustomLogicalFunction)Activator.CreateInstance(type);
				}
			}

			return null;
		}

		private Expression GenerateSelectQueryExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var filter = logicalExpression.SelectQueryExpressionOptions?.Filter ?? "";
			logicalExpression.Arguments.ForEach(x => {
				var argumentValue = CalculateTypedExpressionValue(logicalEntity, x.Value);
				var preparedValue = ConvertSelectQueryExpressionArgumentToString(argumentValue);
				filter = filter.Replace($"[!#{x.Key}#!]", preparedValue);
			});

			var schema = _userConnection.EntitySchemaManager.GetInstanceByName(logicalExpression.SelectQueryExpressionOptions.RootSchemaName);
			var esq = new EntitySchemaQuery(schema) {UseAdminRights = false, RowCount = 1};
			var filterConverter = GetProcessDataContractFilterConverter(_userConnection);
			var filterCollection = filterConverter.ConvertToEntitySchemaQueryFilterItem(esq, filter);
			var resultColumnPath = logicalExpression.SelectQueryExpressionOptions.ResultColumnPath;

			if (logicalExpression.SelectQueryExpressionOptions.Operation ==
			    LogicalSelectQueryExpressionOperation.FirstRow) {
				var column = esq.AddColumn(resultColumnPath);
			}
			esq.Filters.Add(filterCollection);
			var method =
				LogicalReflectionUtilities.GetGenericMethod(GetType(), "GetScalarEsqValue",
					logicalExpression.Type);
			var value = method?.Invoke(this, new object[] {esq});
			return Expression.Convert(Expression.Constant(value), logicalExpression.Type);
		}

		private string ConvertSelectQueryExpressionArgumentToString(object argumentValue) {
			if (argumentValue == null) {
				return "\"\"";
			}

			if (argumentValue.GetType() == typeof(decimal)) {
				return ((decimal)argumentValue).ToString("0.########", CultureInfo.InvariantCulture);
			}

			return argumentValue.ToString();
		}

		private T GetScalarEsqValue<T>(EntitySchemaQuery esq) {
			var select = esq.GetSelectQuery(_userConnection);
			var value = select.ExecuteScalar<T>();
			return value;
		}

		private IProcessDataContractFilterConverter GetProcessDataContractFilterConverter(UserConnection userConnection) {
			var userConnectionArgument = new ConstructorArgument("userConnection", userConnection);
			return ClassFactory.Get<IProcessDataContractFilterConverter>(userConnectionArgument);
		}

		private Expression GenerateSubtractExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.Subtract(leftExpression, rightExpression);
		}

		private Expression GenerateAddExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			if (logicalExpression.LeftExpression.Type == typeof(string) &&
			    logicalExpression.RightExpression.Type == typeof(string)) {
				return Expression.Add(
					leftExpression,
					rightExpression,
					typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
			}
			return Expression.Add(leftExpression, rightExpression);
		}

		private Expression GenerateMultipleExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.Multiply(leftExpression, rightExpression);
		}

		private Expression GenerateDivideExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var leftExpression = GenerateExpression(logicalEntity, logicalExpression.LeftExpression);
			var rightExpression = GenerateExpression(logicalEntity, logicalExpression.RightExpression);
			return Expression.Divide(leftExpression, rightExpression);
		}

		private Expression GeneratePropertyExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			var property = GetPropertyByName(logicalEntity, logicalExpression.PropertyCode);
			return GenerateConstantExpression(property.PropertyValue, logicalExpression.Type);
		}

		private Expression GenerateConstantExpression(LogicalEntity logicalEntity, LogicalExpression logicalExpression) {
			return GenerateConstantExpression(logicalExpression.ConstantValue, logicalExpression.Type);
		}

		private Expression GenerateConstantExpression(object constantValue, Type constantType) {
			return Expression.Convert(Expression.Constant(constantValue), constantType);
		}

		private void ActualizeRulesDepending(LogicalEntity logicalEntity) {
			logicalEntity.LogicalRules.ForEach(logicalRule =>
				logicalRule.CalculatingProperties.ActualDependOn =
					GetActualDependOn(logicalEntity, logicalRule.TargetPropertyName));
		}

		private List<string> GetActualDependOn(LogicalEntity logicalEntity, string targetPropertyName) {
			var logicalRule = logicalEntity.LogicalRules.FirstOrDefault(x => x.TargetPropertyName == targetPropertyName);
			if (logicalRule == null) {
				return new List<string>();
			}
			if (logicalRule.CalculatingProperties.ActualDependOn != null) {
				return logicalRule.CalculatingProperties.ActualDependOn;
			}

			if (logicalRule.CalculatingProperties.IsDependOnActualizeRunning) {
				throw new Exception(
					$"Internal error. Rule dependency actualize for rule {targetPropertyName} already executed");
			}

			var actualDependOn = new List<string>();
			logicalRule.CalculatingProperties.IsDependOnActualizeRunning = true;
			logicalRule.DependOn.ForEach(propertyName => {
				AddRangeIfNotExists(actualDependOn, new List<string>() {propertyName});
				AddRangeIfNotExists(actualDependOn, GetActualDependOn(logicalEntity, propertyName));
			});
			return actualDependOn;
		}

		private void AddRangeIfNotExists(List<string> target, List<string> source) {
			source.ForEach(sourcePropertyName => {
				if (!target.Contains(sourcePropertyName)) {
					target.Add(sourcePropertyName);
				}
			});
		}

		private void ActualizeRulesPosition(LogicalEntity logicalEntity) {
			while (logicalEntity.LogicalRules.Any(x=>!x.CalculatingProperties.IsPositionActualizeFinished)) {
				var logicalRule = logicalEntity.LogicalRules
					.OrderBy(x => x.CalculatingProperties.PositionActualizeTryingIterator)
					.FirstOrDefault(x => !x.CalculatingProperties.IsPositionActualizeFinished);
				if (logicalRule != null) {
					ActualizeRulesPosition(logicalEntity, logicalRule.TargetPropertyName);
				}
			}
		}

		private void ActualizeRulesPosition(LogicalEntity logicalEntity, string targetPropertyName) {
			var logicalRule = logicalEntity.LogicalRules.FirstOrDefault(x => x.TargetPropertyName == targetPropertyName);
			if (logicalRule == null || logicalRule.CalculatingProperties.IsPositionActualizeFinished) {
				return;
			}
			if (!logicalRule.CalculatingProperties.IsPositionActualizeRunning) {
				logicalRule.CalculatingProperties.IsPositionActualizeRunning = true;
			}

			logicalRule.CalculatingProperties.PositionActualizeTryingIterator++;
			var hasNotActualized = false;
			var position = 0;
			logicalRule.CalculatingProperties.ActualDependOn.ForEach(dependOnPropertyName => {
				if (TryGetActualizedRule(logicalEntity, dependOnPropertyName, out var dependedRule)) {
					position += dependedRule?.Position ?? 0;
				} else {
					hasNotActualized = true;
				}
			});
			if (!hasNotActualized) {
				logicalRule.Position = position + 1;
				logicalRule.CalculatingProperties.IsPositionActualizeFinished = true;
			}
		}

		private bool TryGetActualizedRule(LogicalEntity logicalEntity, string targetPropertyName, out LogicalRule actualizedRule) {
			actualizedRule = null;
			var logicalRule = logicalEntity.LogicalRules.FirstOrDefault(x => x.TargetPropertyName == targetPropertyName);
			if (logicalRule == null) {
				return true;
			}
			ActualizeRulesPosition(logicalEntity, targetPropertyName);
			if (!logicalRule.CalculatingProperties.IsPositionActualizeFinished) {
				return false;
			}
			actualizedRule = logicalRule;
			return true;
		}
	}

}

