using System;
using System.Collections.Generic;
using Animancer;

namespace Util
{
    public static class AnimancerUtil
    {
        public static List<(int index, System.Action callback)> AddCallbacksWithResult<T>(this AnimancerEvent.Sequence sequence, StringReference name, Action<T> callback)
        {
            var result = new List<(int index, System.Action callback)>();
            var index = -1;
            while (true)
            {
                index = sequence.IndexOf(name, index + 1);
                if (index < 0)
                    return result;
                
                var newCallback = sequence.AddCallback(index, callback);
                result.Add(new ValueTuple<int, System.Action>(index, newCallback));
            }
        }

        public static void RemoveCallbacksWithResult(this AnimancerEvent.Sequence sequence,
            List<(int index, System.Action callback)> callbacks)
        {
            foreach (var tuple in callbacks)
            {
                sequence.RemoveCallback(tuple.index, tuple.callback);
            }
        }
    }
}