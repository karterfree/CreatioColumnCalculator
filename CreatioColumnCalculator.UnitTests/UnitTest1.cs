using CreatioColumnCalculator;
using PartnerSolution;
using Terrasoft.Common;
using Terrasoft.Configuration.Tests;
using Terrasoft.Core;
using Terrasoft.Core.Factories;
using Terrasoft.Core.Process;
using Terrasoft.Nui.ServiceModel.DataContract;
using Terrasoft.UnitTest;

namespace PartnerSolution
{
	public class SqrtLogicalFunction: ICustomLogicalFunction
	{
		public object Execute(UserConnection userConnection, Dictionary<string, object> arguments) {
			var arg = Convert.ToDouble(arguments["value"]);
			return (decimal)Math.Round(Math.Sqrt(arg), 4);
		}
	}
}

public class SimpleCalculationCase
{
	public object LeftValue { get; set; }
	public object RightValue { get; set; }
	public object ExpectedValue { get; set; }
	public LogicalExpressionNodeType Operation { get; set; }

	public SimpleCalculationCase(LogicalExpressionNodeType operation, object leftValue, object rightValue,
		object expectedValue) {
		LeftValue = leftValue;
		RightValue = rightValue;
		Operation = operation;
		ExpectedValue = expectedValue;
	}
}

namespace CreatioColumnCalculator.UnitTests
{
	using NUnit.Framework;
	using Terrasoft.Core;



	[TestFixture]
	[MockSettings(RequireMock.DBEngine)]
	public class Tests: BaseConfigurationTestFixture
	{
		private LogicalEntityCalculator _logicalEntityCalculator;

		[SetUp]
		public void Setup() {
			base.SetUp();
			var sqrtLogicalFunction = new SqrtLogicalFunction();
			AddCustomizedEntitySchemas();
			ReBindClassFactory();
			_logicalEntityCalculator = new LogicalEntityCalculator(UserConnection);
		}

		private void ReBindClassFactory() {
			ClassFactory.RebindWithFactoryMethod<IProcessDataContractFilterConverter>(() =>
				new ProcessDataContractFilterConverter(UserConnection));
		}

		private void AddCustomizedEntitySchemas() {
			EntitySchemaManager.AddCustomizedEntitySchema("Order", new Dictionary<string, string>() {
				{"Amount", "Float2"}
			});
			var leadSchema = EntitySchemaManager.AddCustomizedEntitySchema("Lead", new Dictionary<string, string>());
			leadSchema.AddLookupColumn("Order", "Order");
		}

		private IProcessDataContractFilterConverter GetProcessDataContractFilterConverter(TestUserConnection userConnection) {
			var userConnectionArgument = new ConstructorArgument("userConnection", userConnection);
			return ClassFactory.Get<IProcessDataContractFilterConverter>(userConnectionArgument);
		}

