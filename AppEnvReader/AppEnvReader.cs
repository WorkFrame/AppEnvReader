using System.Text.RegularExpressions;
using NetEti.Globals;
using System.Text;

namespace NetEti.ApplicationEnvironment
{
    /// <summary>
    /// Verwaltet eine Liste von IGetStringValue-Objekten und fragt diese
    /// der Reihe nach ab um das erste gültige Ergebnis selbst wieder als IGetStringValue
    /// an den Aufrufer zurück zu geben.<br></br>Für Values, die Wildcards der Form '%Name%'
    /// enthalten, findet eine rekursive Ersetzung statt (nur für GetStringValue(...)).<br></br>
    /// Verwaltet eine zusätzliche Liste, die von außen mit Key-Value Paaren gefüllt
    /// werden kann; diese Liste wird bei der Suche ebenfalls berücksichtigt.
    /// </summary>
    /// <remarks>
    /// File: AppEnvReader<br></br>
    /// Autor: Erik Nagel, NetEti<br></br>
    ///<br></br>
    /// 08.03.2012 Erik Nagel: erstellt.<br></br>
    /// 08.03.2012 Erik Nagel: In Defaults werden jetzt Platzhalter der Form '%abc%' ebenfalls ersetzt.<br></br>
    /// 24.08.2012 Erik Nagel: GetValue T (string key, T defaultValue) eingebaut.<br></br>
    /// 21.04.2013 Erik Nagel: Verarbeitung über Array-Kopie in getStringValueReku.<br></br>
    /// 23.01.2014 Erik Nagel: Null-Referenz-Fehler in GetStringValue behoben.
    /// 31.03.2014 Erik Nagel: IsDefault implementiert.<br></br>
    /// 29.07.2018 Erik Nagel: GetParametersSources implementiert.<br></br>
    /// </remarks>
    public class AppEnvReader : IGetStringValue, IGetValue
    {

        #region IGetStringValue Member

        /// <summary>
        /// Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt.
        /// </summary>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValue">Das default-Ergebnis (string)</param>
        /// <returns>Der Ergebnis-String</returns>
        public string? GetStringValue(string key, string? defaultValue)
        {
            List<string> alreadySearched = new List<string>();
            string? parameterSource = null;
            string? rtn = null;
            rtn = getStringValueReku(alreadySearched, key, ref parameterSource);
            if (rtn == null)
            {
                rtn = defaultValue;
                if (!this._defaultProperties.Contains(key))
                {
                    this._defaultProperties.Add(key);
                }
                if (rtn != null)
                {
                    // 08.03.2012 Nagel+: In Defaults werden jetzt Platzhalter der Form '%abc%' ebenfalls ersetzt.
                    MatchCollection alleTreffer;
                    alleTreffer = _compiledPattern.Matches(rtn);
                    for (int i = 0; i < alleTreffer.Count; i++)
                    {
                        string subKey = alleTreffer[i].Groups[1].Value;
                        if (!alreadySearched.Contains(subKey))
                        {
                            alreadySearched.Add(subKey);
                            string? dummy = null;
                            string? subRtn = getStringValueReku(alreadySearched, subKey, ref dummy);
                            if (subRtn != null)
                            {
                                rtn = Regex.Replace(rtn, @"%" + subKey + @"%", subRtn, RegexOptions.IgnoreCase);
                            }
                            alreadySearched.Remove(subKey);
                        }
                    }
                    // 08.03.2012 Nagel-
                }
                this.RememberParameterSource(key, "DEFAULT", rtn ?? "null");
            }
            else
            {
                this.RememberParameterSource(key, parameterSource, rtn ?? "null");
            }
            return rtn;
        }

