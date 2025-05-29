using System;
using System.Collections;
using System.Collections.Generic;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Handlers
{
    public class LoadingScreenHandler : MonoBehaviour
    {
        private List<System.Action> _actionsOnLoading = new List<System.Action>();

        private ILoadingScreenView _loadingScreenView;

        private System.Action _onLoadingComplete;

        private int _totalActions;
        private int _currentAction;
        private float _progress;

        public void PrepareView(ILoadingScreenView loadingScreenView)
        {
            _loadingScreenView = loadingScreenView;
        }

        public void PrepareActionsOnLoading(List<System.Action> actions)
        {
            ResetLoadingActions();
            _actionsOnLoading.Clear();
            _actionsOnLoading.AddRange(actions);
        }

        public void SetOnLoadingComplete(System.Action onComplete)
        {
            _onLoadingComplete = onComplete;
        }

        public void ShowLoadingScreen()
        {
            _loadingScreenView.Show();
            StartCoroutine(ProcessLoadingActions());
        }

        public void HideLoadingScreen()
        {
            _loadingScreenView.Hide();
        }

        private void ResetLoadingActions()
        {
            _totalActions = 0;
            _currentAction = 0;
            _progress = 0f;
            UpdateProgress();
        }

        private IEnumerator ProcessLoadingActions()
        {
            _totalActions = _actionsOnLoading.Count;
            _currentAction = 0;
            _progress = 0f;
            while (_currentAction < _totalActions)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.33f));

                _actionsOnLoading[_currentAction]?.Invoke();
                _currentAction++;
                _progress = _currentAction / (float)_totalActions;
                UpdateProgress();
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.33f));
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(ProcessLoadingActions)}] OK, loading complete.");
            _onLoadingComplete?.Invoke();
        }

        private void UpdateProgress()
        {
            _loadingScreenView.UpdateProgress(_progress);
        }
    }
}