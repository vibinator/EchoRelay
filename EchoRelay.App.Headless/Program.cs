using EchoRelay.App.Settings;
using EchoRelay.Core.Server;
using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Messages;
using EchoRelay.Core.Server.Services;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Filesystem;
using System.Net;
namespace EchoRelay
{

    public class Headless
    {

        bool ReadBoolInput(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToLower();
            if (input == "yes" || input == "y" || input == "true")
            {
                return true;
            }
            else if (input == "no" || input == "n" || input == "false")
            {
                return false;
            }
            Console.WriteLine("Invalid input. Please enter 'Y' or 'N'.");
            return ReadBoolInput(prompt);
        }

        ushort ReadUShortInput(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToLower();
            ushort result;
            if (ushort.TryParse(input, out result))
            {
                return result;
            }
            Console.WriteLine("Invalid input. Please enter a valid port.");
            return ReadUShortInput(prompt);
        }

        string ReadDirectoryInput(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim();

            if (Directory.Exists(input))
            {
                return input;
            }
            try
            {
                Path.GetFullPath(input);
                return input;
            }
            catch
            {
                Console.WriteLine("Invalid input. Please enter a valid directory.");
                return ReadDirectoryInput(prompt);
            }
        }

        /// <summary>
        /// The file path which the <see cref="AppSettings"/> are loaded from.
        /// </summary>
        public string SettingsFilePath { get; }
        /// <summary>
        /// The application settings loaded from the <see cref="SettingsFilePath"/>.
        /// </summary>
        public AppSettings Settings { get; }

        /// <summary>
        /// The websocket server used to power central game services.
        /// </summary>
        public Server Server { get; set; }

        public int ServerCount { get; set; }

        public Headless()
        {
            // Set our settings file path to be within the current directory.
            SettingsFilePath = Path.Join(Environment.CurrentDirectory, "settings.json");

            // Try to load our application settings.
            AppSettings? settings = AppSettings.Load(SettingsFilePath);

            // Validate the settings.
            if (settings == null || !settings.Validate())
            {
                // Show our initial message describing what is about to happen.
                Console.WriteLine("Application settings have not been correctly configured. Please configure them now.", "Echo Relay: Settings");

                // If the settings weren't initialized, do so with some default values.
                settings ??= new AppSettings(port: 777);


                // Collect settings
                settings.Port = ReadUShortInput("Listen port: ");
                settings.FilesystemDatabaseDirectory = ReadDirectoryInput("Database directory path: ");
                settings.GameExecutableFilePath = ReadDirectoryInput("Game executeable file path: ");
                settings.StartServerOnStartup = true;

                if (ReadBoolInput("Use ServerDB API key? (y/n): "))
                {
                    Console.Write("Specify key: ");
                    settings.ServerDBApiKey = Console.ReadLine();
                }

                settings.MatchingPopulationOverPing = ReadBoolInput("Prefer population over ping when matching? (y/n): ");
                settings.MatchingForceIntoAnySessionOnFailure = ReadBoolInput("Force player into any session on match failure? (y/n): ");

                // Save the settings.
                settings.Save(SettingsFilePath);
                Console.WriteLine();
                Console.WriteLine(String.Format("Settings saved to '{0}'.", SettingsFilePath));
                Console.WriteLine();
            }

            // Set our loaded/created settings
            Settings = settings;

            // Create our file system storage and open it.
            ServerStorage serverStorage = new FilesystemServerStorage(Settings.FilesystemDatabaseDirectory!);
            serverStorage.Open();

            // Perform initial deployment
            bool allCriticalResourcesExist = serverStorage.AccessControlList.Exists() && serverStorage.ChannelInfo.Exists() && serverStorage.LoginSettings.Exists() && serverStorage.SymbolCache.Exists();
            bool anyCriticalResourcesExist = serverStorage.AccessControlList.Exists() || serverStorage.ChannelInfo.Exists() || serverStorage.LoginSettings.Exists() || serverStorage.SymbolCache.Exists();
            bool performInitialSetup = !allCriticalResourcesExist;
            if (performInitialSetup && anyCriticalResourcesExist)
            {
                Console.WriteLine("Critical resources are missing from storage, but storage is non-empty.\nWould you like to re-deploy initial setup resources? Warning: this will clear all storage except accounts!");
                performInitialSetup = ReadBoolInput("(y/n): ");
            }
            if (performInitialSetup)
                InitialDeployment.PerformInitialDeployment(serverStorage, null, false);

            // Create a server instance and set up our event handlers
            Server = new Server(serverStorage,
                new ServerSettings(
                    port: Settings.Port,
                    serverDbApiKey: Settings.ServerDBApiKey,
                    favorPopulationOverPing: Settings.MatchingPopulationOverPing,
                    forceIntoAnySessionIfCreationFails: Settings.MatchingForceIntoAnySessionOnFailure
                    )
                );
            /*
            Server.OnServerStarted += Server_OnServerStarted;
            Server.OnServerStopped += Server_OnServerStopped;
            Server.OnAuthorizationResult += Server_OnAuthorizationResult;
            Server.OnServicePeerConnected += Server_OnServicePeerConnected;
            Server.OnServicePeerDisconnected += Server_OnServicePeerDisconnected;
            Server.OnServicePeerAuthenticated += Server_OnServicePeerAuthenticated;
            Server.OnServicePacketSent += Server_OnServicePacketSent;
            Server.OnServicePacketReceived += Server_OnServicePacketReceived;            
            */
            Server.ServerDBService.Registry.OnGameServerRegistered += Registry_OnGameServerRegistered;
            Server.ServerDBService.Registry.OnGameServerUnregistered += Registry_OnGameServerUnregistered;
            ServerCount = 0;


            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Console.WriteLine("Shutting Down");
                Server.Stop();
            };

            if (Settings.StartServerOnStartup)
            {
                Console.WriteLine("Starting Services");
                _ = Server.Start();
            }
        }
        // Count servers registered
        private void Registry_OnGameServerRegistered(EchoRelay.Core.Server.Services.ServerDB.RegisteredGameServer gameServer)
        {
            ServerCount++;
            Console.Title = $"EchoRelay.Headless    Servers[{ServerCount}]";
        }

        private void Registry_OnGameServerUnregistered(EchoRelay.Core.Server.Services.ServerDB.RegisteredGameServer gameServer)
        {
            ServerCount--;
            Console.Title = $"EchoRelay.Headless    Servers[{ServerCount}]";
        }

    }

    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Start services
            new Headless();

            // Keep console window from closing ( will add commands like settings, or startserver (count) etc...)
            while (true)
            {
                if (Console.ReadLine()?.Trim().ToLower() == "exit")
                {
                    break;
                }
            }

        }
    }
}