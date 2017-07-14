#region Copyright

//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of
// which has been included with this distribution in the LICENSE.txt file.
//

#endregion Copyright

using AnEmailService.Nini.Util;
using System;

namespace AnEmailService.Nini.Config
{
    /// <include file='ArgvConfigSource.xml' path='//Class[@name="ArgvConfigSource"]/docs/*' />
    public class ArgvConfigSource : ConfigSourceBase
    {
        #region Constructors

        /// <include file='ArgvConfigSource.xml' path='//Constructor[@name="Constructor"]/docs/*' />
        public ArgvConfigSource(string[] arguments)
        {
            parser = new ArgvParser(arguments);
            this.arguments = arguments;
        }

        #endregion Constructors

        #region Private methods

        /// <summary>
        ///     Returns an IConfig.  If it does not exist then it is added.
        /// </summary>
        private IConfig GetConfig(string name)
        {
            IConfig result = null;

            if (Configs[name] == null)
            {
                result = new ConfigBase(name, this);
                Configs.Add(result);
            }
            else
            {
                result = Configs[name];
            }

            return result;
        }

        #endregion Private methods

        #region Private variables

        private readonly ArgvParser parser;
        private readonly string[] arguments;

        #endregion Private variables



        #region Public methods

        /// <include file='ArgvConfigSource.xml' path='//Method[@name="Save"]/docs/*' />
        public override void Save()
        {
            throw new ArgumentException("Source is read only");
        }

        /// <include file='ArgvConfigSource.xml' path='//Method[@name="Reload"]/docs/*' />
        public override void Reload()
        {
            throw new ArgumentException("Source cannot be reloaded");
        }

        /// <include file='ArgvConfigSource.xml' path='//Method[@name="AddSwitch"]/docs/*' />
        public void AddSwitch(string configName, string longName)
        {
            AddSwitch(configName, longName, null);
        }

        /// <include file='ArgvConfigSource.xml' path='//Method[@name="AddSwitchShort"]/docs/*' />
        public void AddSwitch(string configName, string longName,
            string shortName)
        {
            var config = GetConfig(configName);

            if (shortName != null &&
                (shortName.Length < 1 || shortName.Length > 2))
                throw new ArgumentException("Short name may only be 1 or 2 characters");

            // Look for the long name first
            if (parser[longName] != null) config.Set(longName, parser[longName]);
            else if (shortName != null && parser[shortName] != null) config.Set(longName, parser[shortName]);
        }

        /// <include file='ArgvConfigSource.xml' path='//Method[@name="GetArguments"]/docs/*' />
        public string[] GetArguments()
        {
            var result = new string[arguments.Length];
            Array.Copy(arguments, 0, result, 0, arguments.Length);

            return result;
        }

        #endregion Public methods
    }
}