using System;
using Game.UI.Views;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class CreditsWidgetHandler : MonoBehaviour
    {
        private ICreditsWidgetView _widgetView;

        public void PrepareView(ICreditsWidgetView widgetView)
        {
            _widgetView = widgetView;
        }

        public void ShowCreditsWidget()
        {
            _widgetView.Show();
        }

        public void HideCreditsWidget()
        {
            _widgetView.Hide();
        }

        public void SetCreditsCount(int creditsCount)
        {
            _widgetView.SetCreditsCount(creditsCount);
        }

        public void AddCreditsAnimated(int currentCredits, int creditsToAdd, Action onDone)
        {
            _widgetView.AddAnimatedCredits(currentCredits, creditsToAdd, onDone);
        }

        public void RemoveCreditsAnimated(int currentCredits, int creditsToRemove, Action onDone)
        {
            _widgetView.RemoveAnimatedCredits(currentCredits, creditsToRemove, onDone);
        }
    }
}