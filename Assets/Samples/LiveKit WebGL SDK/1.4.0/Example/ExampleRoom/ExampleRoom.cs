using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LiveKit;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ExampleRoom : MonoBehaviour
{
    private Dictionary<TrackPublication, RawImage> m_Videos = new Dictionary<TrackPublication, RawImage>();
    private Room m_Room;

    public GridLayoutGroup ViewContainer;
    public RawImage ViewPrefab;
    public Button DisconnectButton;

    IEnumerator Start()
    {
        // New Room must be called when WebGL assembly is loaded
        m_Room = new Room();
        
        // Setup the callbacks before connecting to the Room
        m_Room.ParticipantConnected += (p) =>
        {
            Debug.Log($"Participant connected: {p.Sid}");
        };

        m_Room.LocalTrackPublished += (publication, participant) => HandleAddedTrack(publication.Track, publication);
        m_Room.LocalTrackUnpublished += (publication, participant) => HandleRemovedTrack(publication.Track, publication);
        m_Room.TrackSubscribed += (track, publication, participant) => HandleAddedTrack(track, publication);
        m_Room.TrackUnsubscribed += (track, publication, participant) => HandleRemovedTrack(track, publication);
        
        var c = m_Room.Connect(JoinMenu.LivekitURL, JoinMenu.RoomToken);
        yield return c;
        
        if (c.IsError)
        {
            Debug.Log("Failed to connect to the room !");
            yield break;
        }
        
        Debug.Log("Connected to the room");

        DisconnectButton.onClick.AddListener(() =>
        {
            m_Room.Disconnect();
            SceneManager.LoadScene("JoinScene", LoadSceneMode.Single);
        });

        yield return m_Room.LocalParticipant.EnableCameraAndMicrophone();
    }

    private void HandleAddedTrack(Track track, TrackPublication publication)
    {
        Debug.Log("111111111111111111111111111111111111111111111111111111" + track.Kind);
        Debug.Log("111111111111111111111111111111111111111111111111111111" + publication.Track);
        if (track.Kind == TrackKind.Video)
        {
            if (ViewContainer.transform.childCount >= 6)
                return; // No space to show more than 6 tracks

            var video = track.Attach() as HTMLVideoElement;
            var newView = Instantiate(ViewPrefab, ViewContainer.transform);
            m_Videos.Add(publication, newView);

            video.VideoReceived += tex =>
            {
                newView.texture = tex;
            };
        }
        else if (track.Kind == TrackKind.Audio && publication is RemoteTrackPublication)
        {
            track.Attach();
        }
    }

    private void HandleRemovedTrack(Track track, TrackPublication publication)
    {
        
        Debug.Log("22222222222222222222222222222222222222222222222222222" + track.Kind);
        Debug.Log("22222222222222222222222222222222222222222222222222222222" + publication.Track);
        track.Detach();

        if (m_Videos.TryGetValue(publication, out var view))
            Destroy(view.gameObject);
    }
}
