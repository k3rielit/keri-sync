using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace keriWARE_Ultimate_Professional_Plus {
    class Program {

        // ---- //
        // Data //
        // ---- //
        static bool shouldSync = true;
        static string path = Environment.CurrentDirectory;
        static string roamingPath = GetAppDataRoamingPath();
        static string localPath = GetAppDataLocalPath();
        static string docPath = GetDocumentsPath();
        static string[] drivelist = ListDrives();
        static Dictionary<string,string> syncPaths;

        // ----- //
        // Logic //
        // ----- //
        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();
        private async Task MainAsync(string[] args) {
            Console.Title = "Fényképek";
            Log("[Program.MainAsync] Trying to load config...");
            syncPaths = LoadConfig();
            Log("[Program.MainAsync] Doing a startup sync...");
            SyncSaves("Program.SyncSaves");
            Log("[Program.MainAsync] Starting Program.SyncSaves Timer with 5 seconds interval.");
            Timer syncSavesTimer = new Timer(SyncSaves, "Program.SyncSaves", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            await Task.Delay(-1);
        }
        private async void SyncSaves(Object state) {
            if(shouldSync) {
                shouldSync = false;
                int syncedFiles = 0;
                int backedUpFiles = 0;
                await Task.Run(() => {
                    foreach (KeyValuePair<string,string> game in syncPaths) {
                        if(game.Key.Length>0 && game.Value.Length>0) {

                            CreateDir(game.Value);
                            CreateBackupDirectory(game.Key);

                            string[] gameDirs = Directory.GetDirectories(game.Value,"*",SearchOption.AllDirectories);
                            string[] gameFiles = Directory.GetFiles(game.Value,"*",SearchOption.AllDirectories);

                            string[] backupDirs = Directory.GetDirectories(GetBackupDirectory(game.Key),"*",SearchOption.AllDirectories);
                            string[] backupFiles = Directory.GetFiles(GetBackupDirectory(game.Key),"*",SearchOption.AllDirectories);

                            foreach (string dir in backupDirs) CreateDir(ConvertToGamePath(dir, game.Key));  // creating all the directories so File.Copy will work
                            foreach (string dir in gameDirs) CreateDir(ConvertToBackupPath(dir, game.Key));

                            if (backupFiles.Length > 0 && gameFiles.Length == 0) {
                                foreach (string bfile in backupFiles) {
                                    File.Copy(bfile, ConvertToGamePath(bfile, game.Key));    // copy from backup to game if there are no game files
                                    syncedFiles++;
                                }
                            }

                            else if (gameFiles.Length > 0 && backupFiles.Length == 0) {
                                foreach (string gfile in gameFiles) {
                                    File.Copy(gfile, ConvertToBackupPath(gfile, game.Key));  // copy from game to backup if there are no backup files
                                    backedUpFiles++;
                                }
                            }

                            else {
                                // copying over missing, or newer files
                                // check from backup to game
                                foreach (string bfile in backupFiles) {
                                    string gfile = ConvertToGamePath(bfile,game.Key);
                                    if(!File.Exists(gfile) || CompareFileTimes(bfile,gfile)>0) {
                                        File.Delete(gfile);
                                        File.Copy(bfile,ConvertToGamePath(bfile, game.Key));
                                        syncedFiles++;
                                    }
                                }
                                // check from game to backup
                                foreach (string gfile in gameFiles) {
                                    string bfile = ConvertToBackupPath(gfile, game.Key);
                                    if (!File.Exists(bfile) || CompareFileTimes(gfile,bfile)>0) {
                                        File.Delete(bfile);
                                        File.Copy(gfile, ConvertToBackupPath(gfile, game.Key));
                                        backedUpFiles++;
                                    }
                                }
                            }

                        }
                    }
                });
                Log($"Sync: {syncedFiles} Backup: {backedUpFiles}", state);
                syncedFiles = 0;
                backedUpFiles = 0;
                shouldSync = true;
            }
        }

        // --------- //
        // Shortcuts //
        // --------- //
        private static void Log(string log) => Console.WriteLine($"[{GetTime()}] {log}");
        private static void Log(string log, Object state) => Console.WriteLine($"[{GetTime()}] [{state}] {log}");
        private static void CreateDir(string path) {
            if(!Directory.Exists(path) && path.Length>0) Directory.CreateDirectory(path);
        }
        private static string GetTime() =>  $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.Millisecond,3}ms";
        private static string[] ListDrives() => DriveInfo.GetDrives().Select(s => s.RootDirectory.ToString()).ToArray();
        private static string GetAppDataRoamingPath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string GetAppDataLocalPath() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string GetDocumentsPath() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // -------- //
        // IO Utils //
        // -------- //
        private static int CompareFileTimes(string file1, string file2) => DateTime.Compare(File.GetLastWriteTime(file1), File.GetLastWriteTime(file2));
        private static void CreateBackupDirectory(string game) {
            string dir = GetBackupDirectory(game);
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        private static string GetBackupDirectory(string game) => $@"{path}\saves\{game}";
        private static string ConvertToBackupPath(string gpath, string game) => $@"{GetBackupDirectory(game)}\{gpath.Replace(syncPaths[game]+"\\","")}";
        private static string ConvertToGamePath(string bfile, string game) => $@"{syncPaths[game]}\{bfile.Replace($@"{path}\saves\{game}","")}";

        // ------ //
        // Config //
        // ------ //
        private static Dictionary<string,string> LoadConfig() {
            Dictionary<string,string> conf = new Dictionary<string, string>();
            if(File.Exists("config.txt") && new FileInfo("config.txt").Length>=3) {
                string line;
                using StreamReader sr = new StreamReader("config.txt"); while ((line = sr.ReadLine()) != null) {
                    if(!line.StartsWith("//") && line.Contains("=") && line.Length >= 3 && !conf.ContainsKey(line.Split("=")[0].Trim()) && (line.Count(c => c=='%')==2 || !line.Any(c => c == '%'))) {
                        string[] sline = line.Split('=');
                        sline[1] = sline[1].Trim().Replace("%documents%",docPath).Replace("%appdata%",roamingPath).Replace("%localappdata%",localPath);
                        conf.TryAdd(sline[0].Trim(),sline[1]);
                    }
                }
            }
            else {
                conf = new Dictionary<string, string>() {
                    ["nfsug2"] = $@"{localPath}\NFS Underground 2",
                    ["nfsug"] = @"C:\ProgramData\NFS Underground",
                    ["nfsc"] = $@"{docPath}\NFS Carbon",
                    ["nfsmw"] = $@"{docPath}\NFS Most Wanted",
                    ["gta3"] = $@"{docPath}\GTA3 USER FILES",
                    ["gtasa"] = $@"{docPath}\GTA San Andreas User Files",
                    ["gtavc"] = $@"{docPath}\GTA Vice City User Files",
                    ["halo"] = $@"{docPath}\My Games\Halo",
                    ["halo2"] = $@"{localPath}\Microsoft\Halo",
                    ["skyrim"] = $@"{docPath}\My Games\Skyrim",
                };
                using StreamWriter wr = new StreamWriter("config.txt");
                wr.WriteLine("// Configuration\r\n\r\n// These paths will be synchronized.\r\n// <name> = <path> \r\n// %localappdata% : Inserts the current AppData\\Local path\r\n// %documents% : Inserts the current Documents path\r\n// %appdata% : Inserts the current AppData\\Roaming path\r\n// Lines that are empty, or starting with \"//\" will be ignored when parsing this file.\r\n");
                foreach(KeyValuePair<string,string> pair in conf) {
                    string value = pair.Value;
                    if(value.Contains(@"\AppData\Local\")) value = "%localappdata%"+value.Split(@"\AppData\Local")[1];
                    else if(value.Contains(@"\Documents\")) value = "%documents%"+value.Split(@"\Documents")[1];
                    else if(value.Contains(@"\AppData\Roaming\")) value = "%appdata%"+value.Split(@"\AppData\Roaming")[1];
                    wr.WriteLine($@"{pair.Key} = {value}");
                }
            }
            return conf;
        }
    }
}
