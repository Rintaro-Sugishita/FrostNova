using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine;

public interface IResourceConstraint
{
    IEnumerable<(object? Id, double Efficiency)> GetLineCandidates();
    IEnumerable<(object? Id, double Efficiency)> GetMemberCandidates();
}
