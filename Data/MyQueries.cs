namespace Swilago.Data
{
    public class MyQueries
    {
        public static string PublicApi => @"EXEC dbo.P_PublicApi @UserEmail";

        public static string AllStatistics => @"EXEC dbo.P_AllStatistics";

        public static string UserStatistics => @"EXEC dbo.P_UserStatistics @UserEmail @StartDate @EndDate";
    }
}