        /// <summary>
        /// Liefert ein string-Array zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// </summary>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValues">Das default-Ergebnis (string[])</param>
        /// <returns>Das Ergebnis-String-Array</returns>
        public string?[]? GetStringValues(string key, string?[]? defaultValues)
        {
            string?[]? rtn = null;
            IGetStringValue[] getters;
            string? parameterSource = null;
            lock (AppEnvReader._lockMe)
            {
                // Zur weiteren Verarbeitung threadsafe in ein entkoppeltes Array kopieren.
                // Alternative wäre, die gesamte Routine zu sperren. Das würde aber möglicherweise die
                // gesamte Verarbeitung ausbremsen, weshalb ich hier lieber ein im Extremfall verloren
                // gegangenes Value in Kauf nehme.
                getters = new IGetStringValue[this._stringValueGetters.Count];
                this._stringValueGetters.CopyTo(getters, 0);
            }
            foreach (IGetStringValue stringValueGetter in getters)
            {
                rtn = stringValueGetter.GetStringValues(key, null);
                if ((rtn != null) && (rtn.Length > 0))
                {
                    parameterSource = stringValueGetter.Description;
                    break;
                }
            }
            if (rtn != null)
            {
                for (int i = 0; i < rtn.Length; i++)
                {
                    List<string> alreadySearched = new List<string>();
                    string? actKey = rtn[i];
                    if (!String.IsNullOrEmpty(actKey))
                    {
                        MatchCollection alleTreffer;
                        alleTreffer = _compiledPattern.Matches(actKey);
                        for (int j = 0; j < alleTreffer.Count; j++)
                        {
                            string subKey = alleTreffer[j].Groups[1].Value;
                            if (!alreadySearched.Contains(subKey))
                            {
                                alreadySearched.Add(subKey);
                                string? dummy = null;
                                string? subRtn = getStringValueReku(alreadySearched, subKey, ref dummy);
                                if (subRtn != null)
                                {
                                    actKey = Regex.Replace(actKey, @"%" + subKey + @"%", subRtn, RegexOptions.IgnoreCase);
                                }
                                alreadySearched.Remove(subKey);
                            }
                        }
                    }
                    rtn[i] = actKey;
                }
                this.RememberParameterSource(key, parameterSource, String.Join(",", rtn ?? new string[] { "null" }));
            }
            else
            {
                rtn = defaultValues;
                this.RememberParameterSource(key, "DEFAULT", String.Join(",", rtn ?? new string[] { "null" }));
            }
            return rtn;
        }

        /// <summary>
        /// Liefert einen beschreibenden Namen dieses StringValueGetters,
        /// z.B. Name plus ggf. Quellpfad.
        /// </summary>
        public string Description { get; set; }

        #endregion

        #region public members

        /// <summary>
        /// Nur zu Debug-Zwecken - kann später gelöscht werden.
        /// </summary>
        internal Guid DebugGuid { get; set; }

        /// <summary>
        /// Liefert ein Dictionary, das zu jedem Parameter den Namen der Quelle enthält.
        /// Kann in bestimmten Fällen für die Fehlersuche hilfreich sein.
        /// </summary>
        /// <returns>Dictionary, das zu jedem Parameter den Namen der Quelle enthält.</returns>
        public SortedDictionary<string, string> GetParametersSources()
        {
            return AppSettingsRegistry.GetParametersSources();
        }

        /// <summary>
        /// Gibt true zurück, wenn die übergebene Eigenschaft nicht über
        /// externe Quellen, sondern durch den Default-Wert gefüllt wurde.
        /// </summary>
        /// <param name="key">Der Name der Property.</param>
        /// <returns>True, wenn die übergebene Eigenschaft durch den Default-Wert gefüllt wurde.</returns>
        public bool IsDefault(string key)
        {
            return this._defaultProperties.Contains(key);
        }

        /// <summary>
        /// Fügt stringValueGetter an das Ende der Liste an.
        /// </summary>
        /// <param name="stringValueGetter">Die anzufügende IGetStringValue Instanz</param>
        public void RegisterStringValueGetter(IGetStringValue stringValueGetter)
        {
            lock (AppEnvReader._lockMe)
            {
                this.UnregisterStringValueGetter(stringValueGetter);
                this._stringValueGetters.Add(stringValueGetter);
                this.RefreshDescription();
            }
        }

        /// <summary>
        /// Fügt stringValueGetter vor dem anchor in die Liste ein.
        /// </summary>
        /// <param name="stringValueGetter">Die einzufügende IGetStringValue Instanz</param>
        /// <param name="anchor">Die IGetStringValue Instanz vor der eingefügt wird</param>
        public void RegisterStringValueGetterBefore(IGetStringValue stringValueGetter, IGetStringValue anchor)
        {
            lock (AppEnvReader._lockMe)
            {
                this.UnregisterStringValueGetter(stringValueGetter);
                if (this._stringValueGetters.Contains(anchor))
                {
                    this._stringValueGetters.Insert(this._stringValueGetters.IndexOf(anchor), stringValueGetter);
                }
                else
                {
                    this._stringValueGetters.Insert(0, stringValueGetter);
                }
                this.RefreshDescription();
            }
        }

