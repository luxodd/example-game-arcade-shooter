using System;
using System.Reflection;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Luxodd.Integration
{
    /// <summary>
    /// Live score for the platform's leaderboard panel (score_update). Needs plugin 1.2.0 or later.
    ///
    ///  - Plugin 1.3.0+ has <c>WebSocketCommandHandler.ReportScore</c> and tracks the run itself
    ///    (from the level_begin ack, every 2 s, final value before level_end). It is looked up by
    ///    name, so this file also builds on 1.2.0.
    ///  - Plugin 1.2.0 has the opt-in <see cref="ScoreUpdateService"/>. This class adds one next to
    ///    the game's WebSocketCommandHandler and drives it: tracking starts when level_begin is
    ///    acknowledged (<see cref="LevelBegun"/>), every score change updates it (sent every 2 s),
    ///    and <see cref="BeforeLevelEnd"/> sends the final value immediately and stops, so the
    ///    last score_update equals the level_end score (TP-2).
    ///
    /// Game code: call <see cref="LevelBegun"/> in the level_begin success callback,
    /// <see cref="Report"/> on every score change, and send level_end from inside
    /// <see cref="BeforeLevelEnd"/>'s callback. <see cref="LuxoddRunFlow"/> does the last two for you.
    /// </summary>
    public static class LuxoddScore
    {
        private static bool resolved;
        private static MethodInfo reportMethod; // 1.3.0+
        private static FieldInfo serviceHandlerField; // 1.2.0 ScoreUpdateService._commandHandler
        private static WebSocketCommandHandler handler;
        private static ScoreUpdateService service;
        private static Action<int, int> report;
        private static int level, score;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            handler = null;
            service = null;
            report = null;
            level = score = 0;
        }

        /// <summary>The plugin tracks the score itself (1.3.0+ ReportScore).</summary>
        public static bool PluginTracksScore { get { Resolve(); return reportMethod != null; } }

        /// <summary>Call when level_begin is acknowledged: the run's score starts being sent.</summary>
        public static void LevelBegun(int runLevel, int initialScore = 0, WebSocketCommandHandler commands = null)
        {
            level = Mathf.Max(0, runLevel);
            score = Mathf.Max(0, initialScore);
            if (!Bind(commands)) return;
            if (report != null) { report(level, score); return; }
            var tracker = Service();
            if (tracker == null) return;
            if (tracker.IsTracking) tracker.StopTracking();
            tracker.StartTracking(level, score);
        }

        /// <summary>Call on every score change.</summary>
        public static void Report(int runLevel, int newScore, WebSocketCommandHandler commands = null)
        {
            level = Mathf.Max(0, runLevel);
            score = Mathf.Max(0, newScore);
            if (!Bind(commands)) return;
            if (report != null) { report(level, score); return; }
            var tracker = Service();
            if (tracker != null && tracker.IsTracking) tracker.UpdateCurrentState(level, score);
        }

        /// <summary>
        /// Sends the final score now (1.2.0: immediate score_update, at most 2 s), stops tracking,
        /// then calls <paramref name="sendLevelEnd"/>. Always calls it exactly once.
        /// </summary>
        public static void BeforeLevelEnd(int runLevel, int finalScore, Action sendLevelEnd, WebSocketCommandHandler commands = null)
        {
            Report(runLevel, finalScore, commands);
            var tracker = report == null ? Service() : null;
            if (tracker == null || !tracker.IsTracking) { sendLevelEnd?.Invoke(); return; }
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                tracker.StopTracking();
                sendLevelEnd?.Invoke();
            }
            tracker.SendCurrentScoreNow(_ => Finish(), (_, __) => Finish());
        }

        /// <summary>Stops sending (the run ended without a level_end, or the session is leaving).</summary>
        public static void Stop()
        {
            if (service != null && service.IsTracking) service.StopTracking();
        }

        private static bool Bind(WebSocketCommandHandler commands)
        {
            Resolve();
            if (commands != null && commands != handler)
            {
                handler = commands;
                report = null;
            }
            if (handler == null)
            {
#if UNITY_2023_1_OR_NEWER
                handler = Object.FindAnyObjectByType<WebSocketCommandHandler>(FindObjectsInactive.Include);
#else
                handler = Object.FindObjectOfType<WebSocketCommandHandler>(true);
#endif
                report = null;
            }
            if (handler == null) return false;
            if (reportMethod != null && report == null)
                report = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>), handler, reportMethod, false);
            return true;
        }

        private static ScoreUpdateService Service()
        {
            if (service != null || handler == null || serviceHandlerField == null) return service;
            service = handler.GetComponent<ScoreUpdateService>();
            if (service == null)
            {
                // Disabled while it is wired, so its OnEnable subscribes to the right session.
                bool wasActive = handler.gameObject.activeSelf;
                service = handler.gameObject.AddComponent<ScoreUpdateService>();
                service.enabled = false;
                serviceHandlerField.SetValue(service, handler);
                service.enabled = true;
                if (!wasActive) Debug.LogWarning("[LuxoddScore] WebSocketCommandHandler is inactive; score updates wait for it.");
            }
            else if (serviceHandlerField.GetValue(service) == null)
            {
                serviceHandlerField.SetValue(service, handler);
            }
            return service;
        }

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            reportMethod = typeof(WebSocketCommandHandler).GetMethod("ReportScore",
                BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(int) }, null);
            serviceHandlerField = typeof(ScoreUpdateService).GetField("_commandHandler",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Log("[LuxoddScore] live score via " + (reportMethod != null ? "plugin ReportScore"
                : serviceHandlerField != null ? "ScoreUpdateService" : "nothing (plugin has neither)"));
        }
    }
}
