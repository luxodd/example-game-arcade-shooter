using System;
using Game.UI.Views;
using Luxodd.Game.Scripts.HelpersAndUtils;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class CompletedDemoWindowHandler : MonoBehaviour
    {
        private const string OkayButtonKey = "OK";

        private ICompletedDemoWindowView _completedDemoWindowView;

        private Action _okayButtonClickCallback;

        private int _counter = 0;

        public void PrepareView(ICompletedDemoWindowView completedDemoWindowView)
        {
            _completedDemoWindowView = completedDemoWindowView;
            _completedDemoWindowView.KeyboardNavigator.OnKeySubmitted.AddListener(OnVirtualKeySubmitted);
        }

        public void ShowCompletedDemoWindow()
        {
            _completedDemoWindowView.Show();
        }

        public void HideCompletedDemoWindow()
        {
            _completedDemoWindowView.Hide();
        }

        public void SetOkayButtonClickCallback(Action callback)
        {
            _okayButtonClickCallback = callback;
            _completedDemoWindowView.SetOkayButtonCallback(OnOkayButtonClickHandler);
        }

        private void OnVirtualKeySubmitted(string stringValue)
        {
            switch (stringValue)
            {
                case OkayButtonKey:
                    OnOkayButtonClickHandler();
                    break;
            }
        }

        private void OnOkayButtonClickHandler()
        {
            if (_counter > 0) return;

            _counter++;
            _okayButtonClickCallback?.Invoke();

            CoroutineManager.NextFrameAction(3, () => _counter = 0);
        }
    }
}