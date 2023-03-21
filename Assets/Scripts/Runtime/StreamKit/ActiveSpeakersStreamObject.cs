using System;
using Cysharp.Threading.Tasks;
using LiveKit;
using UnityEngine;

namespace Runtime.StreamKit
{
    public class ActiveSpeakersStreamObject : StreamObject
    {
        public int ActiveIndex = 0;
        public bool lockSpeaker = false;

        private async void Start()
        {
            if (StreamKitManager.Instance.currentRoom != null && StreamKitManager.Instance.currentRoom.State == ConnectionState.Connected)
            {
                registerHandlersOver(true);
            }
            else
            {
                if (StreamKitManager.Instance.currentRoom == null)
                    await UniTask.WaitUntil(() => StreamKitManager.Instance.currentRoom != null);
                registerHandlersOver(true);
            }
        }

        protected override void registerHandlers(bool state)
        {
            Debug.unityLogger.Log("ActiveSpeakersStreamObject | registerHandlers | start");
            base.registerHandlers(state);
            registerHandlersOver(state);
        }

        protected void registerHandlersOver(bool state)
        {
            Debug.unityLogger.Log("ActiveSpeakersStreamObject | registerHandlers | start");
            if (state)
            {
                Debug.unityLogger.Log("ActiveSpeakersStreamObject | registerHandlers | state true");
                StreamKitManager.Instance.currentRoom.ActiveSpeakersChanged += CurrentRoomOnActiveSpeakersChanged;
            }
            else
            {
                Debug.unityLogger.Log("ActiveSpeakersStreamObject | registerHandlers | state false");
                StreamKitManager.Instance.currentRoom.ActiveSpeakersChanged -= CurrentRoomOnActiveSpeakersChanged;
            }
        }


        private async void CurrentRoomOnActiveSpeakersChanged(JSArray<Participant> speakers)
        {
            Debug.unityLogger.Log("ActiveSpeakersStreamObject | CurrentRoomOnActiveSpeakersChanged | start");
            if (lockSpeaker)
                return;

            if (speakers.Count <= ActiveIndex) return;

            lockSpeaker = true;

            var newIdentity = speakers[ActiveIndex].Identity;
            var isLocalT = false;
            Debug.unityLogger.Log($"ActiveSpeakersStreamObject | CurrentRoomOnActiveSpeakersChanged | isLocalT: {isLocalT}  - newIdentity: {newIdentity}");
            // NOTE we used to not await this initialization, in case of any problem just use .Forget()
            await Init(newIdentity, isLocalT);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
            lockSpeaker = false;
        }
    }
}