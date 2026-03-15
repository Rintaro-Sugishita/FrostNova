using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine.Tests;

internal class Common
{
    private static string ToDispalyResult(IEnumerable<MyBom> nodes)
    {
        var sb = new StringBuilder();

        //NodeID
        foreach (var node in nodes)
        {
            sb.Append($"Id: {node.Id}");
            sb.Append($", Level: {node.Level}");
            sb.Append($", : {node.Priority}");
            sb.AppendLine();
            foreach (var p in node.ProcessSteps)
            {
                sb.Append("  ");
                sb.Append($"Id: {p.Id}");
                sb.Append($", Start: {p.Start}");
                sb.Append($", End: {p.End}");
                sb.Append($", Lead: {p.LeadTime}");
                sb.Append($", {p.StepOrder}");
                sb.Append($", {p.LineId}");
                sb.AppendLine();
            }
        }

        return sb.ToString();

    }

}
