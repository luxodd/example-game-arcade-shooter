using System;
using System.Collections;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using UnityEngine;

namespace Luxodd.Integration
{
    /// <summary>
    /// The standard start and end of a run on plugin 1.2.0+ (integration criteria ES-1 to ES-8,
    /// TP-1, TP-2), the same flow as Scootlet and Split Time:
    ///
    ///   BeginAttempt  level_begin; on its ack the live score starts (LuxoddScore.LevelBegun).
    ///   EndAttempt    final score_update, then level_end (ack wait capped at 5 s), then ONE host
    ///                 popup: Continue / Restart (Fast Restart) / End.
    ///                   Continue  the same session goes on: the game resumes and calls
    ///                             BeginAttempt again for the new attempt.
    ///                   Restart   the plugin has already handed over to the replacement session;
    ///                             the game resets to a clean start and calls BeginAttempt.
    ///                   End       BackToSystem(); nothing is sent after it.
    ///                   Cancel    the popup is asked for again after a short beat.
    ///                 A Fast Restart failure ends the session cleanly (BackToSystem).
    ///   EndSession    final score_update and level_end, then BackToSystem (no popup).
    ///
    /// The host owns the popup and its timeout: the game never ends a session option by itself.
    /// In the Editor and non-WebGL players the popup is skipped and Continue (or, for a game
    /// without Continue, Restart) is chosen.
    /// </summary>
    public sealed class LuxoddRunFlow : MonoBehaviour
    {
        private const float LevelEndAckTimeout = 5f;
        private const float CancelRetryDelay = 3.5f;

        private static LuxoddRunFlow runner;
        private static int popupGeneration;
        private static bool popupOpen, ending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runner = null;
            popupGeneration = 0;
            popupOpen = ending = false;
        }

        /// <summary>A popup is open or a level_end is on its way; do not start another end.</summary>
        public static bool IsBusy => popupOpen;

        /// <summary>The session is leaving (End chosen or EndSession called).</summary>
        public static bool IsEnding => ending;

        public static void BeginAttempt(WebSocketCommandHandler commands, int level, int initialScore = 0,
            Action onAck = null, Action<int, string> onFailure = null)
        {
            if (commands == null) { onFailure?.Invoke(-1, "No WebSocketCommandHandler."); return; }
            ending = false;
            LuxoddScore.Report(level, initialScore, commands);
            commands.SendLevelBeginRequestCommand(level,
                () =>
                {
                    LuxoddScore.LevelBegun(level, initialScore, commands);
                    onAck?.Invoke();
                },
                (code, message) =>
                {
                    Debug.LogWarning($"[LuxoddRunFlow] level_begin {level} failed ({code}): {message}");
                    onFailure?.Invoke(code, message);
                });
        }

        /// <param name="showResults">Optional: runs after level_end is acknowledged (the game's own
        /// game-over or leaderboard screen) and calls its argument when the popup may open.</param>
        /// <param name="allowContinue">False for a game that cannot resume the attempt (the popup
        /// then offers Restart and End only: SendSessionOptionFastRestart).</param>
        public static void EndAttempt(WebSocketService socket, WebSocketCommandHandler commands, int level, int score,
            Action onContinue, Action onRestart, Action onEnd = null, Action<Action> showResults = null,
            bool allowContinue = true)
        {
            if (!allowContinue) onContinue = null;
            if (popupOpen || ending) return;
            popupOpen = true;
            int generation = ++popupGeneration;
            SendLevelEnd(commands, level, score, () =>
            {
                if (showResults == null) { OpenOptions(socket, generation, onContinue, onRestart, onEnd); return; }
                bool opened = false;
                showResults(() =>
                {
                    if (opened) return;
                    opened = true;
                    OpenOptions(socket, generation, onContinue, onRestart, onEnd);
                });
            });
        }

        public static void EndSession(WebSocketService socket, WebSocketCommandHandler commands, int level, int score,
            Action onLeft = null)
        {
            if (ending) return;
            ending = true;
            popupGeneration++;
            SendLevelEnd(commands, level, score, () => Leave(socket, onLeft));
        }

        private static void SendLevelEnd(WebSocketCommandHandler commands, int level, int score, Action then)
        {
            if (commands == null) { then(); return; }
            bool done = false;
            void Finish(string outcome)
            {
                if (done) return;
                done = true;
                if (outcome != null) Debug.LogWarning("[LuxoddRunFlow] level_end " + outcome);
                then();
            }
            LuxoddScore.BeforeLevelEnd(level, score, () =>
            {
                commands.SendLevelEndRequestCommand(level, Mathf.Max(0, score),
                    () => Finish(null),
                    (code, message) => Finish($"failed ({code}): {message}"));
                // Plugin requests have no timeout of their own.
                Runner().StartCoroutine(After(LevelEndAckTimeout, () => Finish("not acknowledged within 5 s; going on")));
            }, commands);
        }

        private static void OpenOptions(WebSocketService socket, int generation, Action onContinue, Action onRestart, Action onEnd)
        {
            if (generation != popupGeneration) return;
            var offline = onContinue != null ? SessionOptionAction.Continue : SessionOptionAction.Restart;
#if UNITY_EDITOR || !UNITY_WEBGL
            Choose(socket, generation, offline, onContinue, onRestart, onEnd);
#else
            if (socket == null) { Choose(socket, generation, offline, onContinue, onRestart, onEnd); return; }
            Action<SessionOptionAction> onAction = action => Choose(socket, generation, action, onContinue, onRestart, onEnd);
            Action<int, string> onFailure = (code, message) =>
                {
                    if (generation != popupGeneration) return;
                    Debug.LogWarning($"[LuxoddRunFlow] Fast Restart failed ({code}): {message}; ending the session.");
                    ending = true;
                    popupOpen = false;
                    Leave(socket, onEnd);
                };
            if (onContinue != null) socket.SendSessionOptionFastRestartWithContinue(onAction, onFailure);
            else socket.SendSessionOptionFastRestart(onAction, onFailure);
#endif
        }

        private static void Choose(WebSocketService socket, int generation, SessionOptionAction action,
            Action onContinue, Action onRestart, Action onEnd)
        {
            if (generation != popupGeneration) return;
            switch (action)
            {
                case SessionOptionAction.Continue when onContinue != null:
                    popupOpen = false;
                    onContinue();
                    break;
                case SessionOptionAction.Continue: // not offered by this game: start over instead
                    popupOpen = false;
                    onRestart?.Invoke();
                    break;
                case SessionOptionAction.Restart:
                    popupOpen = false;
                    onRestart?.Invoke();
                    break;
                case SessionOptionAction.End:
                    popupOpen = false;
                    ending = true;
                    Leave(socket, onEnd);
                    break;
                default: // Cancel: ask again after a beat; the attempt is already reported.
                    Runner().StartCoroutine(After(CancelRetryDelay, () => OpenOptions(socket, generation, onContinue, onRestart, onEnd)));
                    break;
            }
        }

        private static void Leave(WebSocketService socket, Action onLeft)
        {
            LuxoddScore.Stop();
            onLeft?.Invoke();
            if (socket != null) socket.BackToSystem();
        }

        private static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }

        private static LuxoddRunFlow Runner()
        {
            if (runner != null) return runner;
            var go = new GameObject("[LuxoddRunFlow]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            runner = go.AddComponent<LuxoddRunFlow>();
            return runner;
        }
    }
}
