using System.Collections.Generic;
using WinFlow.Core.Model;

namespace WinFlow.Core.Parsing
{
    public interface IParser
    {
        List<FlowTask> Parse(string filePath);
    }
}