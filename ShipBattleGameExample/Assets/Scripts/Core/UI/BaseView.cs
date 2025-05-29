using System;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.UI
{
    public interface IView
    {
        bool Visible { get; set; }
        
        void Show();
        void Hide();
    }

    public enum ViewType
    {
        LoadingScreen,
        MainMenu,
        GameOverWindow,
        GameScreen,
        LeaderboardWindow,
        ReconnectionWindow,
        ContinueGameWindow,
        LevelCompleteWindow,
        NotEnoughMoneyWindow,
        CreditsWidget,
        PinCodeEnteringPopup,
        NumericKeyboardPopup,
        CompletedDemoWindow,
        ReplayDemoWindow,
    }
    
    public abstract class BaseView : MonoBehaviour, IView
    {
        public bool Visible { get; set; }
        public abstract ViewType ViewType { get; }
        
        [SerializeField] private bool _hideOnAwake = true;
        
        public void Show()
        {
            LoggerHelper.Log($"[{GetType().Name}][{nameof(Show)}] OK");
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            
            OnShow();
        }

        public void Hide()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Hide)}] OK");
            OnBeforeHide(() =>
            {
                transform.SetAsFirstSibling();
                gameObject.SetActive(false);
            
                OnHide();    
            });
            
        }

        private void Awake()
        {
            if (_hideOnAwake)
            {
                Hide();
            }
            
            OnAwake();
        }

        private void OnDestroy()
        {
            OnDestroyInner();
        }
        
        protected virtual void OnShow(){}
        protected virtual void OnHide(){}
        protected virtual void OnAwake(){}
        protected virtual void OnDestroyInner(){}

        protected virtual void OnBeforeHide(Action onDone)
        {
            onDone?.Invoke();
        }
    }
}
