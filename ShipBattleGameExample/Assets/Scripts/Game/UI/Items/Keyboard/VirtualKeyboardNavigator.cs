using System;
using System.Collections.Generic;
using System.Linq;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

namespace Game.UI.Items.Keyboard
{
    public enum NavigationType
    {
        None = 0,
        Vertical = 1,
        Horizontal = 2,
        Both = 3
    }

    public class VirtualKeyboardNavigator : MonoBehaviour
    {
        public ISimpleEvent<string> OnKeySubmitted => _onKeySubmitted;
        [SerializeField] private List<NavigableItem> _navigableItems = new List<NavigableItem>();
        [SerializeField] private bool _useConcreteSelection = false;
        [SerializeField] private NavigableItem _selectedItem;

        [SerializeField] private NavigationType _navigationType = NavigationType.Both;
        [SerializeField] private int _itemsInRow = 0;
        [SerializeField] private KeyCode _keyForButtonSubmitJoystick = KeyCode.JoystickButton0;
        [SerializeField] private KeyCode _keyForButtonSubmitKeyboard = KeyCode.None;

        private SimpleEvent<string> _onKeySubmitted = new SimpleEvent<string>();

        private int _currentIndex;
        private bool _isHorizontalAxisInUse = false;
        private bool _isVerticalAxisInUse = false;
        private bool _isActive = false;
        private bool _isFocused = false;

        public void Activate()
        {
            _isActive = true;
            _isFocused = true;
            _currentIndex = 0;
            if (_useConcreteSelection && _selectedItem)
            {
                _currentIndex = _navigableItems.IndexOf(_selectedItem);
            }

            SelectItem(_navigableItems[_currentIndex]);
        }

        public void Deactivate()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(Deactivate)}] OK");
            _isActive = false;
            _isFocused = false;
            DeselectItem(_navigableItems[_currentIndex]);
            _currentIndex = 0;
        }

        public void SetFocus(bool isFocused)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SetFocus)}] OK, isFocused: {isFocused}");
            _isFocused = isFocused;
        }

        public void DeselectItem()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(DeselectItem)}] OK");
            DeselectItem(_navigableItems[_currentIndex]);
        }

        [ContextMenu("Activate")]
        private void TestActivate()
        {
            Activate();
        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_isActive == false || _isFocused == false) return;

            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");

            if (horizontal > 0)
            {
                if (_isHorizontalAxisInUse == false)
                {
                    _isHorizontalAxisInUse = true;
                    MoveRight();
                }
            }
            else if (horizontal < 0)
            {
                if (_isHorizontalAxisInUse == false)
                {
                    _isHorizontalAxisInUse = true;
                    MoveLeft();
                }
            }
            else if (horizontal == 0)
            {
                _isHorizontalAxisInUse = false;
            }

            if (vertical > 0)
            {
                if (_isVerticalAxisInUse == false)
                {
                    _isVerticalAxisInUse = true;
                    MoveUp();
                }
            }
            else if (vertical < 0)
            {
                if (_isVerticalAxisInUse == false)
                {
                    _isVerticalAxisInUse = true;
                    MoveDown();
                }
            }
            else if (vertical == 0)
            {
                _isVerticalAxisInUse = false;
            }

            if (Input.GetKeyDown(_keyForButtonSubmitKeyboard) || Input.GetKeyDown(_keyForButtonSubmitJoystick))
            {
                ExecuteItem();
            }
        }

        private void ExecuteItem()
        {
            _navigableItems[_currentIndex].Execute();
        }

        private void SelectItem(NavigableItem item)
        {
            item.Select();
        }

        private void DeselectItem(NavigableItem item)
        {
            item.Deselect();
        }

        private void MoveLeft()
        {
            if (_navigationType != NavigationType.Horizontal && _navigationType != NavigationType.Both) return;
            if (_currentIndex <= 0) return;

            if (_navigableItems[_currentIndex - 1].isActiveAndEnabled == false) return;

            var previousItem = _navigableItems[_currentIndex];
            DeselectItem(previousItem);

            _currentIndex--;
            var nextItem = _navigableItems[_currentIndex];
            SelectItem(nextItem);
        }

        private void MoveRight()
        {
            if (_navigationType != NavigationType.Horizontal && _navigationType != NavigationType.Both) return;
            if (_currentIndex >= _navigableItems.Count - 1) return;

            if (_navigableItems[_currentIndex + 1].isActiveAndEnabled == false) return;

            var previousItem = _navigableItems[_currentIndex];
            DeselectItem(previousItem);

            _currentIndex++;

            var nextItem = _navigableItems[_currentIndex];
            SelectItem(nextItem);
        }

        private void MoveUp()
        {
            if (_navigationType != NavigationType.Vertical && _navigationType != NavigationType.Both) return;
            if (_currentIndex <= 0) return;

            if (_navigationType == NavigationType.Both)
            {
                var previousItem = _navigableItems[_currentIndex];
                DeselectItem(previousItem);

                _currentIndex -= _itemsInRow;

                if (_currentIndex < 0)
                {
                    _currentIndex = 0;
                }

                var nextItem = _navigableItems[_currentIndex];
                SelectItem(nextItem);
            }
            else
            {
                var previousItem = _navigableItems[_currentIndex];
                DeselectItem(previousItem);
                _currentIndex--;
                var nextItem = _navigableItems[_currentIndex];
                SelectItem(nextItem);
            }
        }

        private void MoveDown()
        {
            if (_navigationType != NavigationType.Vertical && _navigationType != NavigationType.Both) return;
            if (_currentIndex >= _navigableItems.Count - 1) return;

            if (_navigationType == NavigationType.Both)
            {
                var previousItem = _navigableItems[_currentIndex];
                DeselectItem(previousItem);

                _currentIndex += _itemsInRow;

                if (_currentIndex > _navigableItems.Count - 1)
                {
                    _currentIndex = _navigableItems.Count - 1;
                }

                var nextItem = _navigableItems[_currentIndex];
                SelectItem(nextItem);
            }
            else
            {
                var previousItem = _navigableItems[_currentIndex];
                DeselectItem(previousItem);
                _currentIndex++;
                var nextItem = _navigableItems[_currentIndex];
                SelectItem(nextItem);
            }
        }

        private void SubscribeToEvents()
        {
            foreach (var keyItem in _navigableItems.Select(navigableItem => navigableItem as KeyButtonItem))
            {
                keyItem?.KeySelectedEvent.AddListener(OnKeySubmittedHandler);
            }
        }

        private void UnsubscribeFromEvents()
        {
            foreach (var keyItem in _navigableItems.Select(navigableItem => navigableItem as KeyButtonItem))
            {
                keyItem?.KeySelectedEvent.RemoveListener(OnKeySubmittedHandler);
            }
        }

        private void OnKeySubmittedHandler(string key)
        {
            _onKeySubmitted?.Notify(key);
        }
    }
}