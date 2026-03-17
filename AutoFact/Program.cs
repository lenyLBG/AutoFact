namespace autofact
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // Force a valid connection string
            string connStr = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
            var bdd = new Bdd(connStr);
            var services = new AppServices(bdd);
            int? userId = null;
            string? userEmail = null;

            using (var auth = new FormAuth(bdd))
            {
                var res = auth.ShowDialog();
                if (res != System.Windows.Forms.DialogResult.OK)
                {
                    // user cancelled or failed to authenticate
                    return;
                }

                userId = auth.AuthenticatedUserId;
                userEmail = auth.AuthenticatedEmail;
            }

            Application.Run(new Form1(bdd, userId, userEmail, services));
        }
    }
}