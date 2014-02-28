using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;

namespace DDay.iCal
{
    public class SerializationUtil
    {
        #region Static Public Methods

        static public object GetUninitializedObject(Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }

        static public void OnDeserializing(object obj)
        {
            StreamingContext ctx = new StreamingContext(StreamingContextStates.All);
            foreach (MethodInfo mi in GetDeserializingMethods(obj.GetType()))
                mi.Invoke(obj, new object[] { ctx });
        }

        static public void OnDeserialized(object obj)
        {
            StreamingContext ctx = new StreamingContext(StreamingContextStates.All);
            foreach (MethodInfo mi in GetDeserializedMethods(obj.GetType()))
                mi.Invoke(obj, new object[] { ctx });
        } 

        #endregion

        #region Static Private Methods

        private static ConcurrentDictionary<Type, List<MethodInfo>> DeserializingMethods = new ConcurrentDictionary<Type, List<MethodInfo>>();

        static private IEnumerable<MethodInfo> GetDeserializingMethods(Type targetType)
        {
            if (targetType != null)
            {
                List<MethodInfo> methods;
                if (DeserializingMethods.TryGetValue(targetType, out methods)) return methods;
                methods = new List<MethodInfo>();

                // FIXME: cache this
                foreach (MethodInfo mi in targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    object[] attrs = mi.GetCustomAttributes(typeof (OnDeserializingAttribute), false);
                    if (attrs.Length > 0)
                    {
                        methods.Add(mi);
                    }
                }

                DeserializingMethods.TryAdd(targetType, methods);
                return methods;
            }

            return new List<MethodInfo>();
        }

        private static ConcurrentDictionary<Type, List<MethodInfo>> DeserializedMethods = new ConcurrentDictionary<Type, List<MethodInfo>>();

        static private IEnumerable<MethodInfo> GetDeserializedMethods(Type targetType)
        {
            if (targetType != null)
            {
                List<MethodInfo> methods;
                if (DeserializedMethods.TryGetValue(targetType, out methods)) return methods;
                methods = new List<MethodInfo>();

                // FIXME: cache this
                foreach (MethodInfo mi in targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    object[] attrs = mi.GetCustomAttributes(typeof(OnDeserializedAttribute), true);
                    if (attrs.Length > 0)
                    {
                        methods.Add(mi);
                    }
                }

                DeserializedMethods.TryAdd(targetType, methods);
                return methods;
            }

            return new List<MethodInfo>();
        } 

        #endregion
    }
}