		private LogicalEntity GetSimpleOperationLogicalEntity(LogicalExpressionNodeType operationLogicalExpressionNodeType) {
			return new LogicalEntity() {
				EntitySchemaName = "Order",
				StorageStatus = LogicalEntityStorageStatus.New,
				LogicalProperties = new List<LogicalProperty>() {
					new LogicalProperty() {
						Code = "Amount",
						PropertyType = LogicalPropertyType.EntityColumn,
						DataValueType = IntegerDataValueType.Float2DataValueTypeUId,
						PropertyValue = 110.15m,
						IsChanged = false
					},
					new LogicalProperty() {
						Code = "TotalAmount",
						PropertyType = LogicalPropertyType.EntityColumn,
						DataValueType = IntegerDataValueType.Float2DataValueTypeUId,
						PropertyValue = 0,
						IsChanged = false
					},
					new LogicalProperty() {
						Code = "Coefficient",
						PropertyType = LogicalPropertyType.Variable,
						DataValueType = IntegerDataValueType.Float2DataValueTypeUId,
						PropertyValue = 0,
						IsChanged = false
					}
				},
				LogicalRules = new List<LogicalRule>() {
					new LogicalRule() {
						TargetPropertyName = "Coefficient",
						Expression = new LogicalExpression() {
							NodeType = LogicalExpressionNodeType.ConstantExpression,
							ConstantType = LogicalConstantType.Valuable,
							Type = typeof(decimal),
							ConstantValue = 10
						}
					},
					new LogicalRule() {
						TargetPropertyName = "TotalAmount",
						Expression = new LogicalExpression() {
							NodeType = operationLogicalExpressionNodeType, //LogicalExpressionNodeType.DivideExpression
							Type = typeof(decimal),
							LeftExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "Amount"
							},
							RightExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "Coefficient"
							}
						},
						DependOn = new List<string>() {"Amount", "Coefficient"}
					}
				}
			};
		}

		protected void SetUpTestData(string schemaName, Action<SelectData> filterAction, params Dictionary<string, object>[] items) {
			var selectData = new SelectData(UserConnection, schemaName);
			items.ForEach(values => selectData.AddRow(values));
			filterAction.Invoke(selectData);
			selectData.MockUp();
		}

		protected void SetUpScalarTestData<T>(string schemaName, Action<SelectData> filterAction, T value) {
			var selectData = new SelectData(UserConnection, schemaName);
			filterAction.Invoke(selectData);
			selectData.MockScalar<T>(value);
		}

		[Test]
		// Coefficient = 10;
		public void Calculate_WhenHasConstantValueRule_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.DivideExpression);
			_logicalEntityCalculator.Calculate(logicalEntity);
			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "Coefficient");
			Assert.AreEqual(10m, totalAmountProperty?.PropertyValue ?? 0m);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount / Coefficient;
		public void Calculate_WhenSimpleDivide_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.DivideExpression);
			_logicalEntityCalculator.Calculate(logicalEntity);
			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "TotalAmount");
			Assert.AreEqual(11.015m, totalAmountProperty?.PropertyValue ?? 0m);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount * Coefficient;
		public void Calculate_WhenSimpleMultiple_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.MultipleExpression);
			_logicalEntityCalculator.Calculate(logicalEntity);
			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "TotalAmount");
			Assert.AreEqual(1101.5m, totalAmountProperty?.PropertyValue ?? 0m);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount + Coefficient;
		public void Calculate_WhenSimpleAdding_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.AddExpression);
			_logicalEntityCalculator.Calculate(logicalEntity);

			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "TotalAmount");
			Assert.AreEqual(120.15m, totalAmountProperty?.PropertyValue ?? 0m);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount - Coefficient;
		public void Calculate_WhenSimpleSubtract_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.SubtractExpression);

			//Act
			_logicalEntityCalculator.Calculate(logicalEntity);

			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "TotalAmount");
			Assert.AreEqual(100.15m, totalAmountProperty?.PropertyValue ?? 0m);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount / Coefficient;
		// PrimaryOrder = SELECT Lead.Order.Id FROM Lead WHERE Order.Total > TotalAmount;
		public void Calculate_WhenSimpleSelect_ShouldCorrectCalculate() {
			var expectedPrimaryOrderId = Guid.NewGuid();
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.DivideExpression);
			logicalEntity.LogicalProperties.Add(
			new LogicalProperty() {
				Code = "PrimaryOrder",
				PropertyType = LogicalPropertyType.Variable,
				DataValueType = IntegerDataValueType.LookupDataValueTypeUId,
				PropertyValue = null,
				IsChanged = false
			});
			logicalEntity.LogicalRules.Add(new LogicalRule() {
				TargetPropertyName = "PrimaryOrder",
				Expression = new LogicalExpression() {
					NodeType = LogicalExpressionNodeType.SelectQueryExpression,
					Type = typeof(Guid),
					SelectQueryExpressionOptions = new LogicalSelectQueryExpressionOptions() {
						RootSchemaName = "Lead",
						ResultColumnPath = "Order.Id",
						Operation = LogicalSelectQueryExpressionOperation.FirstRow,
						Filter =
							"{\"className\":\"Terrasoft.FilterGroup\",\"items\":{\"a09ad297-60e6-43db-97f9-ad3e464ff50c\":{\"className\":\"Terrasoft.CompareFilter\",\"filterType\":1,\"comparisonType\":7,\"isEnabled\":true,\"trimDateTimeParameterToDate\":false,\"leftExpression\":{\"className\":\"Terrasoft.ColumnExpression\",\"expressionType\":0,\"columnPath\":\"Order.Amount\"},\"isAggregative\":false,\"key\":\"a09ad297-60e6-43db-97f9-ad3e464ff50c\",\"dataValueType\":6,\"leftExpressionCaption\":\"Order.Total\",\"rightExpression\":{\"className\":\"Terrasoft.ParameterExpression\",\"expressionType\":2,\"parameter\":{\"className\":\"Terrasoft.Parameter\",\"dataValueType\":6,\"value\":[!#P0#!]}}}},\"logicalOperation\":0,\"isEnabled\":true,\"filterType\":6,\"rootSchemaName\":\"Lead\",\"key\":\"\"}"
					},
					Arguments = new Dictionary<string, LogicalExpression>() {
						{
							"P0", new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "TotalAmount"
							}
						}
					}

				},
				DependOn = new List<string>() { "TotalAmount" }
			});

			SetUpScalarTestData("Lead", data => data.Has(11.015m), expectedPrimaryOrderId);

			_logicalEntityCalculator.Calculate(logicalEntity);

			var totalAmountProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "PrimaryOrder");
			Assert.AreEqual(expectedPrimaryOrderId, totalAmountProperty?.PropertyValue ?? Guid.Empty);
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount / Coefficient;
		// Principal = Sqrt(TotalAmount)
		public void Calculate_WhenSimpleCustomFunction_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.DivideExpression);
			logicalEntity.LogicalProperties.Add(
			new LogicalProperty() {
				Code = "Principal",
				PropertyType = LogicalPropertyType.Variable,
				DataValueType = IntegerDataValueType.Float2DataValueTypeUId,
				PropertyValue = null,
				IsChanged = false
			});
			logicalEntity.LogicalRules.Add(new LogicalRule() {
				TargetPropertyName = "Principal",
				Expression = new LogicalExpression() {
					NodeType = LogicalExpressionNodeType.CustomFunctionExpression,
					Type = typeof(decimal),
					FunctionCode = "Sqrt",
					Arguments = new Dictionary<string, LogicalExpression>() {
						{
							"value", new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "TotalAmount"
							}
						}
					}

				},
				DependOn = new List<string>() { "TotalAmount" }
			});

			_logicalEntityCalculator.Calculate(logicalEntity);

			var principalParameter = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "Principal");
			Assert.AreEqual(3.3189m, principalParameter?.PropertyValue ?? 0m);
		}

		[Test]
		// Principal = "abc" + "def";
		public void Calculate_WhenSimpleAddingStrings_ShouldCorrectCalculate() {
			var logicalEntity = GetSimpleOperationLogicalEntity(LogicalExpressionNodeType.AddExpression);
			logicalEntity.LogicalProperties.Add(
				new LogicalProperty() {
					Code = "Principal",
					PropertyType = LogicalPropertyType.Variable,
					DataValueType = IntegerDataValueType.TextDataValueTypeUId,
					PropertyValue = "",
					IsChanged = false
				});
			logicalEntity.LogicalRules.Add(new LogicalRule() {
				TargetPropertyName = "Principal",
				Expression = new LogicalExpression() {
					NodeType = LogicalExpressionNodeType.AddExpression,
					Type = typeof(string),
					LeftExpression = new LogicalExpression() {
						NodeType = LogicalExpressionNodeType.ConstantExpression,
						ConstantType = LogicalConstantType.Valuable,
						Type = typeof(string),
						ConstantValue = "abc"
					},
					RightExpression = new LogicalExpression() {
						NodeType = LogicalExpressionNodeType.ConstantExpression,
						ConstantType = LogicalConstantType.Valuable,
						Type = typeof(string),
						ConstantValue = "def"
					}

				},
				DependOn = new List<string>() { }
			});

			_logicalEntityCalculator.Calculate(logicalEntity);

			var principalProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "Principal");
			Assert.AreEqual("abcdef", principalProperty?.PropertyValue ?? "");
		}

		[Test]
		// Coefficient = 10;
		// TotalAmount = Amount * Coefficient;
		// IF (TotalAmount >= 1000 && TotalAmount <= 2000) {
		//    Principal = 100;
		// } else {
		//    Principal = 100 / 10;
		// }
		[TestCase(LogicalExpressionNodeType.MultipleExpression, 100, 10, 100)]
		[TestCase(LogicalExpressionNodeType.DivideExpression, 100, 10, 10)]
		public void Calculate_WhenSimpleCondition_ShouldCorrectCalculate(LogicalExpressionNodeType logicalExpressionNodeType, decimal trueValue, decimal falseValue, decimal expectedValue) {
			var logicalEntity = GetSimpleOperationLogicalEntity(logicalExpressionNodeType);
			logicalEntity.LogicalProperties.Add(
				new LogicalProperty() {
					Code = "Principal",
					PropertyType = LogicalPropertyType.Variable,
					DataValueType = IntegerDataValueType.Float2DataValueTypeUId,
					PropertyValue = 0m,
					IsChanged = false
				});
			logicalEntity.LogicalRules.Add(new LogicalRule() {
				TargetPropertyName = "Principal",
				Expression = new LogicalExpression() {
					NodeType = LogicalExpressionNodeType.IfThenElseExpression,
					Type = typeof(decimal),
					ConditionExpression = new LogicalExpression() {
						NodeType = LogicalExpressionNodeType.AndExpression,
						LeftExpression = new LogicalExpression() {
							NodeType = LogicalExpressionNodeType.GreaterOrEqual,
							LeftExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "TotalAmount"
							},
							RightExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.ConstantExpression,
								ConstantType = LogicalConstantType.Valuable,
								Type = typeof(decimal),
								ConstantValue = 1000m
							}
						},
						RightExpression = new LogicalExpression() {
							NodeType = LogicalExpressionNodeType.LessOrEqual,
							LeftExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = typeof(decimal),
								PropertyCode = "TotalAmount"
							},
							RightExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.ConstantExpression,
								ConstantType = LogicalConstantType.Valuable,
								Type = typeof(decimal),
								ConstantValue = 2000m
							}
						}
					},
					LeftExpression = new LogicalExpression() {
						NodeType = LogicalExpressionNodeType.ConstantExpression,
						ConstantType = LogicalConstantType.Valuable,
						Type = typeof(decimal),
						ConstantValue = trueValue
					},
					RightExpression = new LogicalExpression() {
						NodeType = LogicalExpressionNodeType.DivideExpression,
						Type = typeof(decimal),
						LeftExpression = new LogicalExpression() {
							NodeType = LogicalExpressionNodeType.ConstantExpression,
							ConstantType = LogicalConstantType.Valuable,
							Type = typeof(decimal),
							ConstantValue = trueValue
						},
						RightExpression = new LogicalExpression() {
							NodeType = LogicalExpressionNodeType.ConstantExpression,
							ConstantType = LogicalConstantType.Valuable,
							Type = typeof(decimal),
							ConstantValue = falseValue
						},
					}
				},
				DependOn = new List<string>() { "TotalAmount" }
			});

			_logicalEntityCalculator.Calculate(logicalEntity);

			var principalProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "Principal");
			Assert.AreEqual(expectedValue, principalProperty?.PropertyValue ?? 0m);
		}

		private static IEnumerable<SimpleCalculationCase> GetSimpleCalculationCases() {
			yield return new SimpleCalculationCase(LogicalExpressionNodeType.AddExpression, 10.0m, 10.0m, 20.0m);
			yield return new SimpleCalculationCase(LogicalExpressionNodeType.AddExpression, 10.15m, 10.15m, 20.30m);
			yield return new SimpleCalculationCase(LogicalExpressionNodeType.AddExpression, 10, 10, 20);
			yield return new SimpleCalculationCase(LogicalExpressionNodeType.AddExpression, 10, -10, 0);
		}

		[Test, TestCaseSource(nameof(GetSimpleCalculationCases))]
		public void Calculate_WhenSimpleCalculatedValues_ShouldCorrectCalculate(SimpleCalculationCase testCase) {
			var logicalEntity = new LogicalEntity() {
				EntitySchemaName = "Order",
				StorageStatus = LogicalEntityStorageStatus.New,
				LogicalProperties = new List<LogicalProperty>() {
					new LogicalProperty() {
						Code = "Left",
						PropertyType = LogicalPropertyType.EntityColumn,
						PropertyValue = testCase.LeftValue,
						IsChanged = false
					},
					new LogicalProperty() {
						Code = "Right",
						PropertyType = LogicalPropertyType.EntityColumn,
						PropertyValue = testCase.RightValue,
						IsChanged = false
					},
					new LogicalProperty() {
						Code = "Expected",
						PropertyType = LogicalPropertyType.Variable,
						IsChanged = false
					}
				},
				LogicalRules = new List<LogicalRule>() {
					new LogicalRule() {
						TargetPropertyName = "Expected",
						Expression = new LogicalExpression() {
							NodeType = testCase.Operation,
							Type = testCase.ExpectedValue.GetType(),
							LeftExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = testCase.LeftValue?.GetType() ?? typeof(object),
								PropertyCode = "Left"
							},
							RightExpression = new LogicalExpression() {
								NodeType = LogicalExpressionNodeType.PropertyExpression,
								Type = testCase.RightValue?.GetType() ?? typeof(object),
								PropertyCode = "Right"
							}
						},
						DependOn = new List<string>() {"Left", "Right"}
					}
				}
			};
			_logicalEntityCalculator.Calculate(logicalEntity);
			var expectedProperty = logicalEntity.LogicalProperties.FirstOrDefault(x => x.Code == "Expected");
			Assert.AreEqual(testCase.ExpectedValue, expectedProperty?.PropertyValue ?? default);
		}
	}


}


