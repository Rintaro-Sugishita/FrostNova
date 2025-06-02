using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Operators
{
    public interface IOperationScope
    {
        Guid StartOperation();

        void EndOperation(Guid? operationId = null);

        Guid OperationId { get; set; }

    }
}
