using System;
using UnityEngine;

namespace VRBuilder.Editor.ProcessUpgradeTool
{
    public abstract class Converter<TIn, TOut> : IConverter where TIn : class where TOut : class
    {
        public Type ConvertedType => typeof(TIn);

        protected abstract TOut PerformConversion(TIn oldObject);

        public object Convert(object oldObject)
        {
            Debug.Log($"Replacing {typeof(TIn)} '{(TIn)oldObject}' with {typeof(TOut)}");
            return PerformConversion((TIn)oldObject);
        }
    }
}
