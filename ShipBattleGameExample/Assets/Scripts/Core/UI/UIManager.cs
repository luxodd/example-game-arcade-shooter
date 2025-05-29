using System;
using System.Collections.Generic;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Core.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private List<BaseView> _views;

        public T ProvideView<T>(ViewType viewType) where T : class, IView
        {
            var view = _views.Find(view => view.ViewType == viewType);
            if (view == null)
                LoggerHelper.LogError(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(ProvideView)}] Error, can not find viewType: {viewType}, view: {typeof(T).Name}");
            return view as T;
        }
    }
}
