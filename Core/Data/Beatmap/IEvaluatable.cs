using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ILMath;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can have dynamic variables be obtained from it.
    /// </summary>
    public interface IEvaluatable
    {
        public void SetOtherObjectVariables(Dictionary<string, float> variables);

        public void SetObjectVariables(Dictionary<string, float> variables);

        public void SetObjectFunctions(Dictionary<string, MathFunction> functions);
    }
}
