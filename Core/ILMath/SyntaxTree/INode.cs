using System.Collections.Generic;

namespace ILMath.SyntaxTree
{
    public interface INode
    {
        IEnumerable<INode> EnumerateChildren();
    }
}