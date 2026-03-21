using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine;

public record BaseDateInfo(int Priority = 0,
    DateTime BaseDate = default,
    BaseDateMode Mode = BaseDateMode.None);
