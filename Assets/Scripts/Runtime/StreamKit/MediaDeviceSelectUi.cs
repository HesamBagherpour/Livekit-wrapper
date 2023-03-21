using System;
using System.Collections;
using System.Runtime.InteropServices;
using Infinite8.MetaRoom.StreamKit;
using LiveKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.StreamKit
{
    public class MediaDeviceSelectUi : MonoBehaviour
    {
        public bool activeLog = true;

        public TMP_Dropdown videoDropdown;
        public TMP_Dropdown audioDropdown;
        public Monitor previewMonitor;

        private JSArray<MediaDeviceInfo> _videoDevices;
        private JSArray<MediaDeviceInfo> _audioDevices;
        private JSArray<MediaDeviceInfo> _audioScreenDevices;

        private static LocalVideoTrack _localVideoTrack;
        private static LocalAudioTrack _localAudioTrack;
        private static LocalTrack _localScreenTrack;

        public Button btnOK;

        [DllImport("__Internal")]
        private static extern string GetURLFromPage();

        private void Start()
        {
            Debug.Log("MediaDeviceSelectUi | Start | StartCoroutine");
            btnOK.onClick.AddListener(() => { StartCoroutine(OnClickOK()); });
        }
        private void OnEnable()
        {
            Debug.Log("MediaDeviceSelectUi | OnEnable | Init");
            Init();
           
        }

        private void OnDisable()
        {
            Debug.Log("MediaDeviceSelectUi | OnEnable | Init");
            previewMonitor.UpdateVideoTrack(null);
            previewMonitor.UpdateAudioTrack(null);
        }

        public void Init()
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | Init | start"));
            InitDropDowns();
        }

        public IEnumerator OnClickOK()
        {
            if (StreamKitManager.Instance.localVideoTrack != null)
                yield return StreamKitManager.Instance.PublishLocalTrack(StreamKitManager.Instance.localVideoTrack);
            if (StreamKitManager.Instance.localAudioTrack != null)
                yield return StreamKitManager.Instance.PublishLocalTrack(StreamKitManager.Instance.localAudioTrack);

            
            gameObject.SetActive(false);
            yield return null;
        }


        public void InitDropDowns()
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | InitDropDowns | start"));
            StartCoroutine(InitVideoDropDown());
            StartCoroutine(InitAudioDropDown());
        }

        public IEnumerator InitVideoDropDown()
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | InitVideoDropDown | start"));

            videoDropdown.ClearOptions();
            videoDropdown.options.Add(new TMP_Dropdown.OptionData()
            {
                text = "None"
            });

            var devicesOp = Room.GetLocalDevices(MediaDeviceKind.VideoInput);
            yield return devicesOp;

            if (devicesOp.IsError)
            {
                Log(() => Debug.Log("MediaDeviceSelectUi | InitVideoDropDown | IsError"));
                yield break;
            }

            _videoDevices = devicesOp.ResolveValue;
            foreach (var d in _videoDevices)
            {
                Log(() => Debug.Log($"MediaDeviceSelectUi | InitVideoDropDown | foreach _videoDevices  Label: {d.Label}"));
                videoDropdown.options.Add(new TMP_Dropdown.OptionData()
                {
                    text = d.Label
                });
            }

            videoDropdown.onValueChanged.AddListener((value) =>
            {
                btnOK.interactable = false;

                Log(() => Debug.Log($"MediaDeviceSelectUi | InitVideoDropDown | onValueChanged value: {value}"));
                if (value > 0)
                    StartCoroutine(SetDeviceId(_videoDevices[value - 1].DeviceId));
                else
                    StartCoroutine(SetDeviceId("None"));
            });

            if (_videoDevices.Count >= 1)
            {
                // StartCoroutine(SetDeviceId(_videoDevices[0].DeviceId));
                videoDropdown.value = 1;
                videoDropdown.RefreshShownValue();
            }
        }

        public IEnumerator InitAudioDropDown()
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | InitAudioDropDown | start"));

            audioDropdown.ClearOptions();
            audioDropdown.options.Add(new TMP_Dropdown.OptionData()
            {
                text = "None"
            });

            var devicesOp = Room.GetLocalDevices(MediaDeviceKind.AudioInput);
            yield return devicesOp;

            if (devicesOp.IsError)
            {
                Log(() => Debug.Log("MediaDeviceSelectUi | InitAudioDropDown | IsError"));
                yield break;
            }

            _audioDevices = devicesOp.ResolveValue;
            foreach (var d in _audioDevices)
            {
                Log(() => Debug.Log($"MediaDeviceSelectUi | InitAudioDropDown | foreach _videoDevices  Label: {d.Label}"));
                audioDropdown.options.Add(new TMP_Dropdown.OptionData()
                {
                    text = d.Label
                });
            }

            audioDropdown.onValueChanged.AddListener((value) =>
            {
                btnOK.interactable = false;
                if (value > 0)
                    StartCoroutine(SetDeviceId(null, _audioDevices[value - 1].DeviceId));
                else
                    StartCoroutine(SetDeviceId(null, "None"));
            });

            if (_audioDevices.Count >= 1)
            {
                // StartCoroutine(SetDeviceId(null, _audioDevices[0].DeviceId));
                audioDropdown.value = 1;
                audioDropdown.RefreshShownValue();
            }
        }

        private IEnumerator SetDeviceId(string videoDeviceId, string audioDeviceId = null)
        {
            
            // if (!String.IsNullOrEmpty(QueryParams.Get("videoDeviceId")))
            // {
            //     videoDeviceId = QueryParams.Get("videoDeviceId");
            // }
            //
            // if (!String.IsNullOrEmpty(QueryParams.Get("audioDeviceId")))
            // {
            //     audioDeviceId = QueryParams.Get("audioDeviceId");
            // }
            //
            
            Log(() => Debug.Log("MediaDeviceSelectUi | SetDeviceId | start"));
            if (videoDeviceId != null)
                yield return StartCoroutine(SetVideoDeviceIdHandler(videoDeviceId));
            if (audioDeviceId != null)
                yield return StartCoroutine(SetAudioDeviceIdHandler(audioDeviceId));

            btnOK.interactable = true;

            // if (!String.IsNullOrEmpty(QueryParams.Get("autoDeviceSelect")))
            // {
            //     yield return StartCoroutine(OnClickOK());
            // }
        }


        public IEnumerator SetVideoDeviceIdHandler(string deviceId)
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | SetVideoDeviceIdHandler | start"));
            // previewMonitor.UpdateVideoTrack(null);

            if (deviceId == "None")
            {
                previewMonitor.UpdateVideoTrack(null);
                StreamKitManager.Instance.videoMediaDeviceId = null;
                StreamKitManager.Instance.localVideoTrack = null;
                yield break;
            }

            var f = Client.CreateLocalVideoTrack(new VideoCaptureOptions()
            {
                DeviceId = deviceId
            });
            yield return f;


            if (f.IsError)
            {
                Log(() => Debug.Log("MediaDeviceSelectUi | SetVideoDeviceIdHandler | IsError"));
                yield break;
            }

            previewMonitor.UpdateVideoTrack(f.ResolveValue);

            //TODO set value to livekit
            StreamKitManager.Instance.videoMediaDeviceId = deviceId;
            StreamKitManager.Instance.localVideoTrack = f.ResolveValue;
        }

        public IEnumerator SetAudioDeviceIdHandler(string deviceId)
        {
            Log(() => Debug.Log("MediaDeviceSelectUi | SetAudioDeviceIdHandler | start"));
            // previewMonitor.UpdateAudioTrack(null);

            if (deviceId == "None")
            {
                previewMonitor.UpdateAudioTrack(null);
                StreamKitManager.Instance.audioMediaDeviceId = null;
                StreamKitManager.Instance.localAudioTrack = null;
                yield break;
            }

            var f = Client.CreateLocalAudioTrack(new AudioCaptureOptions()
            {
                DeviceId = deviceId
            });
            yield return f;


            if (f.IsError)
            {
                Log(() => Debug.Log("MediaDeviceSelectUi | SetAudioDeviceIdHandler | IsError"));
                yield break;
            }

            // previewMonitor.UpdateAudioTrack(f.ResolveValue);

            //TODO set value to livekit
            StreamKitManager.Instance.audioMediaDeviceId = deviceId;
            StreamKitManager.Instance.localAudioTrack = f.ResolveValue;
        }


        private void Log(Action logFunc)
        {
            if (!activeLog)
                return;
            logFunc?.Invoke();
        }
    }
}