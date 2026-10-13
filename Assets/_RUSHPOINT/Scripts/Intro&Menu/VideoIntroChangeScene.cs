
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoIntro : MonoBehaviour
{
    private VideoPlayer _videoPlayer;
    private bool _sceneChanging = false;

    void Start()
    {
        _videoPlayer = GetComponent<VideoPlayer>();

        if (_videoPlayer == null)
        {
            return;
        }

        _videoPlayer.loopPointReached += NextScene;
    }

    void NextScene(VideoPlayer vp)
    {
        if (_sceneChanging)
            return;

        _sceneChanging = true;

        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.loopPointReached -= NextScene;
    }
}
