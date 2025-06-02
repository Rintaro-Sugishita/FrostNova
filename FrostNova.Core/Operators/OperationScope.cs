using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Operators
{
    public class OperationScope : IDisposable
    {
        private readonly List<(IOperationScope scopeInstance, Guid operationId)> _instance = new List<(IOperationScope, Guid)>();

        public OperationScope(IOperationScope instance)
        {
            _instance.Add((instance, instance.StartOperation()));
        }

        public OperationScope(params IOperationScope[] instance)
        {

            foreach (var i in instance)
            {
                _instance.Add((i, i.StartOperation()));
            }
        }



        public void Dispose()
        {
            _instance.ForEach(x => x.scopeInstance.EndOperation(x.operationId));
        }


        public static Guid StartOperation(IOperationScope scope)
        {
            if (scope.OperationId == Guid.Empty)
            {
                scope.OperationId = Guid.NewGuid();
                return scope.OperationId;
            }
            return Guid.Empty;
        }

        public static bool EndOperation(IOperationScope scope, Guid? id)
        {
            if (scope.OperationId == id)
            {
                scope.OperationId = Guid.Empty;
                return true;
            }
            if (scope.OperationId == Guid.Empty)
            {
                return true;
            }
            return false;
        }
    }
}
