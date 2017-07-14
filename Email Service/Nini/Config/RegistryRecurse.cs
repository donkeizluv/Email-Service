namespace AnEmailService.Nini.Config
{
    /// <include file='RegistryConfigSource.xml' path='//Enum[@name="RegistryRecurse"]/docs/*' />
    public enum RegistryRecurse
    {
        /// <include file='RegistryConfigSource.xml' path='//Enum[@name="RegistryRecurse"]/Value[@name="None"]/docs/*' />
        None,

        /// <include file='RegistryConfigSource.xml' path='//Enum[@name="RegistryRecurse"]/Value[@name="Flattened"]/docs/*' />
        Flattened,

        /// <include file='RegistryConfigSource.xml' path='//Enum[@name="RegistryRecurse"]/Value[@name="Namespacing"]/docs/*' />
        Namespacing
    }
}