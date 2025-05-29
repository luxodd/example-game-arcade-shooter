using System;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.States
{
    public interface IState
    {
        void OnStateEnter();
        void OnStateExit();
        
        void SetOnCompleteAction(Action<ApplicationState> action);
    }

    public abstract class BaseState : MonoBehaviour, IState
    {
        public abstract void OnStateEnter();
        public abstract void OnStateExit();

        protected void CompleteState(ApplicationState nextState)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(CompleteState)}] OK, nextState: {nextState}");
            _onCompleteAction?.Invoke(nextState);
        }
        
        private Action<ApplicationState> _onCompleteAction;
        public void SetOnCompleteAction(Action<ApplicationState> action)
        {
            _onCompleteAction = action;
        }
    }
    
    public class StateManager<TState> where TState : class, IState
    {
        private TState _currentState;

        public void SetState(TState newState)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SetState)}] OK, newState: {newState}");
            _currentState?.OnStateExit();
            _currentState = newState;
            _currentState?.OnStateEnter();
        }
    }
}
