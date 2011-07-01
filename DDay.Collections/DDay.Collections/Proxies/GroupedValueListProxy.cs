using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections;

namespace DDay.Collections
{
    /// <summary>
    /// A proxy for a keyed list.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public class GroupedValueListProxy<TGroup, TOriginal, TOriginalValue, TNewValue> :
        IList<TNewValue>
        where TOriginal : class, IGroupedObject<TGroup>, IValueObject<TOriginalValue>, new()
        where TNewValue : TOriginalValue
    {
        #region Private Fields

        GroupedValueList<TGroup, TOriginal, TOriginalValue> _RealObject;
        TGroup _Key;
        TOriginal _Container;

        #endregion

        #region Constructors

        public GroupedValueListProxy(GroupedValueList<TGroup, TOriginal, TOriginalValue> realObject, TGroup group)
        {
            _RealObject = realObject;
            _Key = group;
        }

        #endregion

        #region Private Methods

        TOriginal EnsureContainer()
        {
            if (_Container == null)
            {
                // Find an item that matches our group
                _Container = _RealObject.AllOf(_Key).FirstOrDefault();

                // If no item is found, create a new object and add it to the list
                if (_Container == default(TOriginal))
                {
                    _Container = new TOriginal();
                    _Container.Group = _Key;
                    _RealObject.Add(_Container);
                }
            }
            return _Container;
        }

        void IterateValues(Func<IValueObject<TOriginalValue>, int, int, bool> action)
        {
            int i = 0;
            foreach (var obj in _RealObject)
            {
                // Get the number of items of the target value i this object
                var count = obj.Values != null ? obj.Values.OfType<TNewValue>().Count() : 0;

                // Perform some action on this item
                if (!action(obj, i, count))
                    return;

                i += count;
            }
        }

        IValueObject<TOriginalValue> ObjectForIndex(int index, ref int relativeIndex)
        {
            IValueObject<TOriginalValue> obj = null;
            int retVal = -1;

            IterateValues((o, i, count) =>
                {
                    // Determine if this index is found within this object
                    if (index >= i && index < count)
                    {
                        retVal = index - i;
                        obj = o;
                        return false;
                    }
                    return true;
                });

            relativeIndex = retVal;
            return obj;            
        }

        IEnumerator<TNewValue> GetEnumeratorInternal()
        {
            return _RealObject
                .AllOf(_Key)
                .Where(o => o.ValueCount > 0)
                .SelectMany(o => o.Values)
                .OfType<TNewValue>()
                .GetEnumerator();
        }

        #endregion

        #region IList<TNewValue> Members

        virtual public void Add(TNewValue item)
        {
            // Add the value to the object
            EnsureContainer().AddValue(item);
        }

        virtual public void Clear()
        {
            var items = _RealObject
                .AllOf(_Key)
                .Where(o => o.Values != null);

            foreach (TOriginal original in items)
            {
                // Clear all values from each matching object
                original.SetValue(default(TOriginalValue));
            }
        }

        virtual public bool Contains(TNewValue item)
        {
            return _RealObject
                .AllOf(_Key)
                .Where(o => o.ContainsValue(item))
                .Any();
        }

        virtual public void CopyTo(TNewValue[] array, int arrayIndex)
        {
            _RealObject
                .AllOf(_Key)
                .Where(o => o.Values != null)
                .SelectMany(o => o.Values)
                .ToArray()
                .CopyTo(array, arrayIndex);
        }
        
        virtual public int Count
        {
            get
            {
                return _RealObject
                    .AllOf(_Key)
                    .Sum(o => o.ValueCount);
            }
        }

        virtual public bool IsReadOnly
        {
            get { return false; }
        }

        virtual public bool Remove(TNewValue item)
        {
            var container = _RealObject
                .AllOf(_Key)
                .Where(o => o.ContainsValue(item))
                .FirstOrDefault();

            if (container != null)
            {
                container.RemoveValue(item);
                return true;
            }
            return false;
        }

        virtual public IEnumerator<TNewValue> GetEnumerator()
        {
            return GetEnumeratorInternal();        
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        virtual public int IndexOf(TNewValue item)
        {
            int index = -1;

            IterateValues((o, i, count) =>
                {
                    if (o.Values != null && o.Values.Contains(item))
                    {
                        var list = o.Values.ToList();
                        index = i + list.IndexOf(item);
                        return false;
                    }
                    return true;
                });

            return index;
        }

        virtual public void Insert(int index, TNewValue item)
        {
            IterateValues((o, i, count) =>
                {
                    // Determine if this index is found within this object
                    if (index >= i && index < count)
                    {
                        // Convert the items to a list
                        var items = o.Values.ToList();
                        // Insert the item at the relative index within the list
                        items.Insert(index - i, item);
                        // Set the new list
                        o.SetValue(items);
                        return false;
                    }
                    return true;
                });
        }

        virtual public void RemoveAt(int index)
        {
            IterateValues((o, i, count) =>
            {
                // Determine if this index is found within this object
                if (index >= i && index < count)
                {
                    // Convert the items to a list
                    var items = o.Values.ToList();
                    // Remove the item at the relative index within the list
                    items.RemoveAt(index - i);
                    // Set the new list
                    o.SetValue(items);
                    return false;
                }
                return true;
            });
        }

        virtual public TNewValue this[int index]
        {
            get
            {
                if (index >= 0 && index < Count)
                {
                    return this
                        .Skip(index)
                        .FirstOrDefault();
                }
                return default(TNewValue);
            }
            set
            {
                if (index >= 0 && index < Count)
                {   
                    if (!object.Equals(value, default(TNewValue)))
                    {
                        Insert(index, value);
                        index++;
                    }
                    RemoveAt(index);
                }
            }
        }

        #endregion
    }
}