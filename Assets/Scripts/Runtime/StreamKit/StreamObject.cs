using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Infinite8.MetaRoom.StreamKit;
using LiveKit;
using UnityEngine;

namespace Runtime.StreamKit
{
    public class StreamObject : MonoBehaviour
    {
        private static readonly Dictionary<string, StreamObject> StreamMap = new Dictionary<string, StreamObject>();
        public static StreamObject GetStreamObject(string identity)
        {
            if (identity == null) return null;
            if (StreamMap.ContainsKey(identity)) return StreamMap[identity];
            Debug.unityLogger.LogError(nameof(StreamObject), "Identity " + identity + " were not found in StreamMap");
            return null;
        }

        private Participant _participant;
        private readonly HashSet<Monitor> _monitors = new HashSet<Monitor>();
        public string identity;
        public bool isLocal;


        public async UniTask Init(string identity, bool isLocal)
        {
            Debug.unityLogger.Log("StreamObject | Init | start");
            Debug.unityLogger.Log($"StreamObject | Init | start identity: {identity} - isLocal: {isLocal}");
            if (identity == null && String.IsNullOrEmpty(this.identity))
                return;

            Debug.unityLogger.Log("StreamObject | Init | after identity == null && String.IsNullOrEmpty(this.identity)");

            if (this.identity != identity)
            {
                if (StreamMap.ContainsKey(this.identity))
                {
                    StreamMap.Remove(this.identity);
                }

                if (identity != null) StreamMap.TryAdd(identity, this);
            }
            this.identity = identity;
            this.isLocal = isLocal;

            // monitor.gameObject.SetActive(true);

            if (StreamKitManager.Instance.currentRoom == null)
                await UniTask.WaitUntil(() => StreamKitManager.Instance.currentRoom != null);
            if (this.identity == null)
            {
                Debug.unityLogger.Log("StreamObject | Init | this.identity == null");
                _participant = null;
                // monitor.UpdateVideoTrack(null);
                DoForMonitors(m => m.UpdateVideoTrack(null));
                registerHandlers(false);

                StreamKitManager.Instance.onDisConnected -= onDisConnected;
                StreamKitManager.Instance.onConnected -= onConnected;

                return;
            }
            else
            {
                Debug.unityLogger.Log($"StreamObject | Init | this.identity != null this.identity: {this.identity}");
                if (StreamKitManager.Instance.currentRoom != null && StreamKitManager.Instance.currentRoom.State == ConnectionState.Connected)
                {
                    onConnected();
                }

                StreamKitManager.Instance.onDisConnected += onDisConnected;
                StreamKitManager.Instance.onConnected += onConnected;
            }


            if (!isLocal)
                updateParticipant();
            else
            {
                InitParticipant(StreamKitManager.Instance.currentRoom.LocalParticipant);
            }

            Debug.unityLogger.Log("StreamObject | Init | end");
        }

        private void onConnected()
        {
            Debug.unityLogger.Log("StreamObject | onConnected | start");
            registerHandlers(true);
        }

        private void onDisConnected()
        {
            Debug.unityLogger.Log("StreamObject | onDisConnected | start");
            registerHandlers(false);
        }

        private void updateParticipant()
        {
            Debug.unityLogger.Log("StreamObject | updateParticipant | start");
            if (_participant == null)
                foreach (var currentRoomParticipant in StreamKitManager.Instance.currentRoom.Participants)
                {
                    if (currentRoomParticipant.Value.Identity == identity)
                    {
                        InitParticipant(currentRoomParticipant.Value);
                    }
                }
            else
            {
                Debug.unityLogger.Log("StreamObject | updateParticipant | _participant != null");
            }
        }

        protected virtual void registerHandlers(bool state)
        {
            Debug.unityLogger.Log($"StreamObject | updateParticipant | registerHandlers {state}");
            if (state)
            {
                StreamKitManager.Instance.currentRoom.ParticipantConnected += onParticipantConnected;
                // StreamKitManager.Instance.currentRoom.ParticipantDisconnected += onParticipantDisconnected;
                StreamKitManager.Instance.currentRoom.TrackPublished += onTrackPublished;
                StreamKitManager.Instance.currentRoom.LocalTrackPublished += onTrackPublished;
                // StreamKitManager.Instance.currentRoom.TrackUnpublished += onTrackUnpublished;
                // StreamKitManager.Instance.currentRoom.LocalTrackUnpublished += onTrackUnpublished;
            }
            else
            {
                StreamKitManager.Instance.currentRoom.ParticipantConnected -= onParticipantConnected;
                StreamKitManager.Instance.currentRoom.TrackPublished -= onTrackPublished;
                StreamKitManager.Instance.currentRoom.LocalTrackPublished -= onTrackPublished;
                // StreamKitManager.Instance.currentRoom.TrackUnpublished -= onTrackUnpublished;
                // StreamKitManager.Instance.currentRoom.LocalTrackUnpublished -= onTrackUnpublished;
            }
        }
        private void onParticipantDisconnected(RemoteParticipant participant)
        {
            // throw new NotImplementedException();
        }

