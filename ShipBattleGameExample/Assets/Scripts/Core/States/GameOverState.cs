using System;
using Game.CameraInner;
using Game.PlayerShip;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public class GameOverState : BaseState
    {
        [SerializeField] private KeyboardControlAdapter _keyboardControlAdapter;
        [SerializeField] private CameraFollowBehaviour _cameraFollowBehaviour;
        
        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            _keyboardControlAdapter.OutTheGame();
            _cameraFollowBehaviour.OutTheGame();
            _cameraFollowBehaviour.enabled = false;
        }

        public override void OnStateExit()
        {
            
        }
    }
}
