using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NetEti.ApplicationEnvironment
{
    /// <summary>
    /// Stellt für mehrere AppEnvReader-Instanzen ein gemeinsames statisches ConcurrentDictionary
    /// zur Verfügung, über das eine gemeinsame Parameterliste verwaltet werden kann.
    /// </summary>
    public static class AppSettingsRegistry
    {
        private static ConcurrentDictionary<string, object> _registeredKeyValuePairs;
        private static ConcurrentDictionary<string, string> _parametersSources;
        private static object _lockingObject;

        /// <summary>
        /// Liste, die von außen mit Key-Value Paaren gefüllt werden kann.
        /// Diese Liste wird bei der Suche ebenfalls berücksichtigt.
        /// </summary>
        /// <param name="key">Der Key des zu registrierenden KeyValue-Paares.</param>
        /// <param name="value">Der Wert des zu registrierenden KeyValue-Paares.</param>
        public static void RegisterKeyValue(string key, object value)
        {
            if (AppSettingsRegistry._registeredKeyValuePairs.ContainsKey(key))
            {
                object val;
                AppSettingsRegistry._registeredKeyValuePairs.TryRemove(key, out val);
            }
            AppSettingsRegistry._registeredKeyValuePairs.TryAdd(key, value);
        }

        /// <summary>
        /// Liste, die von außen mit Key-Value Paaren gefüllt werden kann.
        /// Der gegebene Key wird aus der Liste entfernt.
        /// </summary>
        /// <param name="key">Der String-Key</param>
        public static void UnregisterKey(string key)
        {
            if (AppSettingsRegistry._registeredKeyValuePairs.ContainsKey(key))
            {
                object val;
                AppSettingsRegistry._registeredKeyValuePairs.TryRemove(key, out val);
            }
        }

        /// <summary>
        /// Liefert true, wenn der Key in der Aufzählung enthalten ist.
        /// </summary>
        /// <param name="key">Zu suchender Key.</param>
        /// <returns>True, wenn der Key in der Aufzählung enthalten ist.</returns>
        public static bool ContainsKey(string key)
        {
            return AppSettingsRegistry._registeredKeyValuePairs.ContainsKey(key);
        }

        /// <summary>
        /// Liefert aus der Aufzählung den Wert zum übergebenen Key.
        /// </summary>
        /// <param name="key">Key zum zu suchenden Wert.</param>
        /// <returns>Der Wert zum Key als object.</returns>
        public static object GetValue(string key)
        {
            object val;
            if (AppSettingsRegistry._registeredKeyValuePairs.TryGetValue(key, out val))
            {
                return val;
            }
            return null;
        }

        /// <summary>
        /// Merkt sich den Namen der Quelle des Werts eines übergebenen Parameters
        /// in einer Aufzählung für spätere Debug-Ausgaben aller Parameter und
        /// der letzten Quellen ihrer Werte.
        /// </summary>
        /// <param name="key">Der Key des Parameters.</param>
        /// <param name="parameterSource">Die Quelle des Parameters.</param>
        /// <param name="value">Der Wert des Parameters.</param>
        public static void RememberParameterSource(string key, string parameterSource, string value)
        {
            if (!(String.IsNullOrEmpty(key) || String.IsNullOrEmpty(parameterSource)))
            {
                string paraInfo = "Wert: " + value + ", Quelle: " + parameterSource;
                if (!AppSettingsRegistry._parametersSources.ContainsKey(key))
                {
                    AppSettingsRegistry._parametersSources.TryAdd(key, paraInfo);
                }
                else
                {
                    if (parameterSource == "registered")
                    {
                        string orgSource = AppSettingsRegistry._parametersSources[key].Split(new string[] { "Quelle: " }, StringSplitOptions.None)[1];
                        paraInfo = "Wert: " + value + ", Quelle: " + orgSource + " (registered)";
                    }
                    AppSettingsRegistry._parametersSources[key] = paraInfo;
                }
            }
        }

        /// <summary>
        /// Liefert ein Dictionary, das zu jedem Parameter den Namen der Quelle
        /// und den letzten Wert enthält.
        /// Kann in bestimmten Fällen für die Fehlersuche hilfreich sein.
        /// </summary>
        /// <returns>Dictionary, das zu jedem Parameter den Namen der Quelle und den letzten Wert enthält.</returns>
        public static SortedDictionary<string, string> GetParametersSources()
        {
            lock (AppSettingsRegistry._lockingObject)
            {
                return new SortedDictionary<string, string>(AppSettingsRegistry._parametersSources);
            }
        }

        static AppSettingsRegistry()
        {
            AppSettingsRegistry._registeredKeyValuePairs = new ConcurrentDictionary<string, object>();
            AppSettingsRegistry._parametersSources = new ConcurrentDictionary<string, string>();
            AppSettingsRegistry._lockingObject = new object();
        }
    }
}