        private void onTrackUnpublished(TrackPublication publication, Participant participant)
        {
            Debug.unityLogger.Log($"StreamObject | onTrackUnpublished | start participant.Identity: {participant.Identity} - me Identity: {identity}");
            if (participant.Identity == identity && !String.IsNullOrEmpty(identity))
            {
                Debug.unityLogger.Log("StreamObject | onTrackUnpublished | participant.Identity == identity");
                InitParticipant(participant);
            }
        }

        private void onTrackPublished(TrackPublication publication, Participant participant)
        {
            Debug.unityLogger.Log("StreamObject | onTrackPublished | start");
            Debug.unityLogger.Log($"StreamObject | onTrackPublished | start participant.Identity: {participant.Identity} - me Identity: {identity}");
            if (participant.Identity == identity && !String.IsNullOrEmpty(identity))
            {
                Debug.unityLogger.Log("StreamObject | onTrackPublished | participant.Identity == identity");
                InitParticipant(participant);
            }
        }

        private void onParticipantConnected(RemoteParticipant participant)
        {
            Debug.unityLogger.Log("StreamObject | onParticipantConnected | start");
            Debug.unityLogger.Log($"StreamObject | onParticipantConnected | start participant.Identity: {participant.Identity} - me Identity: {identity}");
            if (participant.Identity == identity && !String.IsNullOrEmpty(identity))
            {
                Debug.unityLogger.Log("StreamObject | onParticipantConnected | participant.Identity == identity");
                InitParticipant(participant);
            }
        }

        private void InitParticipant(Participant participant)
        {
            Debug.unityLogger.Log($"StreamObject | InitParticipant | start participant.Identity: {participant.Identity} - me Identity: {identity}");
            Debug.unityLogger.Log("StreamObject | InitParticipant | start");
            Debug.unityLogger.Log("Initializing Participant | " + participant.Identity);
            _participant = participant;
            UpdateTracks();
        }

        private void UpdateTracks()
        {
            if (_participant == null) return;
            Debug.unityLogger.Log($" StreamObject | UpdateTracks  |  _participant.Identity: { _participant.Identity}");
            Debug.unityLogger.Log("StreamObject | UpdateTracks | start");
            if (_participant.VideoTracks.Count >= 1)
            {
                Debug.unityLogger.Log($"StreamObject | UpdateTracks | v _participant.VideoTracks.Count >= 1 Count: {_participant.VideoTracks.Count}");

                // var pub = _participant.VideoTracks.Select(pair => pair.Value.IsEnabled && !pair.Value.IsMuted).First()

                try
                {
                    var pub = _participant.VideoTracks.First();
                    var trackV = pub.Value.Track;
                    if (trackV != null)
                    {
                        Debug.unityLogger.Log("StreamObject | UpdateTracks | trackV != null");
                        DoForMonitors(m => m.UpdateVideoTrack(trackV));
                    }
                }
                catch (Exception e)
                {
                    Debug.unityLogger.LogWarning("StreamObject | UpdateTracks | trackV != null Exception ", e);
                    Debug.unityLogger.LogException(e);
                }
            }
            else
            {
                Debug.unityLogger.Log("StreamObject | UpdateTracks | v _participant.VideoTracks.Count 0");
                DoForMonitors(m => m.UpdateVideoTrack(null));
            }

            if (_participant.AudioTracks.Count >= 1)
            {
                Debug.unityLogger.Log("StreamObject | UpdateTracks | a _participant.VideoTracks.Count >= 1");
                var pub = _participant.AudioTracks.First();
                var trackA = pub.Value.Track as RemoteAudioTrack;
                if (trackA != null)
                {
                    Debug.unityLogger.Log("StreamObject | UpdateTracks | trackA != null");
                    DoForMonitors(m => m.UpdateAudioTrack(trackA));
                }
            }
            else
            {
                Debug.unityLogger.Log("StreamObject | UpdateTracks | a _participant.VideoTracks.Count 0");
                DoForMonitors(m => m.UpdateAudioTrack(null));
            }
        }

        private void DoForMonitors(Action<Monitor> action)
        {
            Debug.unityLogger.Log("StreamObject | DoForMirrors | start");
            if (_monitors == null)
                return;
            foreach (var monitor in _monitors)
            {
                action?.Invoke(monitor);
            }
        }

        public void PushMonitor(Monitor monitor)
        {
            _monitors.Add(monitor);
            UpdateTracks();
// #if UNITY_EDITOR
//             monitor.gameObject.SetActive(true);
// #endif
        }

        public void PopMonitor(Monitor monitor)
        {
            _monitors.Remove(monitor);
            monitor.UpdateVideoTrack(null);
            monitor.UpdateAudioTrack(null);
            UpdateTracks();
// #if UNITY_EDITOR
//             monitor.gameObject.SetActive(false);
// #endif
        }
    }
}