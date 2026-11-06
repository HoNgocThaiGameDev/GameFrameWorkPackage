using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NebulaSoft.Analytics
{
    /// <summary>A snapshot of one event. Values are normalized to string, long or double for Firebase.</summary>
    public sealed class AnalyticsEvent
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }

        public AnalyticsEvent(string name, IDictionary<string, object> parameters = null)
        {
            ValidateName(name);
            Name = name;
            if (parameters != null && parameters.Count > 25)
                throw new ArgumentException("An analytics event supports at most 25 parameters.", nameof(parameters));
            var snapshot = new Dictionary<string, object>(StringComparer.Ordinal);
            if (parameters != null)
                foreach (var pair in parameters)
                {
                    ValidateName(pair.Key);
                    if (pair.Value == null) continue;
                    object value = pair.Value;
                    if (value is bool flag) value = flag ? 1L : 0L;
                    else if (value is byte || value is short || value is int || value is long)
                        value = Convert.ToInt64(value);
                    else if (value is float || value is double)
                    {
                        double number = Convert.ToDouble(value);
                        if (double.IsNaN(number) || double.IsInfinity(number))
                            throw new ArgumentException("Analytics numbers must be finite.", nameof(parameters));
                        value = number;
                    }
                    else if (!(value is string))
                        throw new ArgumentException("Analytics values must be strings, integers, booleans or numbers.", nameof(parameters));
                    if (value is string text && text.Length > 100) value = text.Substring(0, 100);
                    snapshot.Add(pair.Key, value);
                }
            Parameters = new ReadOnlyDictionary<string, object>(snapshot);
        }

        public static void ValidateName(string name, int maxLength = 40)
        {
            if (string.IsNullOrEmpty(name) || name.Length > maxLength || !IsLetter(name[0]) ||
                name.StartsWith("firebase_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("google_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("ga_", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Analytics names must start with a letter and cannot use reserved prefixes.", nameof(name));
            foreach (char c in name)
                if (!IsLetter(c) && !(c >= '0' && c <= '9') && c != '_')
                    throw new ArgumentException("Analytics names support ASCII letters, numbers and underscores.", nameof(name));
        }

        private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
}
