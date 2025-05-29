using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.UseCases
{
    [CreateAssetMenu(menuName = "Create/Helpers/Pin Code Messages Descriptor", fileName = "PinCodeMessagesDescriptor",
        order = 0)]
    public class PinCodeMessagesDescriptor : ScriptableObject
    {
        [SerializeField] private List<PinCodeMessage> _pinCodeMessages = new List<PinCodeMessage>();

        public string GetPinCodeMessage(int attemptsLeft)
        {
            var result = _pinCodeMessages.Find(pinCodeMessage => pinCodeMessage.AttemptsLeft == attemptsLeft);
            return result?.Message;
        }
    }

    [Serializable]
    public class PinCodeMessage
    {
        [field: SerializeField] public int AttemptsLeft { get; private set; }
        [field: SerializeField] public string Message { get; private set; }
    }
}