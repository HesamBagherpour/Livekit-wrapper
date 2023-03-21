using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiveKit;
using Packages.GeneralUtility.General.RunTime;
using UnityEngine;

namespace Runtime.StreamKit
{
    public class StreamKitManager : MonoSingleton<StreamKitManager>
    {
        
        #region Public Variable

        public bool activeLog = true;
        public string serverStreamUrl = "wss://wrapper-15qg6rue.livekit.cloud";
        public Room currentRoom;

        private string _currentToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2Nzk0Mjg1MTcsImlzcyI6IkFQSUFMbVo3a1VkRDJ6biIsIm5iZiI6MTY3OTQxOTUxNywic3ViIjoiaGVzYW0iLCJ2aWRlbyI6eyJjYW5QdWJsaXNoIjp0cnVlLCJjYW5QdWJsaXNoRGF0YSI6dHJ1ZSwiY2FuU3Vic2NyaWJlIjp0cnVlLCJyb29tIjoiSEIxIiwicm9vbUpvaW4iOnRydWV9fQ.pYnXgZmnOZrzsS0WJVlv-Ce4XDU3ZVOQrS9W5SiVwow";
        public Action<ConnectionState> onChangeConnectionState;
        public Action onConnected;
        public Action onDisConnected;

        public string videoMediaDeviceId;
        public LocalVideoTrack localVideoTrack;
        public string audioMediaDeviceId;
        public LocalAudioTrack localAudioTrack;
        public string screenMediaDeviceId;

        public MediaDeviceSelectUi mediaDeviceSelectUi;

        private Dictionary<Track, HTMLMediaElement> attachDic = new Dictionary<Track, HTMLMediaElement>();
        private List<Track> attachTrackLists = new List<Track>();

        #endregion

        #region Private Variable

        private ConnectOperation _connectOperation;

        #endregion

        #region Initialize

        private async Task OnEnable()
        {
            var _CreateRoom =  await CreateRoom();
            if(_CreateRoom == false)
                return;
            
            var _ConnectRoom =  await ConnectRoom(_currentToken);
            
            if(_ConnectRoom == false)
                return;
        
        }

        public UniTask<bool> CreateRoom(RoomOptions? options = null)
        {
            Log(() => Debug.unityLogger.Log($"StreamKit | CreateRoom | Start"));

            if (currentRoom != null)
            {
                Log(() => Debug.LogWarning($"StreamKit | CreateRoom | currentRoom != null"));
                return new UniTask<bool>(false);
            }

            currentRoom = new Room(options);
            if (currentRoom == null)
            {
                Log(() => Debug.LogWarning($"StreamKit | CreateRoom | can not create room"));
                return new UniTask<bool>(false);
            }

            Log(() => Debug.unityLogger.Log($"StreamKit | CreateRoom | end"));

            currentRoom.StateChanged += state =>
            {
                onChangeConnectionState?.Invoke(state);
                if (state == ConnectionState.Connected)
                    onConnected?.Invoke();
                if (state == ConnectionState.Disconnected)
                    onDisConnected?.Invoke();
            };

            return new UniTask<bool>(true);
        }

        public async UniTask<bool> ConnectRoom(string token, RoomConnectOptions? options = null)
        {
            Log(() => Debug.unityLogger.Log($"StreamKit | ConnectRoom | start"));
            Log(() => Debug.unityLogger.Log($"StreamKit | ConnectRoom | token: {token}"));
            _currentToken = token;

            if (currentRoom == null)
            {
                Log(() => Debug.LogWarning($"StreamKit | ConnectRoom | currentRoom == null"));
                return false;
            }

            if (currentRoom.State != ConnectionState.Disconnected)
            {
                Log(() => Debug.LogWarning($"StreamKit | ConnectRoom | currentRoom.State != ConnectionState.Disconnected state: {currentRoom.State.ToString()}"));
                return false;
            }

            try
            {
                await connectRoom(serverStreamUrl, _currentToken, options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            

            if (_connectOperation == null || _connectOperation.IsError)
            {
                Log(() => Debug.LogWarning($"StreamKit | ConnectRoom | _connectOperation null or isError"));
                return false;
            }

            Log(() => Debug.unityLogger.Log($"StreamKit | ConnectRoom | end"));

            return true;
        }

        private IEnumerator connectRoom(string url, string token, RoomConnectOptions? options = null)
        {
            Log(() => Debug.unityLogger.Log($"StreamKit | connectRoom | IEnumerator start"));
            _connectOperation = currentRoom.Connect(url, token, options);
            yield return _connectOperation;
        }

        #endregion

        public IEnumerator PublishLocalTrack(LocalTrack track)
        {
            yield return currentRoom.LocalParticipant.PublishTrack(track);
        }
        public IEnumerator UnPublishLocalTrack(LocalTrack track)
        {
            yield return currentRoom.LocalParticipant.UnpublishTrack(track);
        }
        public IEnumerator MuteLocalTrack(LocalTrack track)
        {
            yield return track.Mute();
        }
        public IEnumerator UnMuteLocalTrack(LocalTrack track)
        {
            yield return track.Unmute();
        }

        public IEnumerator TurnOnOffCamera(bool state)
        {
            yield return currentRoom.LocalParticipant.SetCameraEnabled(state);
        }
        public IEnumerator TurnOnOffMicrophone(bool state)
        {
            yield return currentRoom.LocalParticipant.SetMicrophoneEnabled(state);
        }
        public IEnumerator TurnOnOffScreenShare(bool state)
        {
            yield return currentRoom.LocalParticipant.SetScreenShareEnabled(state);
        }

        public HTMLMediaElement AttachTrack(Track track)
        {
            if (track == null)
                return null;

            attachTrackLists.Add(track);

            if (attachDic.ContainsKey(track))
            {
                return attachDic[track];
            }

            HTMLMediaElement media = track?.Attach();
            attachDic.Add(track, media);
            return media;
        }

        public void DeAttachTrack(Track track)
        {
            if (track != null)
            {
                if (attachTrackLists.Contains(track))
                    attachTrackLists.Remove(track);
                if (!attachTrackLists.Contains(track))
                {
                    track?.Detach();
                    if (attachDic.ContainsKey(track))
                        attachDic.Remove(track);
                }
            }
        }

        public async UniTask ShowDeviceSelectUi()
        {
            mediaDeviceSelectUi.gameObject.SetActive(true);
            await UniTask.WaitUntil(() => !mediaDeviceSelectUi.gameObject.activeInHierarchy);
        }

        private void Log(Action logFunc)
        {
            if (!activeLog)
                return;
            logFunc?.Invoke();
        }
    }
}