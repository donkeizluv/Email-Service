namespace AnEmailService.Nini.Ini
{
    /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/docs/*' />
    public enum IniReadState
    {
        /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/Value[@name="Closed"]/docs/*' />
        Closed,

        /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/Value[@name="EndOfFile"]/docs/*' />
        EndOfFile,

        /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/Value[@name="Error"]/docs/*' />
        Error,

        /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/Value[@name="Initial"]/docs/*' />
        Initial,

        /// <include file='IniReader.xml' path='//Enum[@name="IniReadState"]/Value[@name="Interactive"]/docs/*' />
        Interactive
    }
}