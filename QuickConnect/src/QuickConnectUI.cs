using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib; // Für AccessTools
using UnityEngine.SceneManagement;
using System.Collections;

namespace QuickConnect
{
    class QuickConnectUI : MonoBehaviour
    {
        public static QuickConnectUI instance;
        private Coroutine _connectWatchdog;
        private Task<IPHostEntry> resolveTask;
        private Servers.Entry connecting;

        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        class FejdStartupPatch
        {
            static void Postfix()
            {
                if (QuickConnectUI.instance == null)
                {
                    var go = new GameObject("QuickConnectUI");
                    QuickConnectUI.instance = go.AddComponent<QuickConnectUI>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
            }
        }
        void Update()
        {

            if (resolveTask != null)
            {
                if (resolveTask.IsFaulted)
                {
                    Mod.Log.LogError($"Error resolving IP: {resolveTask.Exception}");
                    //ShowError(resolveTask.Exception.InnerException?.Message ?? resolveTask.Exception.Message);
                    resolveTask = null;
                    connecting = null;
                }
                else if (resolveTask.IsCanceled)
                {
                    resolveTask = null;
                    connecting = null;
                }
                else if (resolveTask.IsCompleted)
                {
                    foreach (var addr in resolveTask.Result.AddressList)
                    {
                        if (addr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Mod.Log.LogInfo($"Resolved: {addr}");
                            resolveTask = null;
                            ZSteamMatchmaking.instance.QueueServerJoin($"{addr}:{connecting.port}");
                            return;
                        }
                    }
                    resolveTask = null;
                    connecting = null;
                    //ShowError("Server DNS resolved to no valid addresses");
                }
            }
        }

        /* ================================================================= */
        /* Hauptmenü fertig → Button bauen                                   */
        /* ================================================================= */
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.ToLower() != "start") return;
            BuildJoinButton();
        }
        private IEnumerator ConnectWatchdog(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                // Methode 1: Auf Peers prüfen
                if (ZNet.instance != null && ZNet.instance.GetPeers().Count > 0)
                {
                    yield break;
                }

                // Methode 2: Szenewechsel
                if (SceneManager.GetActiveScene().name != "start")
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Mod.Log.LogWarning("[QuickConnect] Connection timeout – treating as failed.");
            JoinServerFailed();
        }

        private void BuildJoinButton()
        {
            if (GameObject.Find("JoinMyServerButton") != null) return;   // schon da

            /* -------- Vorlage suchen -------- */
            GameObject template = null;

            if (template == null)
            {
                var menu = GameObject.Find("GuiRoot/GUI/StartGui/Menu");
                template = menu?.GetComponentsInChildren<Button>(true)
                           .FirstOrDefault(b => b.gameObject.activeSelf)?.gameObject;
            }

            // (d) Wenn alles scheitert: eigenen Canvas bauen
            Canvas ownCanvas = null;
            if (template == null)
            {
                ownCanvas = new GameObject("JoinMyServerCanvas").AddComponent<Canvas>();
                ownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ownCanvas.sortingOrder = 999;
                ownCanvas.gameObject.AddComponent<CanvasScaler>()
                         .uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                template = new GameObject("OverlayBtn",
                                          typeof(RectTransform),
                                          typeof(Button),
                                          typeof(Image));
                template.transform.SetParent(ownCanvas.transform, false);
                var t = new GameObject("Text",
                                       typeof(RectTransform),
                                       typeof(TextMeshProUGUI));
                t.transform.SetParent(template.transform, false);
                var tmp = t.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28;
            }

            /* -------- Klonen & anpassen -------- */
            var go = Instantiate(template, template.transform.parent, false);
            go.name = "JoinMyServerButton";
            go.SetActive(true);

            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = "Join " + Servers.entries[0].name;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 220);
            rt.SetAsLastSibling();  // ← diese Zeile gleich entfernen oder ändern


            var menu2 = GameObject.Find("GuiRoot/GUI/StartGui/Menu");
            GameObject startButton = null;
            startButton = menu2?.GetComponentsInChildren<Button>(true)
                       .FirstOrDefault(b => b.gameObject.activeSelf)?.gameObject;
            if (startButton != null)
            {
                go.transform.SetSiblingIndex(startButton.transform.GetSiblingIndex());
            }
            else
            {
                Mod.Log.LogWarning("Start Game button not found → Join My Server button stays at end");
            }


            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (Servers.entries.Count > 0)
                {
                    DoConnect(Servers.entries[0]);
                }
                else
                {
                    Mod.Log.LogInfo("No servers defined");
                }
            });
        }

        void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Mod.Log.LogInfo("Awake");
            Servers.Init();
        }
        private void StopWatchdog()
        {
            if (_connectWatchdog != null)
            {
                StopCoroutine(_connectWatchdog);
                _connectWatchdog = null;
            }
        }

        private void StartWatchdog(float seconds = 20f)
        {
            StopWatchdog();
            _connectWatchdog = StartCoroutine(ConnectWatchdog(seconds));
        }
        private void DoConnect(Servers.Entry server)
        {
            connecting = server;
            Mod.Log.LogInfo("DoConnect");

            try
            {
                IPAddress.Parse(server.ip);
                ZSteamMatchmaking.instance.QueueServerJoin($"{server.ip}:{server.port}");
            }
            catch (FormatException)
            {
                Mod.Log.LogInfo($"Resolving: {server.ip}");
                resolveTask = Dns.GetHostEntryAsync(server.ip);
            }

            StartWatchdog(20f); // ← hier aktivieren
        }
        public void ShowError(string msg)
        {
            //errorMsg = msg;
            Debug.LogError($"QuickConnect Error: {msg}");
            // Hier könntest du später ein separates Error-Panel bauen
        }

        public string CurrentPass()
        {
            return connecting?.pass;
        }

        public void JoinServerFailed()
        {
            StopWatchdog();
            ShowError("Server connection failed");
            connecting = null;
            resolveTask = null;
        }

        public void AbortConnect()
        {
            StopWatchdog();
            connecting = null;
            resolveTask = null;
        }
    }
}