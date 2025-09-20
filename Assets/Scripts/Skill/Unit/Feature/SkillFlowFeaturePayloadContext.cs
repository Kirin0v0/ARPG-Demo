using System;
using System.Collections.Generic;

namespace Skill.Unit.Feature
{
    public class SkillFlowFeaturePayloadContext : IDisposable
    {
        private readonly Dictionary<string, object> _payloads = new();

        public void SetPayload(string key, object payload)
        {
            _payloads[key] = payload;
        }

        public bool GetPayload(string key, out object payload)
        {
            return _payloads.TryGetValue(key, out payload);
        }

        public void Dispose()
        {
            _payloads.Clear();
        }
    }
}