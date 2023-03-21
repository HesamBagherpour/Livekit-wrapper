using System;
using Cysharp.Threading.Tasks;
// using DG.Tweening;
using LiveKit;
using Runtime.StreamKit;
using UnityEngine;
using UnityEngine.UI;

namespace Infinite8.MetaRoom.StreamKit
{
    [System.Serializable]
    public class Monitor : MonoBehaviour
    {
        public int id;
        public bool isActive = true;

        public MonitorType monitorType;
        // public GameObject lcd;
        // public GameObject videoObject;

        public Participant participant;
        public Track videoTrack;
        public Track audioTrack;
        public HTMLVideoElement videoHtml;

        [Header("Default Type")] [Tooltip("just set texture of the material.")]
        public Renderer rendererC;

        public int materialIndex;
        public RawImage rawImage;
        public GameObject activeSpeakerObject;
        public float activeSpeakerActiveTimeSec = 0.8f;

        public string textureFieldName = "_MainTex";

        private RectTransform rawImageRect;
        private bool isTurnAnim = false;


        public async virtual void UpdateVideoTrack(Track track, bool autoPlay = true, bool adjustVideoObjectSize = true)
        {
            Debug.unityLogger.Log("Monitor | UpdateVideoTrack | start");

            if (track == videoTrack)
            {
                Debug.unityLogger.Log("Monitor | UpdateVideoTrack | track == null && videoTrack == null");
                // await TurnOff(true, false, true);
                return;
            }

            if (track == null && videoTrack != null)
            {
                Debug.unityLogger.Log("Monitor | UpdateVideoTrack | track == null && videoTrack != null");
                await TurnOff(true, false, true);
                videoTrack.Unmuted -= onUnmutedVideoTrack;
                videoTrack.Muted -= onMutedVideoTrack;
                videoTrack = track;
                return;
            }

            if (videoTrack != track)
            {
                Debug.unityLogger.Log("Monitor | UpdateVideoTrack | videoTrack != track");
                await TurnOff(true, false, false);
                videoTrack.Unmuted -= onUnmutedVideoTrack;
                videoTrack.Muted -= onMutedVideoTrack;
                videoTrack = track;
            }

            if (videoTrack.IsMuted)
            {
                videoTrack.Unmuted += onUnmutedVideoTrack;
                videoTrack.Muted += onMutedVideoTrack;
                await TurnOff(true, false, false);
                return;
            }

            if (autoPlay)
            {
                Debug.unityLogger.Log("Monitor | UpdateVideoTrack | autoPlay");
                await TurnOn(true, false, 1, adjustVideoObjectSize);
            }
        }

        protected virtual void VideoReceived(Texture2D tex)
        {
            if (rendererC != null)
                rendererC.materials[materialIndex].SetTexture(textureFieldName, tex);
            if (rawImage != null)
                rawImage.texture = tex;
        }

        public async virtual void UpdateAudioTrack(Track track, bool autoPlay = true, float volume = 1)
        {
            Debug.unityLogger.Log("Monitor | UpdateAudioTrack | start");
            
            
            if (track == null && videoTrack == null)
            {
                // await TurnOff(false, true, true);
                return;
            }
            
            if (track == null && audioTrack != null)
            {
                await TurnOff(false, true, true);
                return;
            }

            if (audioTrack != track)
            {
                await TurnOff(false, true, false);
                audioTrack = track;
            }

            if (autoPlay)
            {
                Debug.unityLogger.Log("Monitor | UpdateAudioTrack | autoPlay");
                await TurnOn(false, true, volume);
            }
        }

        public async virtual UniTask TurnOn(bool video, bool audio = false, float volume = 1, bool adjustVideoObjectSize = true)
        {
            // ChangeState(true);
            Debug.unityLogger.Log("Monitor | TurnOn | start");
            if (video)
            {
                videoHtml = StreamKitManager.Instance.AttachTrack(videoTrack) as HTMLVideoElement;
                if (videoHtml != null)
                {
                    Debug.unityLogger.Log("Monitor | TurnOn | videoHtml != null");
                    if (videoHtml.Texture != null)
                    {
                        VideoReceived(videoHtml.Texture);
                    }

                    videoHtml.VideoReceived += VideoReceived;
                    // if (adjustVideoObjectSize)
                    // AdjustVideoObjectSize();
                    videoTrack.Unmuted += onUnmutedVideoTrack;
                    videoTrack.Muted += onMutedVideoTrack;
                }

                Debug.unityLogger.Log("Monitor | TurnOn | video before await TurnAnim");
                await TurnAnim(true, true);
                Debug.unityLogger.Log("Monitor | TurnOn | video after await TurnAnim");
            }

            if (audio)
            {
                StreamKitManager.Instance.AttachTrack(audioTrack);
                SetVolume(volume);
            }
        }
        private void onMutedVideoTrack(Track track)
        {
            TurnOff(true, false, true);
        }

