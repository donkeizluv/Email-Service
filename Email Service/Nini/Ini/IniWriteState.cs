namespace AnEmailService.Nini.Ini
{
    /// <include file='IniWriter.xml' path='//Enum[@name="IniWriteState"]/docs/*' />
    public enum IniWriteState
    {
        /// <include file='IniWriter.xml' path='//Enum[@name="IniWriteState"]/Value[@name="Start"]/docs/*' />
        Start,

        /// <include file='IniWriter.xml' path='//Enum[@name="IniWriteState"]/Value[@name="BeforeFirstSection"]/docs/*' />
        BeforeFirstSection,

        /// <include file='IniWriter.xml' path='//Enum[@name="IniWriteState"]/Value[@name="Section"]/docs/*' />
        Section,

        /// <include file='IniWriter.xml' path='//Enum[@name="IniWriteState"]/Value[@name="Closed"]/docs/*' />
        Closed
    }
}