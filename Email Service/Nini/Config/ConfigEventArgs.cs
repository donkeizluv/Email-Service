using System;

namespace AnEmailService.Nini.Config
{
    /// <include file='ConfigEventArgs.xml' path='//Class[@name="ConfigEventArgs"]/docs/*' />
    public class ConfigEventArgs : EventArgs
    {
        /// <include file='ConfigEventArgs.xml' path='//Constructor[@name="ConstructorIConfig"]/docs/*' />
        public ConfigEventArgs(IConfig config)
        {
            Config = config;
        }

        /// <include file='ConfigEventArgs.xml' path='//Property[@name="Config"]/docs/*' />
        public IConfig Config { get; }
    }
}