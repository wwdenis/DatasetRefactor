using System.Reflection;
using DatasetRefactor.Extensions;

namespace DatasetRefactor.Metadata
{
    internal class ActionParameter
    {
        public ActionParameter()
        {
        }

        public ActionParameter(ParameterInfo info)
        {
            this.Name = info.Name;
            this.Type = info.ParameterType.GetCsName();
        }

        public string Name { get; set; }

        public string Type { get; set; }
    }
}
