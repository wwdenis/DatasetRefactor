﻿#+Dataset#namespace #Namespace##+#
{
    internal static class GlobalSettings
    {
        static GlobalSettings()
        {
            ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TO_BE_CHANGED;Integrated Security=True";
        }

        public static string ConnectionString { get; set; }
    }
}