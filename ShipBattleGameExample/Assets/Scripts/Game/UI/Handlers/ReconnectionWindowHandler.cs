using Luxodd.Game.Scripts.Network;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class ReconnectionWindowHandler : MonoBehaviour
    {
        private IReconnectionWindowView _reconnectionWindowView;

        public void PrepareView(IReconnectionWindowView reconnectionWindowView)
        {
            _reconnectionWindowView = reconnectionWindowView;
        }

        public void ShowReconnectionWindow()
        {
            _reconnectionWindowView.Show();
        }

        public void HideReconnectionWindow()
        {
            _reconnectionWindowView.Hide();
        }

        public void SwitchToReconnection()
        {
            _reconnectionWindowView.SwitchToReconnection();
        }

        public void SwitchToReconnectionFailed()
        {
            _reconnectionWindowView.SwitchToReconnectionFailed();
        }

        private void OnReconnectServiceStateChangedHandler(ReconnectionState state)
        {
            if (state == ReconnectionState.Disconnected)
            {
                _reconnectionWindowView.Show();
                _reconnectionWindowView.SwitchToReconnection();
            }
            else if (state == ReconnectionState.Connected)
            {
                _reconnectionWindowView.Hide();
            }
            else if (state == ReconnectionState.ConnectingFailed)
            {
                _reconnectionWindowView.SwitchToReconnection();
            }
        }
    }
}