using System;
using System.Collections.Generic;
using Game.Level;
using Game.Player;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public class RestartGameState : BaseState
    {
        [Header("UI Handlers")]
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        
        [Header("Gameplay")]
        [SerializeField] private LevelBehaviour _levelBehaviour;
        [SerializeField] private PlayerBehaviour _playerBehaviour;
        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            
            var actions = new List<System.Action>();
            actions.Add(() => LoggerHelper.Log($"[{GetType().Name}][{nameof(OnStateEnter)}] OK, action in loading screen"));
            actions.Add(ClearProjectiles);
            actions.Add(ClearEnemies);
            actions.Add(ClearPlayer);
            actions.Add(ClearLevel);
            
            actions.Add(() => _levelBehaviour.PrepareLevel(_playerBehaviour.CurrentLevel));
            actions.Add(() => _levelBehaviour.PreparePlayerShip());
            
            _loadingScreenHandler.SetOnLoadingComplete((() => CompleteState(ApplicationState.Gameplay)));
            _loadingScreenHandler.PrepareActionsOnLoading(actions);
            _loadingScreenHandler.ShowLoadingScreen();
        }

        private void ClearLevel()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ClearLevel)}] OK");
            _levelBehaviour.DestroyLevel();
        }

        private void ClearEnemies()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ClearEnemies)}] OK");
            _levelBehaviour.ClearEnemies();
        }

        private void ClearPlayer()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ClearPlayer)}] OK");
            _levelBehaviour.DestroyPlayerShip();
        }

        private void ClearProjectiles()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(ClearProjectiles)}] OK");
            _levelBehaviour.ClearProjectiles();
        }

        public override void OnStateExit()
        {
            
        }
    }
}