        /// <summary>
        /// Fügt stringValueGetter am übergebenen index in die Liste ein.
        /// </summary>
        /// <param name="stringValueGetter">Die einzufügende IGetStringValue Instanz.</param>
        /// <param name="index">Der Index (0-basiert), an dem eingefügt wird.</param>
        public void RegisterStringValueGetterAt(IGetStringValue stringValueGetter, int index)
        {
            lock (AppEnvReader._lockMe)
            {
                this.UnregisterStringValueGetter(stringValueGetter);
                if (this._stringValueGetters.Count > index)
                {
                    this._stringValueGetters.Insert(index, stringValueGetter);
                }
                else
                {
                    this._stringValueGetters.Add(stringValueGetter);
                }
                this.RefreshDescription();
            }
        }

        /// <summary>
        /// Löscht stringValueGetter aus der Liste.
        /// </summary>
        /// <param name="stringValueGetter">Die aus der Liste zu löschende IGetStringValue Instanz</param>
        public void UnregisterStringValueGetter(IGetStringValue stringValueGetter)
        {
            lock (AppEnvReader._lockMe)
            {
                if (this._stringValueGetters.Contains(stringValueGetter))
                {
                    this._stringValueGetters.Remove(stringValueGetter);
                }
                this.RefreshDescription();
            }
        }

        /// <summary>
        /// Liste, die von außen mit Key-Value Paaren gefüllt werden kann.
        /// Diese Liste wird bei der Suche ebenfalls berücksichtigt.
        /// </summary>
        /// <param name="key">Der Key des zu registrierenden KeyValue-Paares.</param>
        /// <param name="value">Der Wert des zu registrierenden KeyValue-Paares.</param>
        public void RegisterKeyValue(string key, object? value)
        {
            AppSettingsRegistry.RegisterKeyValue(key, value);
            this.RememberParameterSource(key, "registered", value == null ? "null": value.ToString()?? "");
        }

        /// <summary>
        /// Liste, die von außen mit Key-Value Paaren gefüllt werden kann.
        /// Der gegebene Key wird aus der Liste entfernt.
        /// </summary>
        /// <param name="key">Der String-Key</param>
        public void UnregisterKey(string key)
        {
            AppSettingsRegistry.UnregisterKey(key);
        }

        /// <summary>
        /// Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt;
        /// Es wird versucht, den ermittelten String-Wert in den Rückgabetyp T zu casten.
        /// </summary>
        /// <typeparam name="T">Der gewünschte Rückgabe-Typ</typeparam>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValue">Das default-Ergebnis vom Typ T</param>
        /// <returns>Wert zum key in den Rückgabe-Typ gecastet</returns>
        /// <exception cref="InvalidCastException">Typecast-Fehler</exception>
        public T? GetValue<T>(string key, T? defaultValue)
        {
            string? stringValue = GetStringValue(key, defaultValue?.ToString());
            if (stringValue == null || stringValue.Equals(defaultValue?.ToString()))
            {
                return defaultValue;
            }
            try
            {
                switch (typeof(T).ToString())
                {
                    case "System.Boolean": return (T)(Object)Convert.ToBoolean(stringValue);
                    case "System.Int16": return (T)(Object)Convert.ToInt16(stringValue);
                    case "System.Int32": return (T)(Object)Convert.ToInt32(stringValue);
                    case "System.Int64": return (T)(Object)Convert.ToInt64(stringValue);
                    case "System.Decimal": return (T)(Object)Convert.ToDecimal(stringValue);
                    case "System.Double": return (T)(Object)Convert.ToDouble(stringValue);
                    case "System.DateTime": return (T)(Object)Convert.ToDateTime(stringValue);
                    default: return (T)(Object)stringValue;
                }
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException(String.Format("Konvertierungsfehler bei Schlüssel '{0}' mit Wert '{1}' in DatenTyp '{2}'!", key, stringValue, typeof(T).FullName));
            }
        }

