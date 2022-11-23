/* 
ZeroMiniAVC
Copyright 2017 Malah

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace ZeroMiniAVC
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ZeroMiniAVC : MonoBehaviour
    {

        static ZeroMiniAVC Instance;

        readonly string pruneExt = ".pruned";
        readonly string cfgNode = "ZeroMiniAVC";
        string configPath;

        ConfigNode config;
        bool disabled = false;
        bool prune = true;
        bool delete = false;
        bool noMessage = false;
        bool restart = true;

        bool prunedOrDeleted = false;
        bool exitCountdownActive = false;
        double countdownStartTime;

        Log log;
        ConfigNode loadConfig()
        {
            ConfigNode[] _nodes = GameDatabase.Instance.GetConfigNodes(cfgNode);
            if (_nodes.Length > 0)
            {
                ConfigNode _node = _nodes[_nodes.Length - 1];
                if (_node.HasValue("disabled"))
                {
                    disabled = bool.Parse(_node.GetValue("disabled"));
                }
                if (_node.HasValue("prune"))
                {
                    prune = bool.Parse(_node.GetValue("prune"));
                }
                if (_node.HasValue("delete"))
                {
                    delete = bool.Parse(_node.GetValue("delete"));
                }
                if (_node.HasValue("noMessage"))
                {
                    noMessage = bool.Parse(_node.GetValue("noMessage"));
                }
                if (_node.HasValue("restart"))
                {
                    restart = bool.Parse(_node.GetValue("restart"));
                }
                return _node;
            }
            return new ConfigNode();
        }

        string mod(string path)
        {
            string[] _splitedPath = path.Split(new char[2] { '/', '\\' });
            string _mod = _splitedPath[_splitedPath.IndexOf("GameData") + 1];
            return _mod;
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            log = new Log("ZeroMiniAVC", Log.LEVEL.INFO);
            var t = System.DateTime.Now;
            log.Info("=================================================");
            log.Info("ZeroMiniAVC started at: " + t.ToString());

            Instance = this;
            DontDestroyOnLoad(Instance);
            configPath = KSPUtil.ApplicationRootPath + "GameData/ZeroMiniAVC/Config.cfg";
            config = loadConfig();
            DontDestroyOnLoad(this);
            log.Info("ZeroMiniAVC: Awake");
        }

        string[] args = null;
        void GetCLIParams()
        {
            args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                log.Info("Arg[" + i + "]: " + args[i]);
            }
        }



        void StartNewGame()
        {
            if (!restart)
                return;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = args[0];
            startInfo.Arguments = "";
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].ToLower() != "-single-instance")
                    startInfo.Arguments += args[i];
                if (i < args.Length - 1)
                    startInfo.Arguments += " ";
            }

            log.Info("startInfo.FileName: " + startInfo.FileName +
                ", startInfo.Arguments: " + startInfo.Arguments);
            Process.Start(startInfo);

        }

        void Start()
        {
            GetCLIParams();

            if (disabled)
            {
                log.Info("Disabled ... destroy.");
                Destroy(this);
                return;
            }
            screenMsg("ZeroMiniAVC started ...");

            cleanMiniAVC();

            if (affectedByDuplicateDLLBug())
            {
                duplicateDLLWarning();
            }

            if (!prune && !delete)
            {
                cleanData();
            }

            ConfigNode _config = new ConfigNode();
            _config.AddNode(config);
            _config.Save(configPath);
            if (prunedOrDeleted)
            {
#if false
                ShowWarning();
#endif
                log.Info("showwin set to true");
                showWin = true;
                exitCountdownActive = true;
                countdownStartTime = Time.realtimeSinceStartup;
            }
            else
            {
                screenMsg("ZeroMiniAVC destroyed...");
                Destroy(this);
            }
        }

#if false
        private PopupDialog popup;

        public void ShowWarning()
        {
            exitCountdownActive = true;
            countdownStartTime = Time.realtimeSinceStartup;

            InputLockManager.SetControlLock(ControlTypes.All, "ZeroMiniAVC");
            string
                dialogMsg = "One or more MiniAVC.dll files have been detected and pruned.  The game needs to be restarted to avoid potential problems, and will restart in 15 seconds";
            if (restart)
                dialogMsg += ".  The game will automatically restart after exiting";

            log.Info(dialogMsg);
            string windowTitle = "WARNING";

            DialogGUIBase[] options =
            {
                new DialogGUIButton("Press to exit"+ (restart?" and restart":""), OkToExit)
            };

            MultiOptionDialog confirmationBox = new MultiOptionDialog("ZeroMiniAVC", dialogMsg, windowTitle, HighLogic.UISkin, options);

            popup = PopupDialog.SpawnPopupDialog(confirmationBox, false, HighLogic.UISkin);
        }
#endif
        bool showWin = false;
        Rect winRect = new Rect(0, 0, 450, 150);
        const int COUNTDOWN = 30;
        bool initted = false;
        GUIStyle window;

        void OnGUI()
        {
            if (showWin)
            {
                if (!initted)
                {
                    GUI.color = Color.grey;
                    window = new GUIStyle(HighLogic.Skin.window);
                    //window.normal.background.SetPixels( new[] { new Color(0.5f, 0.5f, 0.5f, 1f) });
                    window.active.background = window.normal.background;

                    Texture2D tex = window.normal.background; //.CreateReadable();
                    var pixels = tex.GetPixels32();

                    for (int i = 0; i < pixels.Length; ++i)
                        pixels[i].a = 255;

                    tex.SetPixels32(pixels); tex.Apply();
                    window.active.background =
                    window.focused.background =
                    window.normal.background = tex;

                }
                GUI.skin = HighLogic.Skin;
                winRect.x = (Screen.width - winRect.width) / 2;
                winRect.y = (Screen.height - winRect.height) / 2;

                InputLockManager.SetControlLock(ControlTypes.All, "ZeroMiniAVC");

                winRect = GUILayout.Window(939387374, winRect, WarnWin, "ZeroMiniAVC Restart", window);
            }
        }

        void WarnWin(int i)
        {
            var now = Time.realtimeSinceStartup;
            int timeLeft = COUNTDOWN - (int)(now - countdownStartTime);

            GUILayout.BeginVertical();
            GUILayout.Label("One or more MiniAVC.dll files have been detected and pruned.");
            GUILayout.Label("The game needs to be restarted to avoid potential problems,");
            GUILayout.Label("and will restart in " + timeLeft + " seconds");
            GUILayout.Space(20);
            if (restart)
                GUILayout.Label("The game will automatically restart after exiting");


            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Exit Game now" + (restart ? " and Restart" : ""), GUILayout.Width(250)))
            {
                OkToExit();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

        }

        bool newgameStarted = false;
        public void OkToExit()
        {
            if (!newgameStarted)
            {
                newgameStarted = true;
                StartNewGame();
            }
            Application.Quit();
        }

        void FixedUpdate()
        {
            if (prunedOrDeleted && exitCountdownActive)
            {
                InputLockManager.SetControlLock(ControlTypes.All, "ZeroMiniAVC");
                var now = Time.realtimeSinceStartup;
                if (now - countdownStartTime > COUNTDOWN)
                    OkToExit();
            }
        }

        void DoCleanup(string path)
        {
            string _mod = mod(path);
            string _prunePath = path + pruneExt;
            if (File.Exists(_prunePath))
            {
                File.Delete(_prunePath);
            }
            if (prune)
            {
                File.Move(path, _prunePath);
                ConfigNode _cfgMod = config.AddNode("mod");
                _cfgMod.AddValue("name", _mod);
                _cfgMod.AddValue("pruned", _prunePath);
                screenMsg("MiniAVC pruned for " + _mod);
                log.Info("MiniAVC pruned for " + _mod + ", path: " + _prunePath);
                prunedOrDeleted = true;
            }
            else if (delete)
            {
                File.Delete(path);
                screenMsg("MiniAVC deleted for " + _mod);
                prunedOrDeleted = true;
            }
            else
            {
                screenMsg("MiniAVC disabled for " + _mod);
            }

        }

        void cleanMiniAVC()
        {
            AssemblyLoader.LoadedAssembyList _assemblies = AssemblyLoader.loadedAssemblies;
            log.Info("cleanMiniAVC");
            bool cleanupDone = false;
            for (int _i = _assemblies.Count - 1; _i >= 0; --_i)
            {
                AssemblyLoader.LoadedAssembly _assembly = _assemblies[_i];

                if ((_assembly.name.ToLower().Contains("miniavc") || _assembly.name.ToLower().Contains("miniavc-v2")) &&
                    !_assembly.name.ToLower().Contains("zerominiavc"))
                {
                    _assembly.Unload();
                    AssemblyLoader.loadedAssemblies.RemoveAt(_i);
                    DoCleanup(_assembly.path);
                    cleanupDone = true;
                }
            }
            // From KSP 1.12.3 onward, it only loaded a single DLL of each type. so if there were multiple DLLs in 
            // the game with the same name, it would only load the newest.  Good news is that after the step above, 
            // any others found will not be loaded and can be simply pruned
            if (cleanupDone)
            {
                var parentDirectory = KSPUtil.ApplicationRootPath;

                foreach (string file in System.IO.Directory.GetFiles(parentDirectory, "MiniAVC.dll", SearchOption.AllDirectories))
                {
                    log.Info("Unloaded MiniAVC.dll file found: " + file);
                    DoCleanup(file);
                }
                // Following added for Mac & Linux, since they have case sensitive file names
                if (!System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    foreach (string file in System.IO.Directory.GetFiles(parentDirectory, "*", SearchOption.AllDirectories))
                    {
                        if (file.ToLower().Contains("miniavc.dll"))
                        {
                            log.Info("Unloaded MiniAVC.dll file found: " + file);
                            DoCleanup(file);
                        }
                    }
                }
            }
        }

        private bool affectedByDuplicateDLLBug()
        {
            // The duplicate DLL bug first appeared in KSP 1.12.0 and was fixed in 1.12.3
            return Versioning.version_major == 1
                && Versioning.version_minor == 12
                && Versioning.Revision >= 0
                && Versioning.Revision <= 2;
        }

        private void duplicateDLLWarning()
        {
            FindAllDLLs();
            if (duplicateDlls.Count > 0)
                this.gameObject.AddComponent<IssueGui>();
        }


        Dictionary<string, string> installedDlls = new Dictionary<string, string>();
        internal static List<string> duplicateDlls = new List<string>();
        void FindAllDLLs()
        {
            var dir = KSPUtil.ApplicationRootPath + "GameData/";
            var files = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                if ((f.ToLower().Contains("miniavc.dll") || f.ToLower().Contains("miniavc-v2.dll")) &&
                    !f.ToLower().Contains("zerominiavc"))
                {
                    DoCleanup(f);
                }
                else
                {
                    if (installedDlls.ContainsKey(Path.GetFileName(f)))
                    {
                        if (Path.GetFileName(f) != "KSP-AVC.dll")
                        {
                            log.Info("Duplicate DLLs found: " + installedDlls[Path.GetFileName(f)] + " : " + f);
                            duplicateDlls.Add(f);
                            if (!duplicateDlls.Contains(installedDlls[Path.GetFileName(f)]))
                                duplicateDlls.Add(installedDlls[Path.GetFileName(f)]);
                        }
                    }
                    else
                        installedDlls.Add(Path.GetFileName(f), f);
                }
            }
            duplicateDlls = duplicateDlls.OrderByAlphaNumeric(o => Path.GetFileName(o) + o);
        }

        void cleanData()
        {
            ConfigNode[] _cfgMods = config.GetNodes("mod");
            for (int _i = _cfgMods.Length - 1; _i >= 0; --_i)
            {
                ConfigNode _cfgMod = _cfgMods[_i];
                string _prunedPath = _cfgMod.GetValue("pruned");
                string _mod = _cfgMod.GetValue("name");
                if (File.Exists(_prunedPath))
                {
                    string _unprunedPath = _prunedPath.Substring(0, _prunedPath.Length - pruneExt.Length);
                    if (File.Exists(_unprunedPath))
                    {
                        File.Delete(_prunedPath);
                        screenMsg("MiniAVC deleted prune duplication for " + _mod);
                    }
                    else
                    {
                        File.Move(_prunedPath, _unprunedPath);
                        screenMsg("MiniAVC unpruned for " + _mod);
                    }
                }
                else
                {
                    screenMsg("MiniAVC data removed for " + _mod);
                }
                config.RemoveNode(_cfgMod);
            }
        }

        void screenMsg(string msg)
        {
            log.Info(msg);
            if (noMessage)
            {
                return;
            }
            ScreenMessages.PostScreenMessage(msg, 10);
        }
    }
}


// From MiniAVC GPLv3 Copyright (C) 2014 CYBUTEK
namespace MiniAVC
{
    public class Logger : MonoBehaviour
    {
        void Awake()
        {
            UnityEngine.Debug.Log("MiniAVC.Logger: Destroy");
            Destroy(this);
        }
    }
    public class Starter : MonoBehaviour
    {
        void Awake()
        {
            UnityEngine.Debug.Log("MiniAVC.Starter: Destroy");
            Destroy(this);
        }
    }
}