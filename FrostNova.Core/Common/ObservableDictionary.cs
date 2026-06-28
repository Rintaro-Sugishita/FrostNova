using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FrostNova.Core.Common
{
    /// <summary>
    /// A dictionary that implements <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>,
    /// enabling WPF bindings to react to add, remove, and replace operations.
    /// </summary>
    public class ObservableDictionary<TKey, TValue> : CollectionBasedDictionary<TKey, TValue?>, INotifyCollectionChanged, INotifyPropertyChanged, ICloneable where TKey : notnull
    {
        #region Constructor
        /// <summary>Initializes an empty dictionary with the default equality comparer for <typeparamref name="TKey"/>.</summary>
        public ObservableDictionary()
            : base()
        {
        }

        /// <summary>Initializes a dictionary copied from <paramref name="dictionary"/> using the default equality comparer.</summary>
        public ObservableDictionary(IDictionary<TKey, TValue?> dictionary)
            : base(dictionary)
        {
        }

        /// <summary>Initializes an empty dictionary using the specified <paramref name="comparer"/>.</summary>
        public ObservableDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
        }

        /// <summary>Initializes a dictionary copied from <paramref name="dictionary"/> using the specified <paramref name="comparer"/>.</summary>
        public ObservableDictionary(IDictionary<TKey, TValue?> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer)
        {
        }
        #endregion // Constructor

        #region Field
        private const string DictionaryName = "Dictionary";
        private const string ItemsName = "Item[]";
        private const string KeysName = "Keys[]";
        private const string ValuesName = "Values[]";
        private const string CountName = "Count";
        #endregion // Field

        #region Event
        /// <summary>Raised when an item is added, removed, changed, or the entire list is refreshed.</summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>Raised when a property value changes.</summary>
        protected virtual event PropertyChangedEventHandler? PropertyChanged;

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }
            remove
            {
                PropertyChanged -= value;
            }
        }

        /// <summary>
        ///   指定された引数を使用して、
        ///   <see cref="E:System.Collections.Generic.ObservableDictionary`2.PropertyChanged" /> イベントを発生させます。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティの名前。</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        ///   指定された引数を使用して、
        ///   <see cref="E:System.Collections.Generic.ObservableDictionary`2.CollectionChanged" /> イベントを発生させます。
        /// </summary>
        /// <param name="e">発生させるイベントの引数。</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        private void OnCollectionAdded(KeyValuePair<TKey, TValue?> changedItem, int startingIndex)
        {
            OnPropertyChanged(DictionaryName);
            OnPropertyChanged(ItemsName);
            OnPropertyChanged(CountName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItem, startingIndex));
        }

        private void OnCollectionRemoved(KeyValuePair<TKey, TValue?> changedItem, int startingIndex)
        {
            OnPropertyChanged(DictionaryName);
            OnPropertyChanged(ItemsName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
            OnPropertyChanged(CountName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItem, startingIndex));
        }

        private void OnCollectionMoved(KeyValuePair<TKey, TValue?> changedItem, int index, int oldIndex)
        {
            OnPropertyChanged(DictionaryName);
            OnPropertyChanged(ItemsName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, changedItem, index, oldIndex));
        }

        private void OnCollectionReplaced(KeyValuePair<TKey, TValue?> newItem, KeyValuePair<TKey, TValue?> oldItem)
        {
            OnPropertyChanged(DictionaryName);
            OnPropertyChanged(ItemsName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem));
        }

        private void OnCollectionReset()
        {
            OnPropertyChanged(DictionaryName);
            OnPropertyChanged(ItemsName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
            OnPropertyChanged(CountName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        #endregion // Event

        #region Method
        /// <summary>
        ///   指定したインデックス位置の項目を、指定した項目で置き換えます。
        /// </summary>
        /// <param name="index">置き換えられる項目の 0 から始まるインデックス。</param>
        /// <param name="item">新しい項目。</param>
        protected override void SetItem(int index, KeyValuePair<TKey, TValue?> item)
        {
            KeyValuePair<TKey, TValue?> oldItem = this[index];
            base.SetItem(index, item);
            OnCollectionReplaced(item, oldItem);
        }

        /// <summary>
        ///   <see cref="T:System.Collections.Generic.ObservableDictionary`2" /> 内の指定したインデックスの位置に要素を挿入します。
        /// </summary>
        /// <param name="index">
        ///   <paramref name="item" /> を挿入する位置の、0 から始まるインデックス番号。
        /// </param>
        /// <param name="item">
        ///   挿入するオブジェクト。
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" /> が 0 未満です。
        ///   または<paramref name="index" /> が <see cref="P:System.Collections.Generic.ObservableDictionary`2Count" /> より大きくなっています。
        /// </exception>
        protected override void InsertItem(int index, KeyValuePair<TKey, TValue?> item)
        {
            if (!this.ContainsKey(item.Key))
            {
                base.InsertItem(index, item);
                OnCollectionAdded(item, index);
            }
        }

        /// <summary>
        ///   <see cref="T:System.Collections.Generic.ObservableDictionary`2" /> の指定したインデックスにある要素を削除します。
        /// </summary>
        /// <param name="index">削除する要素のインデックス。</param>
        protected override void RemoveItem(int index)
        {
            KeyValuePair<TKey, TValue?> removedItem = this[index];
            base.RemoveItem(index);
            OnCollectionRemoved(removedItem, index);
        }

        /// <summary>
        ///   <see cref="T:System.Collections.Generic.ObservableDictionary`2" /> からすべての要素を削除します。
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();
            OnCollectionReset();
        }

        public object Clone()
        {

            var DestDic = new ObservableDictionary<TKey, TValue>();
            foreach (var item in this)
            {
                var destValue = TypeToolkit.DeepCloneHelper.DeepCopy(item);
                DestDic.Add(destValue);
            }

            return DestDic;
        }

        #endregion // Method
    }

}
