namespace InsuranceClaimsAPI.Configuration
{
    public static class ConnectionStrings
    {
        public const string DefaultConnection = 
            "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=password123;Port=3306;CharSet=utf8mb4;";
        
        public const string DevelopmentConnection = 
            "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=password123;Port=3306;CharSet=utf8mb4;";
        
        public const string TestingConnection = 
            "Server=localhost;Database=InsuranceClaimsDB_Test;Uid=root;Pwd=password;Port=3306;CharSet=utf8mb4;";
    }
}
