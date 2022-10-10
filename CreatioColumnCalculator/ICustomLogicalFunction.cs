using Terrasoft.Core;

namespace CreatioColumnCalculator
{
	public interface ICustomLogicalFunction
	{
		object Execute(UserConnection userConnection, Dictionary<string, object> arguments);
	}
}


