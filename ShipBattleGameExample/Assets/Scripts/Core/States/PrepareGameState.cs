using System;
using System.Collections.Generic;
using Game.Level;
using Game.Player;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public class PrepareGameState : BaseState
    {
        [SerializeField] private LevelBehaviour _levelBehaviour;
        [SerializeField] private PlayerBehaviour _playerBehaviour;
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        [SerializeField] private MainMenuHandler _mainMenuHandler;
        
        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            var actions = new List<System.Action>();
            actions.Add(() => _levelBehaviour.PrepareLevel(_playerBehaviour.CurrentLevel));
            actions.Add(() => _levelBehaviour.PreparePlayerShip());
            _loadingScreenHandler.PrepareActionsOnLoading(actions);
            _mainMenuHandler.HideMainMenu();
            _loadingScreenHandler.ShowLoadingScreen();
        }

        public override void OnStateExit()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateExit)}] OK");
        }

        public void SetOnLoadingComplete(Action onLoadingComplete)
        {
            _loadingScreenHandler.SetOnLoadingComplete(onLoadingComplete);
        }
    }
}
