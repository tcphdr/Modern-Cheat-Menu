/*
 * Modern Cheat Menu
 * Core.Notifications.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private Queue<Notification> _notifications = new Queue<Notification>();
        private List<Notification> _activeNotifications = new List<Notification>();
        private float _notificationDisplayTime = 3f;
        private float _notificationFadeTime = 0.5f;

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        public struct Notification
        {
            public string Title;
            public string Message;
            public NotificationType Type;
            public float Time;
            public float Alpha;
            public float PositionY;
        }

        private void ShowNotification(string title, string message, NotificationType type)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Time = Time.realtimeSinceStartup,
                Alpha = 0f
            };

            _notifications.Enqueue(notification);

            // Limit queue size
            if (_notifications.Count > 5)
            {
                _notifications.Dequeue();
            }
        }

        private void UpdateNotifications()
        {
            // Process queue
            if (_notifications.Count > 0 && _activeNotifications.Count < 3)
            {
                _activeNotifications.Add(_notifications.Dequeue());
            }

            // Update active notifications
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = _activeNotifications[i];
                float elapsed = Time.realtimeSinceStartup - notification.Time;

                // Fade in
                if (elapsed < 0.3f)
                {
                    notification.Alpha = Mathf.Lerp(0, 1, elapsed / 0.3f);
                }
                // Stay visible
                else if (elapsed < _notificationDisplayTime)
                {
                    notification.Alpha = 1f;
                }
                // Fade out
                else if (elapsed < _notificationDisplayTime + _notificationFadeTime)
                {
                    notification.Alpha = Mathf.Lerp(1, 0, (elapsed - _notificationDisplayTime) / _notificationFadeTime);
                }
                // Remove
                else
                {
                    _activeNotifications.RemoveAt(i);
                    continue;
                }

                // Update position
                float targetY = Screen.height - 100 - (i * 70);
                if (notification.PositionY == 0)
                {
                    // Initial position
                    notification.PositionY = targetY;
                }
                else
                {
                    // Smooth movement
                    notification.PositionY = Mathf.Lerp(notification.PositionY, targetY, Time.deltaTime * 5f);
                }

                _activeNotifications[i] = notification;
            }
        }

        private void DrawNotifications()
        {
            if (_activeNotifications.Count == 0)
                return;

            GUIStyle notifStyle = _statusStyle ?? GUI.skin.box;
            if (notifStyle == null)
                return;

            foreach (var notification in _activeNotifications)
            {
                Rect notifRect = new Rect(
                    Screen.width - 320f,
                    notification.PositionY,
                    300f,
                    60f
                );

                // Background
                GUI.color = new Color(1, 1, 1, notification.Alpha);
                GUI.Box(notifRect, "");

                // Content
                GUILayout.BeginArea(notifRect);

                // Header
                GUILayout.BeginHorizontal();
                GUILayout.Label(notification.Title, GUILayout.Width(280));
                GUILayout.EndHorizontal();

                // Message
                GUILayout.Label(notification.Message);

                GUILayout.EndArea();

                GUI.color = Color.white;
            }
        }
    }
}
