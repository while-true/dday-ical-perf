using System;
using System.Collections.Generic;
using System.Text;

namespace DDay.Collections
{
    public interface IKeyedObject<T>
    {
        event EventHandler<ObjectEventArgs<T, T>> KeyChanged;
        T Key { get; set; }
    }
}