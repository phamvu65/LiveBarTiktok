using System.Collections.Generic;
using UnityEngine;

namespace TikTokLiveGame
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class DjBirthdayDisplay : MonoBehaviour
    {
        private readonly Queue<string> pending = new();
        private TextMesh label;
        private TikTokWebSocketClient client;
        private float expiresAt;

        private void Start()
        {
            label = GetComponent<TextMesh>();
            label.richText = false;
            client = FindFirstObjectByType<TikTokWebSocketClient>();
            if (client != null) client.EventReceived += HandleEvent;
        }

        private void HandleEvent(TikTokEvent data)
        {
            if (data.type == "reset")
            {
                pending.Clear();
                expiresAt = 0f;
                RestoreSign();
                return;
            }
            if (data.type != "gift" || data.action != "birthday") return;
            string account = string.IsNullOrWhiteSpace(data.uniqueId) ? data.nickname : "@" + data.uniqueId.TrimStart('@');
            account = (account ?? "TikTok user").Replace('\n', ' ').Replace('\r', ' ');
            if (account.Length > 26) account = account.Substring(0, 25) + "…";
            if (pending.Count < 10) pending.Enqueue(account);
        }

        private void Update()
        {
            if (label == null || Time.unscaledTime < expiresAt) return;
            if (pending.Count == 0)
            {
                RestoreSign();
                return;
            }
            label.text = "CHÚC MỪNG SINH NHẬT\n" + pending.Dequeue();
            label.characterSize = 0.045f;
            label.color = new Color(1f, 0.8f, 0.25f);
            expiresAt = Time.unscaledTime + 12f;
        }

        private void RestoreSign()
        {
            label.text = "DJ LIVE";
            label.characterSize = 0.16f;
            label.color = Color.cyan;
        }

        private void OnDestroy()
        {
            if (client != null) client.EventReceived -= HandleEvent;
        }
    }
}
