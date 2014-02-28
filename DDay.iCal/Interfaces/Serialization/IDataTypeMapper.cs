using System;
using System.Collections.Generic;
using System.Text;

namespace DDay.iCal.Serialization
{
    public interface IDataTypeMapper
    {
        bool GetPropertyAllowsMultipleValues(object obj);
        Type GetPropertyMapping(object obj);
    }
}
