using System;
using System.Collections.Generic;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Storage
{
    public interface IStorable
    {
        void Save(Dictionary<string, object> data);
        void Load(Dictionary<string, object> data);
        void Clear();
    }

    public interface IDataHolder
    {
        Dictionary<string, object> GetData();
    }

    public interface IStorageService: IDataHolder
    {
        void Register(IStorable storable);
        void Save(Action onLoaded = null, Action<int, string> onFailed = null);
        void Load(Action onLoaded = null, Action<int, string> onFailed = null);
        
        void ClearStorage();
    }
    
    public class StorageService : MonoBehaviour, IStorageService
    {
        [SerializeField] private WebSocketCommandHandler _webSocketCommandHandler;
        
        private readonly List<IStorable> _storables = new List<IStorable>();
        private Dictionary<string, object> _dataHolderForSave = new Dictionary<string, object>();
        private Dictionary<string, object> _dataHolderForLoad = new Dictionary<string, object>();
        
        private Action _onSuccess;
        private Action<int, string> _onFailed;
        
        public void Register(IStorable storable)
        {
            _storables.Add(storable);
        }

        public void Save(Action onSuccess = null, Action<int, string> onFailed = null)
        {
            _onSuccess = onSuccess;
            _onFailed = onFailed;
            var data = GetData();
            _webSocketCommandHandler.SendSetUserDataRequestCommand(data, OnSaveGameRequestSuccess, OnSaveGameRequestFail);
        }

        public void Load(Action onSuccess = null, Action<int, string> onFailed = null)
        {
            _onSuccess = onSuccess;
            _onFailed = onFailed;
            _webSocketCommandHandler.SendGetUserDataRequestCommand(OnLoadGameRequestSuccess, OnLoadGameRequestFail);   
        }

        public void ClearStorage()
        {
            _dataHolderForSave.Clear();
            _dataHolderForLoad.Clear();
            foreach (var storable in _storables)
            {
                storable.Clear();   
            }
            
            Save();
        }

        private void OnSaveGameRequestSuccess()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnSaveGameRequestSuccess)}] OK");
            _onSuccess?.Invoke();
            _onSuccess = null;
        }

        private void OnSaveGameRequestFail(int code, string message)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnSaveGameRequestFail)}] OK, code: {code}, message: {message}");
            _onFailed?.Invoke(code, message);
            _onFailed = null;
        }

        private void OnLoadGameRequestSuccess(object data)
        {
            if (data == null) return;
            
            var userDataPayload = data as UserDataPayload;
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnLoadGameRequestSuccess)}] OK, data: {data}, user Data Payload: {userDataPayload.Data}");
            
            var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(userDataPayload.Data.ToString());
            var userDataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson["user_data"].ToString());;
            _dataHolderForLoad = userDataJson;
            _dataHolderForSave = userDataJson;
            foreach (var storable in _storables)
            {
                storable.Load(_dataHolderForLoad);
            }
            _onSuccess?.Invoke();
            _onSuccess = null;
        }

        private void OnLoadGameRequestFail(int code, string message)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLoadGameRequestFail)}] OK, code: {code}, message: {message}");
            _onFailed?.Invoke(code, message);
            _onFailed = null;
        }

        public Dictionary<string, object> GetData()
        {
            foreach (var storable in _storables)
            {
                storable.Save(_dataHolderForSave);
            }
            
            return _dataHolderForSave;
        }
    }
}
