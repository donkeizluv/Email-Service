#region Copyright

//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 

#endregion

using System;
using System.Globalization;
using AnEmailService.Nini.Util;

namespace AnEmailService.Nini.Config
{

    #region ConfigKeyEventArgs class

    #endregion

    /// <include file='IConfig.xml' path='//Interface[@name="IConfig"]/docs/*' />
    public class ConfigBase : IConfig
    {
        #region Protected variables

        protected OrderedList keys = new OrderedList();

        #endregion

        #region Constructors

        /// <include file='ConfigBase.xml' path='//Constructor[@name="ConfigBase"]/docs/*' />
        public ConfigBase(string name, IConfigSource source)
        {
            configName = name;
            ConfigSource = source;
            Alias = new AliasText();
        }

        #endregion

        #region Private variables

        private string configName;
        private readonly IFormatProvider format = NumberFormatInfo.CurrentInfo;

        #endregion

        #region Public properties

        /// <include file='IConfig.xml' path='//Property[@name="Name"]/docs/*' />
        public string Name
        {
            get { return configName; }
            set
            {
                if (configName != value) Rename(value);
            }
        }

        /// <include file='IConfig.xml' path='//Property[@name="ConfigSource"]/docs/*' />
        public IConfigSource ConfigSource { get; }

        /// <include file='IConfig.xml' path='//Property[@name="Alias"]/docs/*' />
        public AliasText Alias { get; }

        #endregion

        #region Public methods

        /// <include file='IConfig.xml' path='//Method[@name="Contains"]/docs/*' />
        public bool Contains(string key)
        {
            return Get(key) != null;
        }

