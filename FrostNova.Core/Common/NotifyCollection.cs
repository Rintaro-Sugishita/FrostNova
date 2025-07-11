using FrostNova.Core.Operators;
using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FrostNova.Core.Common
{
  public  class NotifyCollection<T> : ObservableCollection<T>, IOperationScope
    {

        public NotifyCollection()
        {
            }

        public NotifyCollection(IEnumerable<T> values)
        {
            AddRange(values);
        }

        [JsonIgnore, IgnoreCopy]
        public int RangeThreshold { get; set; } = 2;

        public void AddRange(IEnumerable<T> addItems)
        {

            if (Items.IsReadOnly)
            {
                throw new NotSupportedException();
            }
            int c = addItems.Count();
            if (c == 0) { return; }
            if (c < RangeThreshold)
            {
                foreach (var item in addItems)
                {
                    base.Add(item);
                }
            }
            else
            {
                foreach (var item in addItems)
                {
                    base.Items.Add(item);
                }
                NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                OnCollectionChanged(a);
            }
        }

        public void AddRange(params T[] addParams)
        {
            AddRange(addItems: addParams);
        }




        public void RemoveRange(IEnumerable<T> removeItems)
        {
            if (Items.IsReadOnly)
            {
                throw new NotSupportedException();
            }
            IList<T> removedItems = new List<T>();
            foreach (var item in removeItems)
            {
                if (base.Items.Remove(item))
                {
                    removedItems.Add(item);
                }
            }
            if (removedItems.Count() > 0)
            {
                NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

                OnCollectionChanged(a);
            }

        }

        [JsonIgnore]
        //public Action? CollectionChangedAction { get; set; }
        public Guid OperationId { get; set; }

        public void Refresh()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_isEditing) return;
            base.OnCollectionChanged(e);
            //CollectionChangedAction?.Invoke();
        }

        public void BeginEdit()
        {
            _isEditing = true;

        }

        [JsonIgnore]
        private bool _isEditing = false;

        public void EndEdit()
        {
            if (!_isEditing) return;

            _isEditing = false;
            Refresh();
        }

        public Guid StartOperation()
        {
            this.BeginEdit();
            return OperationScope.StartOperation(this);
        }


        public void EndOperation(Guid? operationId = null)
        {
            if (OperationScope.EndOperation(this, operationId))
            {
                this.EndEdit();
            }
        }

    }
}
