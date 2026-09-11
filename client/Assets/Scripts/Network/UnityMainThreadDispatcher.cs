using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mahjong.Network
{
    /// <summary>
    /// UnityMainThreadDispatcher: Memastikan pesan WebSocket yang diterima di background thread
    /// dieksekusi secara aman di Main Thread Unity (untuk manipulasi GameObject & UI).
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> executionQueue = new Queue<Action>();
        private static UnityMainThreadDispatcher instance;

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("UnityMainThreadDispatcher");
                    instance = obj.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    executionQueue.Dequeue()?.Invoke();
                }
            }
        }

        /// <summary>
        /// Menambahkan action ke antrean eksekusi Main Thread Unity.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }
    }
}
