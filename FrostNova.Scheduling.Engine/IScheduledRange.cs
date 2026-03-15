using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine;

public interface IScheduledRange
{
    DateTime Start { get; set; }
    DateTime End { get; set; }

}