        private void onUnmutedVideoTrack(Track track)
        {
            TurnOn(true);
        }
        public async virtual UniTask TurnOff(bool video, bool audio, bool isTurnAnimTime)
        {
            Debug.unityLogger.Log("Monitor | TurnOff | start");
            if (StreamKitManager.Instance == null || StreamKitManager.Instance.currentRoom == null)
            {
                Debug.unityLogger.Log("Monitor | TurnOff | StreamKitManager.Instance == null || StreamKitManager.Instance.currentRoom == null");
                await TurnAnim(false, isTurnAnimTime);
                if (videoHtml != null)
                    videoHtml.VideoReceived -= VideoReceived;
                videoHtml = null;
                return;
            }

            if (video)
            {
                Debug.unityLogger.Log("Monitor | TurnOff | video");
                await TurnAnim(false, isTurnAnimTime);
                Debug.unityLogger.Log("Monitor | TurnOff | video after await TurnAnim");
                if (videoHtml != null)
                    videoHtml.VideoReceived -= VideoReceived;
                // videoTrack?.Detach();
                Debug.unityLogger.Log("Monitor | TurnOff | video after videoHtml != null");
                StreamKitManager.Instance.DeAttachTrack(videoTrack);
                Debug.unityLogger.Log("Monitor | TurnOff | video DeAttachTrack");
                videoHtml = null;
            }

            if (audio)
            {
                SetVolume(0); // TODO lerp
                // audioTrack?.Detach();
                StreamKitManager.Instance.DeAttachTrack(audioTrack);
            }
        }


        private async UniTask TurnAnim(bool state, bool isTurnAnimTime)
        {
            Debug.unityLogger.Log("Monitor | TurnAnim | start");
            // if (rawImage != null)
            // {
            //     Debug.unityLogger.Log("Monitor | TurnAnim | rawImage != null");
            //     if (rawImageRect == null)
            //     {
            //         Debug.unityLogger.Log("Monitor | TurnAnim | rawImageRect == nul");
            //         rawImageRect = rawImage.gameObject.GetComponent<RectTransform>();
            //     }

            //     var isVideoHtmlNull = videoHtml == null;

            //     if (isTurnAnim)
            //     {
            //         Debug.unityLogger.Log("Monitor | TurnAnim | isTurnAnim");
            //         await UniTask.WaitWhile(() => isTurnAnim);
            //     }

            //     if (state)
            //     {
            //         Debug.unityLogger.Log("Monitor | TurnAnim | state true");
            //         isTurnAnim = true;
            //         ChangeState(state);
            //         if (isTurnAnimTime)
            //         {
            //             Debug.unityLogger.Log("Monitor | TurnAnim | state true isTurnAnimTime");
            //             rawImageRect.DOScale(new Vector3(0f, 0f, 0f), 0);
            //             await rawImageRect.DOScale(new Vector3(1f, 0.01f, 1f), 0.7f).SetEase(Ease.InOutExpo).AsyncWaitForCompletion();
            //             rawImageRect.DOShakeScale(0.2f, 0.05f);
            //             await rawImageRect.DOScale(new Vector3(1f, 1f, 1f), 0.7f).SetEase(Ease.InOutExpo).AsyncWaitForCompletion();
            //         }
            //     }
            //     else
            //     {
            //         isTurnAnim = true;
            //         if (!isVideoHtmlNull && isTurnAnimTime)
            //         {
            //             Debug.unityLogger.Log("Monitor | TurnAnim | state false !isVideoHtmlNull && isTurnAnimTime");
            //             await rawImageRect.DOScale(new Vector3(1f, 0.01f, 1f), 0.7f).SetEase(Ease.InOutExpo).AsyncWaitForCompletion();
            //             rawImageRect.DOShakeScale(0.2f, 0.05f);
            //             await rawImageRect.DOScale(new Vector3(0f, 0f, 1f), 0.7f).SetEase(Ease.InOutExpo).AsyncWaitForCompletion();
            //         }

            //         ChangeState(state);
            //     }
            // }
            // else
            // {
            //     Debug.unityLogger.Log("Monitor | TurnAnim | if (rawImage != null) else");
            //     ChangeState(state);
            // }

            ChangeState(state);
            isTurnAnim = false;
            Debug.unityLogger.Log("Monitor | TurnAnim | end");
        }
        void ChangeState(bool state)
        {
            if (rawImage != null)
            {
                rawImage.gameObject.SetActive(state);
            }
            if (rendererC != null)
            {
                rendererC.gameObject.SetActive(state);
            }
        }
        public virtual void SetVolume(float volume)
        {
            if (audioTrack is RemoteAudioTrack track)
                track.SetVolume(volume);
        }

        public virtual void AdjustVideoObjectSize()
        {
            if (videoHtml.VideoWidth > videoHtml.VideoHeight)
            {
                // videoObject.transform.localScale = 
            }
        }

        public async virtual void UpdateActiveSpeaker()
        {
            if (activeSpeakerObject == null) return;

            activeSpeakerObject.SetActive(true);
            await UniTask.Delay(TimeSpan.FromSeconds(activeSpeakerActiveTimeSec));
            activeSpeakerObject.SetActive(true);
        }

        private void OnDestroy()
        {
            TurnOff(true, true, false);
        }
    }

    public enum MonitorType
    {
        Wall_Center,
        Wall_Left,
        Wall_Right,
        Chair,
        Player
    }
}