        /// <include file='IConfig.xml' path='//Method[@name="Get"]/docs/*' />
        public virtual string Get(string key)
        {
            string result = null;

            if (keys.Contains(key)) result = keys[key].ToString();

            return result;
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetDefault"]/docs/*' />
        public string Get(string key, string defaultValue)
        {
            string result = Get(key);

            return result == null ? defaultValue : result;
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetExpanded"]/docs/*' />
        public string GetExpanded(string key)
        {
            return ConfigSource.GetExpanded(this, key);
        }

        /// <include file='IConfig.xml' path='//Method[@name="Get"]/docs/*' />
        public string GetString(string key)
        {
            return Get(key);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetDefault"]/docs/*' />
        public string GetString(string key, string defaultValue)
        {
            return Get(key, defaultValue);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetInt"]/docs/*' />
        public int GetInt(string key)
        {
            string text = Get(key);

            if (text == null) throw new ArgumentException("Value not found: " + key);

            return Convert.ToInt32(text, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetIntAlias"]/docs/*' />
        public int GetInt(string key, bool fromAlias)
        {
            if (!fromAlias) return GetInt(key);

            string result = Get(key);

            if (result == null) throw new ArgumentException("Value not found: " + key);

            return GetIntAlias(key, result);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetIntDefault"]/docs/*' />
        public int GetInt(string key, int defaultValue)
        {
            string result = Get(key);

            return result == null
                ? defaultValue
                : Convert.ToInt32(result, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetIntDefaultAlias"]/docs/*' />
        public int GetInt(string key, int defaultValue, bool fromAlias)
        {
            if (!fromAlias) return GetInt(key, defaultValue);

            string result = Get(key);

            return result == null ? defaultValue : GetIntAlias(key, result);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetLong"]/docs/*' />
        public long GetLong(string key)
        {
            string text = Get(key);

            if (text == null) throw new ArgumentException("Value not found: " + key);

            return Convert.ToInt64(text, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetLongDefault"]/docs/*' />
        public long GetLong(string key, long defaultValue)
        {
            string result = Get(key);

            return result == null
                ? defaultValue
                : Convert.ToInt64(result, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetBoolean"]/docs/*' />
        public bool GetBoolean(string key)
        {
            string text = Get(key);

            if (text == null) throw new ArgumentException("Value not found: " + key);

            return GetBooleanAlias(text);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetBooleanDefault"]/docs/*' />
        public bool GetBoolean(string key, bool defaultValue)
        {
            string text = Get(key);

            return text == null ? defaultValue : GetBooleanAlias(text);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetFloat"]/docs/*' />
        public float GetFloat(string key)
        {
            string text = Get(key);

            if (text == null) throw new ArgumentException("Value not found: " + key);

            return Convert.ToSingle(text, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetFloatDefault"]/docs/*' />
        public float GetFloat(string key, float defaultValue)
        {
            string result = Get(key);

            return result == null
                ? defaultValue
                : Convert.ToSingle(result, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetDouble"]/docs/*' />
        public double GetDouble(string key)
        {
            string text = Get(key);

            if (text == null) throw new ArgumentException("Value not found: " + key);

            return Convert.ToDouble(text, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetDoubleDefault"]/docs/*' />
        public double GetDouble(string key, double defaultValue)
        {
            string result = Get(key);

            return result == null
                ? defaultValue
                : Convert.ToDouble(result, format);
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetKeys"]/docs/*' />
        public string[] GetKeys()
        {
            var result = new string[keys.Keys.Count];

            keys.Keys.CopyTo(result, 0);

            return result;
        }

        /// <include file='IConfig.xml' path='//Method[@name="GetValues"]/docs/*' />
        public string[] GetValues()
        {
            var result = new string[keys.Values.Count];

            keys.Values.CopyTo(result, 0);

            return result;
        }

        /// <include file='ConfigBase.xml' path='//Method[@name="Add"]/docs/*' />
        public void Add(string key, string value)
        {
            keys.Add(key, value);
        }

        /// <include file='IConfig.xml' path='//Method[@name="Set"]/docs/*' />
        public virtual void Set(string key, object value)
        {
            if (value == null) throw new ArgumentNullException("Value cannot be null");

            if (Get(key) == null) Add(key, value.ToString());
            else keys[key] = value.ToString();

            if (ConfigSource.AutoSave) ConfigSource.Save();

            OnKeySet(new ConfigKeyEventArgs(key, value.ToString()));
        }

        /// <include file='IConfig.xml' path='//Method[@name="Remove"]/docs/*' />
        public virtual void Remove(string key)
        {
            if (key == null) throw new ArgumentNullException("Key cannot be null");

            if (Get(key) != null)
            {
                string keyValue = null;
                if (KeySet != null) keyValue = Get(key);
                keys.Remove(key);

                OnKeyRemoved(new ConfigKeyEventArgs(key, keyValue));
            }
        }

        #endregion

        #region Public events

        /// <include file='IConfig.xml' path='//Event[@name="KeySet"]/docs/*' />
        public event ConfigKeyEventHandler KeySet;

        /// <include file='IConfig.xml' path='//Event[@name="KeyRemoved"]/docs/*' />
        public event ConfigKeyEventHandler KeyRemoved;

        #endregion

        #region Protected methods

        /// <include file='ConfigBase.xml' path='//Method[@name="OnKeySet"]/docs/*' />
        protected void OnKeySet(ConfigKeyEventArgs e)
        {
            if (KeySet != null) KeySet(this, e);
        }

        /// <include file='ConfigBase.xml' path='//Method[@name="OnKeyRemoved"]/docs/*' />
        protected void OnKeyRemoved(ConfigKeyEventArgs e)
        {
            if (KeyRemoved != null) KeyRemoved(this, e);
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Renames the config to the new name.
        /// </summary>
        private void Rename(string name)
        {
            ConfigSource.Configs.Remove(this);
            configName = name;
            ConfigSource.Configs.Add(this);
        }

        /// <summary>
        ///     Returns the integer alias first from this IConfig then
        ///     the parent if there is none.
        /// </summary>
        private int GetIntAlias(string key, string alias)
        {
            int result = -1;

            if (Alias.ContainsInt(key, alias)) result = Alias.GetInt(key, alias);
            else result = ConfigSource.Alias.GetInt(key, alias);

            return result;
        }

        /// <summary>
        ///     Returns the boolean alias first from this IConfig then
        ///     the parent if there is none.
        /// </summary>
        private bool GetBooleanAlias(string key)
        {
            bool result = false;

            if (Alias.ContainsBoolean(key))
            {
                result = Alias.GetBoolean(key);
            }
            else
            {
                if (ConfigSource.Alias.ContainsBoolean(key)) result = ConfigSource.Alias.GetBoolean(key);
                else
                    throw new ArgumentException
                    ("Alias value not found: " + key
                     + ". Add it to the Alias property.");
            }

            return result;
        }

        #endregion
    }
}