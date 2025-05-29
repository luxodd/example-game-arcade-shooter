using System;
using System.Collections.Generic;
using Core.Audio;
using Game.UI.Handlers;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public class LaunchLoadingState : BaseState
    {
        [SerializeField] private LoadingScreenHandler _loadingScreenHandler;
        [SerializeField] private AudioManager _audioManager;

        public void SetOnLoadingComplete(Action onLoadingComplete)
        {
            _loadingScreenHandler.SetOnLoadingComplete(onLoadingComplete);
        }
        public override void OnStateEnter()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnStateEnter)}] OK");
            var actions = new List<System.Action>();

            actions.Add(() => LoggerHelper.Log($"[{GetType().Name}][{nameof(OnStateEnter)}] OK, action in loading screen"));
            _loadingScreenHandler.PrepareActionsOnLoading(actions);
            _loadingScreenHandler.ShowLoadingScreen();
            _audioManager.PlayMusic(MusicType.Menu);
        }

        public override void OnStateExit()
        {
            
        }
    }
}
