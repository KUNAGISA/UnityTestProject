using System;
using UnityEngine;

internal class ConsoleGUI : MonoBehaviour
{
    private struct Message
    {
        public string Condition;
        public string StackTrace;
        public LogType LogType;
        public DateTime Time;
    }

    private int m_front = 0;
    private int m_rear = 0;
    private readonly Message[] m_messages = new Message[100];

    private Vector2 m_scrollPos = Vector2.zero;
    private int m_selected = -1;

    private void Awake()
    {
        Application.logMessageReceived += OnLogMessageReceived;
        DontDestroyOnLoad(this);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        var next = (m_rear + 1) % m_messages.Length;
        if (next == m_front)
        {
            m_selected = (m_selected == m_front) ? -1 : m_selected;
            m_front = (m_front + 1) % m_messages.Length;
        }

        ref var message = ref m_messages[m_rear];
        message.Condition = condition;
        message.StackTrace = stackTrace;
        message.LogType = type;
        message.Time = DateTime.Now;
        m_rear = next;
    }

    private void OnGUI()
    {
        m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

        var color = GUI.color;
        var size = (m_rear + m_messages.Length - m_front) % m_messages.Length;

        for(var i = m_front + size; i >= m_front; --i)
        { 
            var index = i % m_messages.Length;
            ref readonly var message = ref m_messages[index];

            GUI.color = message.LogType switch
            {
                LogType.Error => Color.red,
                LogType.Assert => Color.red,
                LogType.Warning => Color.yellow,
                LogType.Log => Color.black,
                LogType.Exception => Color.red,
                _ => throw new NotImplementedException(),
            };

            if (GUILayout.Button($"[{message.Time:U}] {message.Condition}", GUIStyle.none))
            {
                m_selected = m_selected == index ? -1 : index;
            }

            if (m_selected == index)
            {
                GUILayout.Label(message.StackTrace);
            }
        }
        GUI.color = color;

        GUILayout.EndScrollView();
    }
}
