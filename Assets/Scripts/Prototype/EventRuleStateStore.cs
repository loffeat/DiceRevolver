using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class EventRuleStateStore
    {
        private readonly Dictionary<(ScriptableObject, string), object> values = new();

        public int GetInt(ScriptableObject owner, string key, int fallback = 0)
        {
            ValidateKey(owner, key);
            return values.TryGetValue((owner, key), out object value) && value is int number
                ? number
                : fallback;
        }

        public void SetInt(ScriptableObject owner, string key, int value)
        {
            ValidateKey(owner, key);
            values[(owner, key)] = value;
        }

        public bool GetBool(ScriptableObject owner, string key, bool fallback = false)
        {
            ValidateKey(owner, key);
            return values.TryGetValue((owner, key), out object value) && value is bool flag
                ? flag
                : fallback;
        }

        public void SetBool(ScriptableObject owner, string key, bool value)
        {
            ValidateKey(owner, key);
            values[(owner, key)] = value;
        }

        public float GetFloat(ScriptableObject owner, string key, float fallback = 0f)
        {
            ValidateKey(owner, key);
            return values.TryGetValue((owner, key), out object value) && value is float number
                ? number
                : fallback;
        }

        public void SetFloat(ScriptableObject owner, string key, float value)
        {
            ValidateKey(owner, key);
            values[(owner, key)] = value;
        }

        public void Clear()
        {
            values.Clear();
        }

        private static void ValidateKey(ScriptableObject owner, string key)
        {
            if (owner == null || string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("State requires a module owner and a non-blank key.");
            }
        }
    }
}
