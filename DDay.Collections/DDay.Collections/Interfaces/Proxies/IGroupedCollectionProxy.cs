﻿using System;

namespace DDay.Collections
{
    public interface IGroupedCollectionProxy<TGroup, TOriginal, TNew> :
        IGroupedCollection<TGroup, TNew>
        where TOriginal : class, IGroupedObject<TGroup>
        where TNew : class, TOriginal
    {
        IGroupedList<TGroup, TOriginal> RealObject { get; }
        void SetProxiedObject(IGroupedList<TGroup, TOriginal> realObject);        
    }
}
