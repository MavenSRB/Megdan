using UnityEngine;
using UnityEngine.Video;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>Owns the menu decoder and GPU target for the lobby lifetime.</summary>
    public sealed class MegdanMenuVideo : MonoBehaviour
    {
        private PrivateLobbyUnityView view;
        private VideoPlayer player;
        private RenderTexture target;
        private bool visible;
        public void Configure(PrivateLobbyUnityView menu)
        {
            view = menu;
            var clip = Resources.Load<VideoClip>("Private/Megdan/MainMenu/background");
            if (clip == null) return;
            target = new RenderTexture(1920, 1080, 0) { name = "Megdan Menu Video" };
            target.Create();
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.clip = clip;
            player.isLooping = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.prepareCompleted += Prepared;
            player.errorReceived += Failed;
            view.BackgroundVisibilityChanged += SetVisible;
            visible = true;
            player.Prepare();
        }
        private void Prepared(VideoPlayer source) { view.SetVideoBackground(target); if (visible) source.Play(); }
        private void Failed(VideoPlayer source, string message) { view.SetVideoBackground(null); Debug.LogWarning("Megdan menu still fallback: " + message); }
        private void SetVisible(bool value)
        {
            if (visible == value) return;
            visible = value;
            if (player == null || !player.isPrepared) return;
            if (value) player.Play(); else player.Pause();
        }
        private void OnDestroy()
        {
            if (view != null) view.BackgroundVisibilityChanged -= SetVisible;
            if (player != null) { player.Stop(); player.targetTexture = null; Destroy(player); }
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
