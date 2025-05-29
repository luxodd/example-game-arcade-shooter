using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.UseCases
{
    [CreateAssetMenu(menuName = "Create/Helpers/Transaction Error Messages Descriptor",
        fileName = "TransactionErrorMessagesDescriptor", order = 0)]
    public class TransactionErrorMessagesDescriptor : ScriptableObject
    {
        [SerializeField] private List<TransactionErrorMessage> _messages = new List<TransactionErrorMessage>();

        public string GetTransactionErrorMessage(int attemptsLeft)
        {
            var result = _messages.Find(message => message.AttemptsLeft == attemptsLeft);
            return result?.Message;
        }
    }

    [Serializable]
    public class TransactionErrorMessage
    {
        [field: SerializeField] public int AttemptsLeft { get; private set; }
        [field: SerializeField] public string Message { get; private set; }
    }
}