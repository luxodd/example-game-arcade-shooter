using UnityEngine;

namespace Game.UI.Items.Keyboard
{
    public abstract class NavigableItem : MonoBehaviour, IExecutable, ISelectable
    {
        [SerializeField] private GameObject _selectedItem = null;

        protected bool _isSelected = false;

        //navigable item
        public abstract void Execute();

        public void Select()
        {
            if (_isSelected) return;
            _isSelected = true;

            _selectedItem?.SetActive(true);

            OnSelect();
        }

        public void Deselect()
        {
            _isSelected = false;

            _selectedItem?.SetActive(false);

            OnDeselect();
        }

        private void Awake()
        {
            Deselect();

            OnAwake();
        }

        protected virtual void OnSelect()
        {
        }

        protected virtual void OnDeselect()
        {
        }

        protected virtual void OnAwake()
        {
        }
    }
}