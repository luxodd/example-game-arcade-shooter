using Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public interface ILoadingScreenView : IView
    {
        void UpdateProgress(float progress);
    }

    public class LoadingScreenView : BaseView, ILoadingScreenView
    {
        public override ViewType ViewType => ViewType.LoadingScreen;

        [SerializeField] private Slider _progressSlider;

        public void UpdateProgress(float progress)
        {
            _progressSlider.value = progress;
        }

        protected override void OnAwake()
        {
            _progressSlider.value = 0f;
        }
    }
}