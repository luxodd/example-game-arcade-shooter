using System;
using System.Collections.Generic;
using Game.Level;
using Game.UI;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public class BackToMainMenuState : BaseState
    {
        [Header("UI Handlers")]
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        
        
        [SerializeField] private LevelBehaviour _levelBehaviour;
        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            
            var actions = new List<System.Action>();
            actions.Add(() => LoggerHelper.Log($"[{GetType().Name}][{nameof(OnStateEnter)}] OK, action in loading screen"));
            actions.Add(ClearProjectiles);
            actions.Add(ClearEnemies);
            actions.Add(ClearPlayer);
            actions.Add(ClearLevel);
            
            _loadingScreenHandler.SetOnLoadingComplete((() => CompleteState(ApplicationState.MainMenu)));
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