        /// <summary>
        /// NICHT IMPLEMENTIERT!
        /// Liefert ein Array von Werten zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt;
        /// Es wird versucht, den ermittelten String-Wert in den Rückgabetyp T zu casten.
        /// </summary>
        /// <typeparam name="T">Der gewünschte Rückgabe-Typ</typeparam>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValues">Das default-Ergebnis vom Typ T[]</param>
        /// <returns>Wert-Array zum key in den Rückgabe-Typ gecastet</returns>
        /// <exception cref="InvalidCastException">Typecast-Fehler</exception>
        public T?[]? GetValues<T>(string key, T?[]? defaultValues)
        {
            throw new NotImplementedException();
        }

        #endregion public members

        #region private members

        private static object _lockMe = new object(); // nur für Threadlocks
        private List<IGetStringValue> _stringValueGetters;
        private string _textPattern;
        private Regex _compiledPattern;
        private List<string> _defaultProperties;

        /// <summary>
        /// Parameterloser Konstruktor, initialisiert die Listen.
        /// </summary>
        public AppEnvReader()
        {
            this.DebugGuid = Guid.NewGuid();
            this.Description = "AppEnvReader";
            this._stringValueGetters = new List<IGetStringValue>();
            this._textPattern = @"\%([A-Za-z0-9_\.-]+)\%";
            this._compiledPattern = new Regex(_textPattern);
            this._defaultProperties = new List<string>();
        }

        private void RefreshDescription()
        {
            if (this._stringValueGetters.Count > 0)
            {
                StringBuilder getters = new StringBuilder("");
                string delimiter = "";
                delimiter = Environment.NewLine;
                for (int i = 0; i < this._stringValueGetters.Count; i++)
                {
                    getters.Append(this._stringValueGetters[i].Description);
                }
                this.Description = "AppEnvReader: " + getters.ToString();
            }
            else
            {
                this.Description = "AppEnvReader";
            }
        }

        // Wird von GetStringValue aufgerufen.
        // Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
        // Key gibt, wird defaultValue zurückgegeben.
        // Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt.
        private string? getStringValueReku(List<string> alreadySearched, string key, ref string? parameterSource)
        {
            string? rtn = null;
            string? lastGetterDescription = null;
            if (AppSettingsRegistry.ContainsKey(key))
            {
                rtn = AppSettingsRegistry.GetValue(key)?.ToString();
            }
            else
            {
                IGetStringValue[] getters;
                lock (AppEnvReader._lockMe)
                {
                    // zur weiteren Verarbeitung threadsafe in ein entkoppeltes Array kopieren.
                    // In einer Multithreading-Umgebung können den _stringValueGetters Reader hinzugefügt
                    // werden, während diese Routine gerade läuft, was bei der direkten Verwendung von
                    // this._stringValueGetters zu der Exception führen würde, dass die Auflistung
                    // während der Verarbeitung geändert wurde.
                    // Alternative wäre, die gesamte Routine zu sperren. Das würde aber möglicherweise die
                    // gesamte Verarbeitung ausbremsen, weshalb ich hier lieber ein im Extremfall verloren
                    // gegangenes Value in Kauf nehme.
                    getters = new IGetStringValue[this._stringValueGetters.Count];
                    this._stringValueGetters.CopyTo(getters, 0);
                }
                foreach (IGetStringValue stringValueGetter in getters)
                {
                    if ((rtn = stringValueGetter.GetStringValue(key, null)) != null)
                    {
                        lastGetterDescription = stringValueGetter.Description;
                        break;
                    }
                }
            }
            if (rtn != null)
            {
                MatchCollection alleTreffer;
                alleTreffer = _compiledPattern.Matches(rtn);
                for (int i = 0; i < alleTreffer.Count; i++)
                {
                    string subKey = alleTreffer[i].Groups[1].Value;
                    if (!alreadySearched.Contains(subKey))
                    {
                        alreadySearched.Add(subKey);
                        string? dummy = null;
                        string? subRtn = getStringValueReku(alreadySearched, subKey, ref dummy);
                        if (subRtn != null)
                        {
                            rtn = Regex.Replace(rtn, @"%" + subKey + @"%", subRtn, RegexOptions.IgnoreCase);
                        }
                        alreadySearched.Remove(subKey);
                    }
                }
                parameterSource = lastGetterDescription;
            }
            return rtn;
        }

        private void RememberParameterSource(string key, string? parameterSource, string value)
        {
            AppSettingsRegistry.RememberParameterSource(key, parameterSource ?? "", value);
        }

        #endregion private members

    }
}
