using System;

namespace AnEmailService.Nini.Config
{
    /// <include file='ConfigEventArgs.xml' path='//Class[@name="ConfigEventArgs"]/docs/*' />
    public class ConfigKeyEventArgs : EventArgs
    {
        /// <include file='ConfigEventArgs.xml' path='//Constructor[@name="Constructor"]/docs/*' />
        public ConfigKeyEventArgs(string keyName, string keyValue)
        {
            KeyName = keyName;
            KeyValue = keyValue;
        }

        /// <include file='ConfigEventArgs.xml' path='//Property[@name="KeyName"]/docs/*' />
        public string KeyName { get; }

        /// <include file='ConfigEventArgs.xml' path='//Property[@name="KeyValue"]/docs/*' />
        public string KeyValue { get; }
    }
}