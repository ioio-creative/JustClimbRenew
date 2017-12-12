namespace JustClimbTrial.DataAccess
{
    public abstract class DataAccessBase
    {
        protected static JustClimbAppDataContext database = 
            JustClimbDataContextProvider.Database;
    }
}
